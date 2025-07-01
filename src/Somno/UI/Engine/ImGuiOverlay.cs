using Somno.Native;
using Somno.Native.WinUSER;
using Somno.Threading;
using Somno.UI.Engine.GDI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Somno.UI.Engine
{
    internal abstract class ImGuiOverlay : IDisposable
    {
        const int OverlayMarginMin = 2;
        const int OverlayMarginMax = 12;

        /// <summary>
        /// Represents the window of the active <see cref="WindowHost"/>.
        /// </summary>
        public Win32Window HostWindow { get; private set; }

        /// <summary>
        /// The window host that is managing the <see cref="HostWindow"/>.
        /// </summary>
        public WindowHost Host { get; private set; }

        readonly Format format;

        ID3D11Device device;
        IDXGISwapChain swapChain;
        ID3D11Texture2D backBuffer;
        ID3D11DeviceContext deviceContext;
        ID3D11RenderTargetView renderView;

        ImGuiRenderer renderer;
        ImGuiInputHandler inputHandler;

        bool isDisposed;
        Thread renderThread;
        volatile bool overlayIsReady;
        readonly CancellationTokenSource cancellationTokenSource;

        bool isClickable;
        WindowExStyles clickableStyles;
        WindowExStyles notClickableStyles;

        readonly Dictionary<string, (IntPtr Handle, uint Width, uint Height)> loadedTexturesPtrs;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ImGuiOverlay"/> class.
        /// </summary>
        public ImGuiOverlay(bool dpiAware = false)
        {
            VSync = true;
            isDisposed = false;
            overlayIsReady = false;
            cancellationTokenSource = new();
            format = Format.R8G8B8A8_UNorm;
            loadedTexturesPtrs = new();

            Host = null!;
            HostWindow = null!;
            device = null!;
            swapChain = null!;
            deviceContext = null!;
            backBuffer = null!;
            renderView = null!;
            renderer = null!;
            inputHandler = null!;
            renderThread = null!;

            if (dpiAware) {
                User32.SetProcessDPIAware();
            }
        }

        #endregion

        #region PublicAPI

        /// <summary>
        /// Starts the overlay
        /// </summary>
        /// <returns>A Task that finishes once the overlay window is ready</returns>
        public async Task Start()
        {
            this.renderThread = new Thread(async () => {
                await InitializeResources();
                renderer.Start();
                RunInfiniteLoop(cancellationTokenSource.Token);
            });

            this.renderThread.Start();
            await WaitHelpers.SpinWait(() => overlayIsReady);
        }

        /// <summary>
        /// Starts the overlay and waits for the overlay window to be closed.
        /// </summary>
        /// <returns>A task that finishes once the overlay window closes</returns>
        public virtual async Task Run()
        {
            if (!overlayIsReady) {
                await Start();
            }

            await WaitHelpers.SpinWait(() => cancellationTokenSource.IsCancellationRequested);
        }

        /// <summary>
        /// Safely Closes the Overlay.
        /// </summary>
        public virtual void Close()
        {
            cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Safely dispose all the resources created by the overlay
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Enable or disable the vsync on the overlay.
        /// </summary>
        public bool VSync;

        /// <summary>
        /// Removes the image from the Overlay.
        /// </summary>
        /// <param name="key">name or pathname which was used to add the image in the first place.</param>
        /// <returns> true if the image is removed otherwise false.</returns>
        public bool RemoveImage(string key)
        {
            if (loadedTexturesPtrs.Remove(key, out var data)) {
                return renderer.RemoveImageTexture(data.Handle);
            }

            return false;
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) {
                return;
            }

            if (disposing) {
                Close();
                renderThread?.Join(100);

                foreach (var key in loadedTexturesPtrs.Keys.ToArray()) {
                    RemoveImage(key);
                }

                cancellationTokenSource?.Dispose();
                swapChain?.Release();
                backBuffer?.Release();
                renderView?.Release();
                renderer?.Dispose();
                Host?.Dispose();
                deviceContext?.Release();
                device?.Release();
            }

            isDisposed = true;
        }

        /// <summary>
        /// Steps to execute after the overlay has fully initialized.
        /// </summary>
        protected virtual Task PostInitialized()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Abstract Task for creating the UI.
        /// </summary>
        /// <returns>Task that finishes once per frame</returns>
        protected abstract void Render();

        private void RunInfiniteLoop(CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();
            float deltaTime = 0f;
            var clearColor = new Color4(0.0f);

            while (!token.IsCancellationRequested) {
                deltaTime = stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
                stopwatch.Restart();
                SetOverlayClickable(inputHandler.Update()); // hot 7%
                renderer.Update(deltaTime, Render);
                deviceContext.OMSetRenderTargets(renderView);
                deviceContext.ClearRenderTargetView(renderView, clearColor);
                renderer.Render(); // hot 4%

                swapChain.Present(0, PresentFlags.None); // hot 15%

                Thread.Sleep(8);
            }
        }

        private void OnResize()
        {
            if (renderView == null) //first show
            {
                using var dxgiFactory = device.QueryInterface<IDXGIDevice>().GetParent<IDXGIAdapter>().GetParent<IDXGIFactory>();
                var swapchainDesc = new SwapChainDescription() {
                    BufferCount = 1,
                    BufferDescription = new ModeDescription(HostWindow.Dimensions.Width, HostWindow.Dimensions.Height, format),
                    Windowed = true,
                    OutputWindow = HostWindow.Handle,
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.Discard,
                    BufferUsage = Usage.RenderTargetOutput,
                };

                swapChain = dxgiFactory.CreateSwapChain(device, swapchainDesc);
                dxgiFactory.MakeWindowAssociation(HostWindow.Handle, WindowAssociationFlags.IgnoreAll);

                backBuffer = swapChain.GetBuffer<ID3D11Texture2D>(0);
                renderView = device.CreateRenderTargetView(backBuffer);
            }
            else {
                renderView.Dispose();
                backBuffer.Dispose();

                swapChain.ResizeBuffers(1, HostWindow.Dimensions.Width, HostWindow.Dimensions.Height, format, SwapChainFlags.None);

                backBuffer = swapChain.GetBuffer<ID3D11Texture2D1>(0);
                renderView = device.CreateRenderTargetView(backBuffer);
            }

            renderer.Resize(HostWindow.Dimensions.Width, HostWindow.Dimensions.Height);
        }

        private async Task InitializeResources()
        {
            D3D11.D3D11CreateDevice(
                null,
                DriverType.Hardware,
                DeviceCreationFlags.None,
                new[] { FeatureLevel.Level_10_0 },
                out device,
                out deviceContext
            );

            Host = new WindowHost(WndProc);
            HostWindow = new Win32Window(Host.WindowHandle);

            renderer = new ImGuiRenderer(device, deviceContext, 800, 600);
            inputHandler = new ImGuiInputHandler(Host);
            overlayIsReady = true;
            await PostInitialized();

            int screenWidth = User32.GetSystemMetrics(SystemMetricsIndex.ScreenWidth);
            int screenHeight = User32.GetSystemMetrics(SystemMetricsIndex.ScreenHeight);

            int x = Random.Shared.Next(OverlayMarginMin, OverlayMarginMax);
            int y = Random.Shared.Next(OverlayMarginMin, OverlayMarginMax);
            int width = screenWidth - x - Random.Shared.Next(OverlayMarginMin, OverlayMarginMax);
            int height = screenHeight - y - Random.Shared.Next(OverlayMarginMin, OverlayMarginMax);
            HostWindow.Dimensions = new(x, y, width, height);

            User32.ShowWindow(HostWindow.Handle, ShowWindowCommand.ShowMaximized);
            User32.SetWindowPos(
                HostWindow.Handle, default,
                x, y,
                width, height,
                0
            );

            InitTransparency();
        }

        /// <summary>
        /// Allows the window to become transparent.
        /// </summary>
        /// <param name="handle">
        /// Window native pointer.
        /// </param>
        internal void InitTransparency()
        {
            var handle = HostWindow.Handle;

            clickableStyles = (WindowExStyles)User32.GetWindowLong(handle, (int)WindowLongParam.GWL_EXSTYLE);
            notClickableStyles = clickableStyles | WindowExStyles.WS_EX_LAYERED | WindowExStyles.WS_EX_TRANSPARENT;
            var margins = new DWMMargins(-1);
            _ = DWMApi.DwmExtendFrameIntoClientArea(handle, ref margins);
            SetOverlayClickable(true);
        }

        internal void SetOverlayClickable(bool wantClickable)
        {
            var handle = HostWindow.Handle;

            if (isClickable ^ wantClickable) {
                if (wantClickable) {
                    User32.SetWindowLong(handle, (int)WindowLongParam.GWL_EXSTYLE, (uint)clickableStyles);
                }
                else {
                    User32.SetWindowLong(handle, (int)WindowLongParam.GWL_EXSTYLE, (uint)notClickableStyles);
                }

                isClickable = wantClickable;
            }
        }

        bool ProcessMessage(WindowMessage msg, UIntPtr wParam, IntPtr lParam)
        {
            switch (msg) {
                case WindowMessage.SIZE:
                    switch ((SizeMessage)wParam) {
                        case SizeMessage.SIZE_RESTORED:
                        case SizeMessage.SIZE_MAXIMIZED:
                            var lp = (int)lParam;
                            HostWindow.Dimensions.Width = lp & 0xFFFF;
                            HostWindow.Dimensions.Height = lp >> 16;
                            OnResize();
                            break;
                        default:
                            break;
                    }

                    break;
                case WindowMessage.DESTROY:
                    Close();
                    break;
                default:
                    break;
            }

            return false;
        }

        (nint, bool) WndProc(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam)
        {
            if (overlayIsReady) {
                if (inputHandler.ProcessMessage((WindowMessage)msg, wParam, lParam) ||
                    ProcessMessage((WindowMessage)msg, wParam, lParam)) {
                    return (0, true);
                }
            }

            return (default, false);
        }
    }
}
