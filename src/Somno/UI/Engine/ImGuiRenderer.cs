using ImGuiNET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using ImDrawIdx = System.UInt16;

namespace Somno.UI.Engine;

internal unsafe sealed class ImGuiRenderer : IDisposable
{
    const int VertexConstantBufferSize = 16 * 4;

    readonly ID3D11Device device;
    readonly ID3D11DeviceContext deviceContext;
    readonly ImGuiRendererObjects devObj;
    ID3D11Buffer? vertexBuffer;
    ID3D11Buffer? indexBuffer;
    ID3D11SamplerState? fontSampler;

    readonly Dictionary<IntPtr, ID3D11ShaderResourceView> textureResources = new();
    int vertexBufferSize = 5000, indexBufferSize = 10000;

    public ImGuiRenderer(ID3D11Device device, ID3D11DeviceContext deviceContext, int width, int height)
    {
        this.device = device;
        this.deviceContext = deviceContext;

        device.AddRef();
        deviceContext.AddRef();

        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;  // We can honor the ImDrawCmd::VtxOffset field, allowing for large meshes.
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        ImGui.StyleColorsDark();

        Resize(width, height);

        devObj = new ImGuiRendererObjects(device);

        CreateFontsTexture();
        CreateFontSampler();
    }

    public void Start()
    {
        ImGui.NewFrame();
    }

    public void Update(float deltaTime, Action doRender)
    {
        var io = ImGui.GetIO();
        io.DeltaTime = deltaTime;
        ImGui.NewFrame();
        doRender?.Invoke();
        ImGui.Render();
    }

