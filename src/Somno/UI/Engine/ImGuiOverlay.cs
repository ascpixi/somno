using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using Somno.Native;
using Somno.Native.WinUSER;
using Somno.Threading;
using Somno.UI.Engine.GDI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace Somno.UI.Engine
{
    [SupportedOSPlatform("windows")]
    internal abstract class ImGuiOverlay : IDisposable
    {
        public Win32Window Window { get; private set; }
        WndClassEx wndClass;
        readonly string windowClassName;

        readonly Format dxFormat;
        ID3D11Device dxDevice;
        ID3D11DeviceContext dxDeviceCtx;
        IDXGISwapChain dxSwapChain;
        ID3D11Texture2D dxBackBuffer;
        ID3D11RenderTargetView dxRenderView;

        ImGuiRenderer renderer;
        ImGuiInputHandler inputHandler;

        IntPtr selfPointer;
        Thread renderThread;
        readonly CancellationTokenSource cancellationTokenSource;
        volatile bool overlayIsReady;

        bool isClickable;
        WindowExStyles clickableStyles;
        WindowExStyles notClickableStyles;
        WindowHost winHost;
        Process hostProcess;

        readonly Dictionary<string, (IntPtr Handle, uint Width, uint Height)> loadedTexturesPtrs;
        bool isDisposed = false;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Overlay"/> class.
        /// </summary>
        /// <param name="dpiAware">
        /// should the overlay scale with windows scale value or not.
        /// </param>
        public ImGuiOverlay(bool dpiAware = false)
        {
            VSync = true;
            overlayIsReady = false;
            cancellationTokenSource = new();
            dxFormat = Format.R8G8B8A8_UNorm;
            loadedTexturesPtrs = new();

            windowClassName = RandomProvider.GenerateString(Random.Shared.Next(13, 18));
            Window = null!;
            dxDevice = null!;
            dxSwapChain = null!;
            dxDeviceCtx = null!;
            dxBackBuffer = null!;
            dxRenderView = null!;
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
            renderThread = new Thread(async () => {
                await InitializeResources();
                renderer.Start();
                RunInfiniteLoop(cancellationTokenSource.Token);
            });

            renderThread.Start();
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
        /// Safely closes the overlay.
        /// </summary>
        public virtual void Close()
        {
            cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Safely disposes all of the resources created by the overlay.
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
        /// Gets or sets the position of the overlay window.
        /// </summary>
        public Point Position {
            get {
                return Window.Dimensions.Location;
            }

            set {
                if (Window.Dimensions.Location != value) {
                    User32.MoveWindow(
                        Window.Handle,
                        value.X, value.Y,
                        Window.Dimensions.Width, Window.Dimensions.Height,
                        true
                    );

                    Window.Dimensions.Location = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the size of the overlay window.
        /// </summary>
        public Size Size {
            get {
                return Window.Dimensions.Size;
            }
            set {
                if (Window.Dimensions.Size != value) {
                    User32.MoveWindow(
                        Window.Handle,
                        Window.Dimensions.X, Window.Dimensions.Y,
                        value.Width, value.Height,
                        true
                    );

                    Window.Dimensions.Size = value;
                }
            }
        }

        /// <summary>
        /// Adds the image to the Graphic Device as a texture.
        /// Then returns the pointer of the added texture. It also
        /// cache the image internally rather than creating a new texture on every call,
        /// so this function can be called multiple times per frame.
        /// </summary>
        /// <param name="filePath">Path to the image on disk.</param>
        /// <param name="srgb"> a value indicating whether pixel format is srgb or not.</param>
        /// <param name="handle">output pointer to the image in the graphic device.</param>
        /// <param name="width">width of the loaded texture.</param>
        /// <param name="height">height of the loaded texture.</param>
        public void AddOrGetImagePointer(string filePath, bool srgb, out IntPtr handle, out uint width, out uint height)
        {
            if (loadedTexturesPtrs.TryGetValue(filePath, out var data)) {
                handle = data.Handle;
                width = data.Width;
                height = data.Height;
            }
            else {
                var decorderOptions = new DecoderOptions();
                decorderOptions.Configuration.PreferContiguousImageBuffers = true;
                using var image = Image.Load<Rgba32>(decorderOptions, filePath);
                handle = renderer.CreateImageTexture(image, srgb ? Format.R8G8B8A8_UNorm_SRgb : Format.R8G8B8A8_UNorm);
                width = (uint)image.Width;
                height = (uint)image.Height;
                loadedTexturesPtrs.Add(filePath, new(handle, width, height));
            }
        }

        /// <summary>
        /// Adds the image to the Graphic Device as a texture.
        /// Then returns the pointer of the added texture. It also
        /// cache the image internally rather than creating a new texture on every call,
        /// so this function can be called multiple times per frame.
        /// </summary>
        /// <param name="name">user friendly name given to the image.</param>
        /// <param name="image">Image data in <see cref="Image"> format.</param>
        /// <param name="srgb"> a value indicating whether pixel format is srgb or not.</param>
        /// <param name="handle">output pointer to the image in the graphic device.</param>
        public void AddOrGetImagePointer(string name, Image<Rgba32> image, bool srgb, out IntPtr handle)
        {
            if (loadedTexturesPtrs.TryGetValue(name, out var data)) {
                handle = data.Handle;
            }
            else {
                handle = renderer.CreateImageTexture(image, srgb ? Format.R8G8B8A8_UNorm_SRgb : Format.R8G8B8A8_UNorm);
                loadedTexturesPtrs.Add(name, new(handle, (uint)image.Width, (uint)image.Height));
            }
        }

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
                renderThread?.Join();
                foreach (var key in loadedTexturesPtrs.Keys.ToArray()) {
                    RemoveImage(key);
                }

                cancellationTokenSource?.Dispose();
                dxSwapChain?.Release();
                dxBackBuffer?.Release();
                dxRenderView?.Release();
                renderer?.Dispose();
                Window?.Dispose();
                dxDeviceCtx?.Release();
                dxDevice?.Release();
                winHost.Dispose();
                hostProcess.Kill();
            }

            if (selfPointer != IntPtr.Zero) {
                if (!User32.UnregisterClass(windowClassName, selfPointer)) {
                    throw new Exception($"Failed to Unregister {windowClassName} class during dispose.");
                }

                selfPointer = IntPtr.Zero;
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
            OnResize();

            var stopwatch = Stopwatch.StartNew();
            float deltaTime = 0f;
            var clearColor = new Color4(0.0f);
            while (!token.IsCancellationRequested) {
                User32.SetWindowLong(
    winHost.Handle,
    (int)WindowLongParam.GWL_EXSTYLE,
    (uint)(WindowExStyles.WS_EX_ACCEPTFILES | WindowExStyles.WS_EX_TOPMOST)
);
                deltaTime = stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
                stopwatch.Restart();
                Window.PumpEvents(); // hot 1.4%
                SetOverlayClickable(inputHandler.Update()); // hot 7%
                renderer.Update(deltaTime, Render);
                dxDeviceCtx.OMSetRenderTargets(dxRenderView);
                dxDeviceCtx.ClearRenderTargetView(dxRenderView, clearColor);
                renderer.Render(); // hot 4%

                dxSwapChain.Present(0, PresentFlags.None); // hot 15%

                Thread.Sleep(8);
            }
        }

        private void OnResize()
        {
            if (dxRenderView == null) //first show
            {
                using var dxgiFactory = dxDevice.QueryInterface<IDXGIDevice>().GetParent<IDXGIAdapter>().GetParent<IDXGIFactory>();
                var swapchainDesc = new SwapChainDescription() {
                    BufferCount = 1,
                    BufferDescription = new ModeDescription(Window.Dimensions.Width, Window.Dimensions.Height, dxFormat),
                    Windowed = true,
                    OutputWindow = Window.Handle,
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.Discard,
                    BufferUsage = Usage.RenderTargetOutput,
                };

                dxSwapChain = dxgiFactory.CreateSwapChain(dxDevice, swapchainDesc);
                dxgiFactory.MakeWindowAssociation(Window.Handle, WindowAssociationFlags.IgnoreAll);

                dxBackBuffer = dxSwapChain.GetBuffer<ID3D11Texture2D>(0);
                dxRenderView = dxDevice.CreateRenderTargetView(dxBackBuffer);
            }
            else {
                dxRenderView.Dispose();
                dxBackBuffer.Dispose();

                dxSwapChain.ResizeBuffers(1, Window.Dimensions.Width, Window.Dimensions.Height, dxFormat, SwapChainFlags.None);

                dxBackBuffer = dxSwapChain.GetBuffer<ID3D11Texture2D1>(0);
                dxRenderView = dxDevice.CreateRenderTargetView(dxBackBuffer);
            }

            renderer.Resize(Window.Dimensions.Width, Window.Dimensions.Height);
        }

        private async Task InitializeResources()
        {
            D3D11.D3D11CreateDevice(
                null,
                DriverType.Hardware,
                DeviceCreationFlags.None,
                new[] { FeatureLevel.Level_10_0 },
                out dxDevice,
                out dxDeviceCtx
            );

            selfPointer = Kernel32.GetModuleHandle(null);

            wndClass = new WndClassEx {
                Size = Unsafe.SizeOf<WndClassEx>(),
                Styles =
                    WindowClassStyles.HorizontalRedraw |
                    WindowClassStyles.VerticalRedraw |
                    WindowClassStyles.ParentDC,
                WindowProc = WndProc,
                InstanceHandle = selfPointer,
                CursorHandle = User32.LoadCursor(IntPtr.Zero, SystemCursor.IDC_ARROW),
                BackgroundBrushHandle = IntPtr.Zero,
                IconHandle = IntPtr.Zero,
                MenuName = string.Empty,
                ClassName = windowClassName,
                SmallIconHandle = IntPtr.Zero,
                ClassExtraBytes = 0,
                WindowExtraBytes = 0
            };

            if (User32.RegisterClassEx(ref wndClass) == 0) {
                throw new Exception($"Failed to register the window class.");
            }

            //Window = new Win32Window(
            //    wndClass.ClassName,
            //    800,
            //    600,
            //    0,
            //    0,
            //    RandomProvider.GenerateSentence(3),
            //    WindowStyles.WS_POPUP,
            //    WindowExStyles.WS_EX_ACCEPTFILES | WindowExStyles.WS_EX_TOPMOST
            //);

            hostProcess = Process.Start("Poopfart.exe");
            Thread.Sleep(20);

            winHost = new WindowHost(hostProcess, WndProc);
            winHost.Start();
            Window = new Win32Window(winHost.Handle);

            User32.SetWindowLong(
                winHost.Handle,
                (int)WindowLongParam.GWL_STYLE,
                (uint)WindowStyles.WS_POPUP
            );

            User32.SetWindowLong(
                winHost.Handle,
                (int)WindowLongParam.GWL_EXSTYLE,
                (uint)(WindowExStyles.WS_EX_ACCEPTFILES | WindowExStyles.WS_EX_TOPMOST)
            );

            User32.SetWindowPos(winHost.Handle, 0, 0, 0, 1366, 768, -1);

            renderer = new ImGuiRenderer(dxDevice, dxDeviceCtx, 800, 600);
            inputHandler = new ImGuiInputHandler(Window.Handle);
            overlayIsReady = true;
            await PostInitialized();
            User32.ShowWindow(Window.Handle, ShowWindowCommand.ShowMaximized);
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
            var handle = Window.Handle;

            clickableStyles = (WindowExStyles)User32.GetWindowLong(handle, (int)WindowLongParam.GWL_EXSTYLE);
            notClickableStyles = clickableStyles | WindowExStyles.WS_EX_LAYERED | WindowExStyles.WS_EX_TRANSPARENT;
            var margins = new DWMMargins(-1);
            _ = DWMApi.DwmExtendFrameIntoClientArea(handle, ref margins);
            SetOverlayClickable(true);
        }

        /// <summary>
        /// Enables (clickable) / Disables (not clickable) the Window keyboard/mouse inputs.
        /// NOTE: This function depends on InitTransparency being called when the Window was created.
        /// </summary>
        /// <param name="handle">Veldrid window handle in IntPtr format.</param>
        /// <param name="wantClickable">Set to true if you want to make the window clickable otherwise false.</param>
        internal void SetOverlayClickable(bool wantClickable)
        {
            var handle = Window.Handle;

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

        private bool ProcessMessage(WindowMessage msg, UIntPtr wParam, IntPtr lParam)
        {
            switch (msg) {
                case WindowMessage.SIZE:
                    switch ((SizeMessage)wParam) {
                        case SizeMessage.SIZE_RESTORED:
                        case SizeMessage.SIZE_MAXIMIZED:
                            var lp = (int)lParam;
                            Window.Dimensions.Width = lp & 0xFFFF;
                            Window.Dimensions.Height = lp >> 16;
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

        private IntPtr WndProc(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam)
        {
            Console.WriteLine($"WndProc");

            if (overlayIsReady) {
                if (inputHandler.ProcessMessage((WindowMessage)msg, wParam, lParam) ||
                    ProcessMessage((WindowMessage)msg, wParam, lParam)) {
                    return IntPtr.Zero;
                }
            }

            return User32.DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }
}
