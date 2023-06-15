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
    internal abstract class ImGuiOverlay : IDisposable
    {
        public Win32Window Window { get; private set; }

        private readonly string title;
        private readonly Format format;

        private WndClassEx wndClass;
        private ID3D11Device device;
        private ID3D11DeviceContext deviceContext;
        private IDXGISwapChain swapChain;
        private ID3D11Texture2D backBuffer;
        private ID3D11RenderTargetView renderView;

        private ImGuiRenderer renderer;
        private ImGuiInputHandler inputhandler;

        private bool _disposedValue;
        private IntPtr selfPointer;
        private Thread renderThread;
        private volatile CancellationTokenSource cancellationTokenSource;
        private volatile bool overlayIsReady;

        private bool replaceFont = false;
        private ushort[]? fontCustomGlyphRange;
        private string fontPathName;
        private float fontSize;

        private bool isClickable;
        private WindowExStyles clickableStyles;
        private WindowExStyles notClickableStyles;

        private Dictionary<string, (IntPtr Handle, uint Width, uint Height)> loadedTexturesPtrs;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Overlay"/> class.
        /// </summary>
        public ImGuiOverlay() : this("Overlay")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Overlay"/> class.
        /// </summary>
        /// <param name="windowTitle">
        /// Title of the window created by the overlay
        /// </param>
        public ImGuiOverlay(string windowTitle) : this(windowTitle, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Overlay"/> class.
        /// </summary>
        /// <param name="dpiAware">
        /// should the overlay scale with windows scale value or not.
        /// </param>
        public ImGuiOverlay(bool dpiAware) : this("Overlay", dpiAware)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Overlay"/> class.
        /// </summary>
        /// <param name="windowTitle">
        /// Title of the window created by the overlay
        /// </param>
        /// <param name="dpiAware">
        /// should the overlay scale with windows scale value or not.
        /// </param>
        public ImGuiOverlay(string windowTitle, bool dpiAware)
        {
            this.VSync = true;
            this._disposedValue = false;
            this.overlayIsReady = false;
            this.title = windowTitle;
            this.cancellationTokenSource = new();
            this.format = Format.R8G8B8A8_UNorm;
            this.loadedTexturesPtrs = new();
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
                await this.InitializeResources();
                this.renderer.Start();
                this.RunInfiniteLoop(this.cancellationTokenSource.Token);
            });

            this.renderThread.Start();
            await WaitHelpers.SpinWait(() => this.overlayIsReady);
        }

        /// <summary>
        /// Starts the overlay and waits for the overlay window to be closed.
        /// </summary>
        /// <returns>A task that finishes once the overlay window closes</returns>
        public virtual async Task Run()
        {
            if (!this.overlayIsReady) {
                await this.Start();
            }

            await WaitHelpers.SpinWait(() => this.cancellationTokenSource.IsCancellationRequested);
        }

        /// <summary>
        /// Safely Closes the Overlay.
        /// </summary>
        public virtual void Close()
        {
            this.cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Safely dispose all the resources created by the overlay
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Replaces the ImGui font with another one.
        /// </summary>
        /// <param name="pathName">pathname to the TTF font file.</param>
        /// <param name="size">font size to load.</param>
        /// <param name="language">supported language by the font.</param>
        /// <returns>true if the font replacement is valid otherwise false.</returns>
        public bool ReplaceFont(string pathName, int size)
        {
            if (!File.Exists(pathName)) {
                return false;
            }

            this.fontPathName = pathName;
            this.fontSize = size;
            this.replaceFont = true;
            this.fontCustomGlyphRange = null;
            return true;
        }

        /// <summary>
        /// Replaces the ImGui font with another one.
        /// </summary>
        /// <param name="pathName">pathname to the TTF font file.</param>
        /// <param name="size">font size to load.</param>
        /// <param name="glyphRange">custom glyph range of the font to load. Read <see cref="FontGlyphRangeType"/> for more detail.</param>
        /// <returns>>true if the font replacement is valid otherwise false.</returns>
        public bool ReplaceFont(string pathName, int size, ushort[] glyphRange)
        {
            if (!File.Exists(pathName)) {
                return false;
            }

            fontPathName = pathName;
            fontSize = size;
            fontCustomGlyphRange = glyphRange;
            replaceFont = true;
            return true;
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
                return this.Window.Dimensions.Location;
            }

            set {
                if (this.Window.Dimensions.Location != value) {
                    User32.MoveWindow(this.Window.Handle, value.X, value.Y, this.Window.Dimensions.Width, this.Window.Dimensions.Height, true);
                    this.Window.Dimensions.Location = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the size of the overlay window.
        /// </summary>
        public Size Size {
            get {
                return this.Window.Dimensions.Size;
            }
            set {
                if (this.Window.Dimensions.Size != value) {
                    User32.MoveWindow(this.Window.Handle, this.Window.Dimensions.X, this.Window.Dimensions.Y, value.Width, value.Height, true);
                    this.Window.Dimensions.Size = value;
                }
            }
        }

        /// <summary>
        /// Gets the number of displays available on the computer.
        /// </summary>
        public static int NumberVideoDisplays {
            get {
                return User32.GetSystemMetrics(0x50); // SM_CMONITORS
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
            if (this.loadedTexturesPtrs.TryGetValue(filePath, out var data)) {
                handle = data.Handle;
                width = data.Width;
                height = data.Height;
            }
            else {
                var decorderOptions = new DecoderOptions();
                decorderOptions.Configuration.PreferContiguousImageBuffers = true;
                using var image = Image.Load<Rgba32>(decorderOptions, filePath);
                handle = this.renderer.CreateImageTexture(image, srgb ? Format.R8G8B8A8_UNorm_SRgb : Format.R8G8B8A8_UNorm);
                width = (uint)image.Width;
                height = (uint)image.Height;
                this.loadedTexturesPtrs.Add(filePath, new(handle, width, height));
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
            if (this.loadedTexturesPtrs.TryGetValue(name, out var data)) {
                handle = data.Handle;
            }
            else {
                handle = this.renderer.CreateImageTexture(image, srgb ? Format.R8G8B8A8_UNorm_SRgb : Format.R8G8B8A8_UNorm);
                this.loadedTexturesPtrs.Add(name, new(handle, (uint)image.Width, (uint)image.Height));
            }
        }

        /// <summary>
        /// Removes the image from the Overlay.
        /// </summary>
        /// <param name="key">name or pathname which was used to add the image in the first place.</param>
        /// <returns> true if the image is removed otherwise false.</returns>
        public bool RemoveImage(string key)
        {
            if (this.loadedTexturesPtrs.Remove(key, out var data)) {
                return this.renderer.RemoveImageTexture(data.Handle);
            }

            return false;
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (this._disposedValue) {
                return;
            }

            if (disposing) {
                this.renderThread?.Join();
                foreach (var key in this.loadedTexturesPtrs.Keys.ToArray()) {
                    this.RemoveImage(key);
                }

                this.cancellationTokenSource?.Dispose();
                this.swapChain?.Release();
                this.backBuffer?.Release();
                this.renderView?.Release();
                this.renderer?.Dispose();
                this.Window?.Dispose();
                this.deviceContext?.Release();
                this.device?.Release();
            }

            if (this.selfPointer != IntPtr.Zero) {
                if (!User32.UnregisterClass(this.title, this.selfPointer)) {
                    throw new Exception($"Failed to Unregister {this.title} class during dispose.");
                }

                this.selfPointer = IntPtr.Zero;
            }

            this._disposedValue = true;
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
                this.Window.PumpEvents();
                SetOverlayClickable(this.inputhandler.Update());
                this.renderer.Update(deltaTime, () => { Render(); });
                this.deviceContext.OMSetRenderTargets(renderView);
                this.deviceContext.ClearRenderTargetView(renderView, clearColor);
                this.renderer.Render();
                if (VSync) {
                    this.swapChain.Present(1, PresentFlags.None); // Present with vsync
                }
                else {
                    this.swapChain.Present(0, PresentFlags.None); // Present without vsync
                }

                this.ReplaceFontIfRequired();
            }
        }

        private void ReplaceFontIfRequired()
        {
            if (this.replaceFont && this.renderer != null) {
                this.renderer.UpdateFontTexture(this.fontPathName, this.fontSize, this.fontCustomGlyphRange);
                this.replaceFont = false;
            }
        }

        private void OnResize()
        {
            if (renderView == null)//first show
            {
                using var dxgiFactory = device.QueryInterface<IDXGIDevice>().GetParent<IDXGIAdapter>().GetParent<IDXGIFactory>();
                var swapchainDesc = new SwapChainDescription() {
                    BufferCount = 1,
                    BufferDescription = new ModeDescription(this.Window.Dimensions.Width, this.Window.Dimensions.Height, this.format),
                    Windowed = true,
                    OutputWindow = this.Window.Handle,
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.Discard,
                    BufferUsage = Usage.RenderTargetOutput,
                };

                this.swapChain = dxgiFactory.CreateSwapChain(this.device, swapchainDesc);
                dxgiFactory.MakeWindowAssociation(this.Window.Handle, WindowAssociationFlags.IgnoreAll);

                this.backBuffer = this.swapChain.GetBuffer<ID3D11Texture2D>(0);
                this.renderView = this.device.CreateRenderTargetView(backBuffer);
            }
            else {
                this.renderView.Dispose();
                this.backBuffer.Dispose();

                this.swapChain.ResizeBuffers(1, this.Window.Dimensions.Width, this.Window.Dimensions.Height, this.format, SwapChainFlags.None);

                backBuffer = this.swapChain.GetBuffer<ID3D11Texture2D1>(0);
                renderView = this.device.CreateRenderTargetView(backBuffer);
            }

            this.renderer.Resize(this.Window.Dimensions.Width, this.Window.Dimensions.Height);
        }

        private async Task InitializeResources()
        {
            D3D11.D3D11CreateDevice(
            null,
                DriverType.Hardware,
                DeviceCreationFlags.None,
                new[] { FeatureLevel.Level_10_0 },
                out this.device,
                out this.deviceContext);
            this.selfPointer = Kernel32.GetModuleHandle(null);
            this.wndClass = new WndClassEx {
                Size = Unsafe.SizeOf<WndClassEx>(),
                Styles =
                    WindowClassStyles.HorizontalRedraw |
                    WindowClassStyles.VerticalRedraw |
                    WindowClassStyles.ParentDC,
                WindowProc = WndProc,
                InstanceHandle = this.selfPointer,
                CursorHandle = User32.LoadCursor(IntPtr.Zero, SystemCursor.IDC_ARROW),
                BackgroundBrushHandle = IntPtr.Zero,
                IconHandle = IntPtr.Zero,
                MenuName = string.Empty,
                ClassName = this.title,
                SmallIconHandle = IntPtr.Zero,
                ClassExtraBytes = 0,
                WindowExtraBytes = 0
            };

            if (User32.RegisterClassEx(ref this.wndClass) == 0) {
                throw new Exception($"Failed to Register class of name {this.wndClass.ClassName}");
            }

            this.Window = new Win32Window(
                wndClass.ClassName,
                800,
                600,
                0,
                0,
                this.title,
                WindowStyles.WS_POPUP,
                WindowExStyles.WS_EX_ACCEPTFILES | WindowExStyles.WS_EX_TOPMOST);
            this.renderer = new ImGuiRenderer(device, deviceContext, 800, 600);
            this.inputhandler = new ImGuiInputHandler(this.Window.Handle);
            this.overlayIsReady = true;
            await this.PostInitialized();
            User32.ShowWindow(this.Window.Handle, ShowWindowCommand.ShowMaximized);
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
            var handle = this.Window.Handle;

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
            var handle = this.Window.Handle;

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
                            this.Window.Dimensions.Width = lp & 0xFFFF;
                            this.Window.Dimensions.Height = lp >> 16;
                            this.OnResize();
                            break;
                        default:
                            break;
                    }

                    break;
                case WindowMessage.DESTROY:
                    this.Close();
                    break;
                default:
                    break;
            }

            return false;
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam)
        {
            if (this.overlayIsReady) {
                if (this.inputhandler.ProcessMessage((WindowMessage)msg, wParam, lParam) ||
                    ProcessMessage((WindowMessage)msg, wParam, lParam)) {
                    return IntPtr.Zero;
                }
            }

            return User32.DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }
}