    public void Render()
    {
        ImDrawDataPtr data = ImGui.GetDrawData();

        // Avoid rendering when minimized
        if (data.DisplaySize.X <= 0.0f || data.DisplaySize.Y <= 0.0f)
            return;

        var ctx = deviceContext;
        if (vertexBuffer == null || vertexBufferSize < data.TotalVtxCount) {
            vertexBuffer?.Release();

            vertexBufferSize = data.TotalVtxCount + 5000;
            var desc = new BufferDescription(
                vertexBufferSize * sizeof(ImDrawVert),
                BindFlags.VertexBuffer,
                ResourceUsage.Dynamic,
                CpuAccessFlags.Write
            );

            vertexBuffer = device.CreateBuffer(desc);
        }

        if (indexBuffer == null || indexBufferSize < data.TotalIdxCount) {
            indexBuffer?.Release();

            indexBufferSize = data.TotalIdxCount + 10000;

            var desc = new BufferDescription(
                indexBufferSize * sizeof(ImDrawIdx),
                BindFlags.IndexBuffer,
                ResourceUsage.Dynamic,
                CpuAccessFlags.Write
            );

            indexBuffer = device.CreateBuffer(desc);
        }

        // Upload vertex/index data into a single contiguous GPU buffer
        var vertexResource = ctx.Map(vertexBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
        var indexResource = ctx.Map(indexBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
        var vertexResourcePointer = (ImDrawVert*)vertexResource.DataPointer;
        var indexResourcePointer = (ImDrawIdx*)indexResource.DataPointer;
        
        for (int n = 0; n < data.CmdListsCount; n++) {
            var cmdlList = data.CmdListsRange[n];

            var vertBytes = cmdlList.VtxBuffer.Size * sizeof(ImDrawVert);
            Buffer.MemoryCopy((void*)cmdlList.VtxBuffer.Data, vertexResourcePointer, vertBytes, vertBytes);

            var idxBytes = cmdlList.IdxBuffer.Size * sizeof(ImDrawIdx);
            Buffer.MemoryCopy((void*)cmdlList.IdxBuffer.Data, indexResourcePointer, idxBytes, idxBytes);

            vertexResourcePointer += cmdlList.VtxBuffer.Size;
            indexResourcePointer += cmdlList.IdxBuffer.Size;
        }

        ctx.Unmap(vertexBuffer, 0);
        ctx.Unmap(indexBuffer, 0);

        // Setup orthographic projection matrix into our constant buffer
        // Our visible imgui space lies from draw_data.DisplayPos (top left) to draw_data.DisplayPos+data_data.DisplaySize (bottom right). DisplayPos is (0,0) for single viewport apps.

        var constResource = ctx.Map(devObj.ConstantBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
        var span = constResource.AsSpan<float>(VertexConstantBufferSize);

        // Create the MVP (ModelViewProjection) matrix.
        float l = data.DisplayPos.X;
        float r = data.DisplayPos.X + data.DisplaySize.X;
        float t = data.DisplayPos.Y;
        float b = data.DisplayPos.Y + data.DisplaySize.Y;
        float[] mvp =
        {
                2.0f/(r-l),   0.0f,           0.0f,       0.0f,
                0.0f,         2.0f/(t-b),     0.0f,       0.0f,
                0.0f,         0.0f,           0.5f,       0.0f,
                (r+l)/(l-r),  (t+b)/(b-t),    0.5f,       1.0f,
        };

        mvp.CopyTo(span);
        ctx.Unmap(devObj.ConstantBuffer, 0);
        //BackupDX11State(ctx); // only required if imgui is injected + drawn on existing process.
        SetupRenderState(data, ctx);
        // Render command lists
        // (Because we merged all buffers into a single one, we maintain our own offset into them)
        int global_idx_offset = 0;
        int global_vtx_offset = 0;
        for (int n = 0; n < data.CmdListsCount; n++) {
            var cmdList = data.CmdListsRange[n];
            for (int i = 0; i < cmdList.CmdBuffer.Size; i++) {
                var cmd = cmdList.CmdBuffer[i];
                if (cmd.UserCallback != IntPtr.Zero) {
                    throw new NotImplementedException("user callbacks not implemented");
                }
                else {
                    ctx.RSSetScissorRect(
                        (int)cmd.ClipRect.X,
                        (int)cmd.ClipRect.Y,
                        (int)(cmd.ClipRect.Z - cmd.ClipRect.X),
                        (int)(cmd.ClipRect.W - cmd.ClipRect.Y));

                    if (textureResources.TryGetValue(cmd.GetTexID(), out var texture)) {
                        ctx.PSSetShaderResource(0, texture);
                    }

                    ctx.DrawIndexed((int)cmd.ElemCount, (int)(cmd.IdxOffset + global_idx_offset), (int)(cmd.VtxOffset + global_vtx_offset));
                }
            }

            global_idx_offset += cmdList.IdxBuffer.Size;
            global_vtx_offset += cmdList.VtxBuffer.Size;
        }

        //RestoreDX11State(ctx); // only required if imgui is injected + drawn on existing process.
    }

    public void Dispose()
    {
        if (device == null)
            return;

        UnregisterAllTextures();
        fontSampler?.Release();
        indexBuffer?.Release();
        vertexBuffer?.Release();
        devObj.Dispose();
    }

    public void Resize(int width, int height)
    {
        ImGui.GetIO().DisplaySize = new Vector2(width, height);
    }

    public IntPtr CreateImageTexture(Image<Rgba32> image, Format format)
    {
        var texDesc = new Texture2DDescription(format, image.Width, image.Height, 1, 1);
        if (!image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> memory)) {
            throw new Exception("Make sure to initialize MemoryAllocator.Default!");
        }

        using MemoryHandle imageMemoryHandle = memory.Pin();
        var subResource = new SubresourceData(imageMemoryHandle.Pointer, texDesc.Width * 4);
        using var texture = device.CreateTexture2D(texDesc, new[] { subResource });
        var resViewDesc = new ShaderResourceViewDescription(texture, ShaderResourceViewDimension.Texture2D, format, 0, texDesc.MipLevels);
        return RegisterTexture(device.CreateShaderResourceView(texture, resViewDesc));
    }

    public bool RemoveImageTexture(IntPtr handle)
    {
        using var tex = UnregisterTexture(handle);
        return tex != null;
    }

    public void UpdateFontTexture(string fontPathName, float fontSize, ushort[]? fontCustomGlyphRange)
    {
        var io = ImGui.GetIO();
        UnregisterTexture(io.Fonts.TexID)?.Dispose();
        io.Fonts.Clear();

        var config = ImGuiNative.ImFontConfig_ImFontConfig();
        if (fontCustomGlyphRange == null) {
            io.Fonts.AddFontFromFileTTF(fontPathName, fontSize, config, io.Fonts.GetGlyphRangesDefault());
        }
        else {
            fixed (ushort* p = &fontCustomGlyphRange[0]) {
                io.Fonts.AddFontFromFileTTF(fontPathName, fontSize, config, new IntPtr(p));
            }
        }

        CreateFontsTexture();
        ImGuiNative.ImFontConfig_destroy(config);
    }

    void SetupRenderState(ImDrawDataPtr drawData, ID3D11DeviceContext ctx)
    {
        var viewport = new Viewport(0f, 0f, drawData.DisplaySize.X, drawData.DisplaySize.Y, 0f, 1f);
        ctx.RSSetViewport(viewport);
        int stride = sizeof(ImDrawVert);
        ctx.IASetInputLayout(devObj.InputLayout);
        ctx.IASetVertexBuffer(0, vertexBuffer!, stride);
        ctx.IASetIndexBuffer(indexBuffer, sizeof(ImDrawIdx) == 2 ? Format.R16_UInt : Format.R32_UInt, 0);
        ctx.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        ctx.VSSetShader(devObj.VertexShader);
        ctx.VSSetConstantBuffer(0, devObj.ConstantBuffer);
        ctx.PSSetShader(devObj.PixelShader);
        ctx.PSSetSampler(0, fontSampler);
        ctx.GSSetShader(null);
        ctx.HSSetShader(null);
        ctx.DSSetShader(null);
        ctx.CSSetShader(null);

        ctx.OMSetBlendState(devObj.BlendState, new Color4(0f, 0f, 0f, 0f));
        ctx.OMSetDepthStencilState(devObj.DepthStencilState);
        ctx.RSSetState(devObj.RasterizerState);
    }

    void CreateFontsTexture()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out var width, out var height);
        var texDesc = new Texture2DDescription(Format.R8G8B8A8_UNorm, width, height, 1, 1);
        var subResource = new SubresourceData(pixels, texDesc.Width * 4);
        using var texture = device.CreateTexture2D(texDesc, new[] { subResource });
        var resViewDesc = new ShaderResourceViewDescription(
            texture,
            ShaderResourceViewDimension.Texture2D,
            Format.R8G8B8A8_UNorm,
            0,
            texDesc.MipLevels
        );

        io.Fonts.SetTexID(RegisterTexture(device.CreateShaderResourceView(texture, resViewDesc)));
        io.Fonts.ClearTexData();
    }

    void CreateFontSampler()
    {
        var samplerDesc = new SamplerDescription(
            Filter.MinMagMipLinear,
            TextureAddressMode.Wrap,
            TextureAddressMode.Wrap,
            TextureAddressMode.Wrap,
            0f,
            0,
            ComparisonFunction.Always,
            0f,
            0f
        );

        this.fontSampler = device.CreateSamplerState(samplerDesc);
    }

    IntPtr RegisterTexture(ID3D11ShaderResourceView texture)
    {
        var imguiID = texture.NativePointer;
        textureResources.TryAdd(imguiID, texture);
        return imguiID;
    }

    ID3D11ShaderResourceView? UnregisterTexture(IntPtr texturePtr)
    {
        if (textureResources.Remove(texturePtr, out var texture)) {
            return texture;
        }

        return null;
    }

    void UnregisterAllTextures()
    {
        foreach (var key in textureResources.Keys.ToArray()) {
            UnregisterTexture(key)?.Release();
        }
    }
}
