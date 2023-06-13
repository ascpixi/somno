using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Somno.UI.Engine
{
    internal class ImGuiRendererObjects : IDisposable
    {
        const int VertexConstantBufferSize = 16 * 4;

        public Blob VertexShaderBlob;
        public ID3D11VertexShader VertexShader;
        public ID3D11InputLayout InputLayout;
        public ID3D11Buffer ConstantBuffer;
        public Blob PixelShaderBlob;
        public ID3D11PixelShader PixelShader;
        public ID3D11RasterizerState RasterizerState;
        public ID3D11BlendState BlendState;
        public ID3D11DepthStencilState DepthStencilState;

        const string VertexShaderSource =
        @"
            cbuffer vertexBuffer : register(b0)
            {
                float4x4 ProjectionMatrix;
            };

            struct VS_INPUT
            {
                float2 pos : POSITION;
                float4 col : COLOR0;
                float2 uv  : TEXCOORD0;
            };

            struct PS_INPUT
            {
                float4 pos : SV_POSITION;
                float4 col : COLOR0;
                float2 uv  : TEXCOORD0;
            };

            PS_INPUT main(VS_INPUT input)
            {
                PS_INPUT output;
                output.pos = mul(ProjectionMatrix, float4(input.pos.xy, 0.f, 1.f));
                output.col = input.col;
                output.uv  = input.uv;
                return output;
            }
        ";

        const string PixelShaderSource =
        @"
            struct PS_INPUT
            {
                float4 pos : SV_POSITION;
                float4 col : COLOR0;
                float2 uv  : TEXCOORD0;
            };

            sampler sampler0;
            Texture2D texture0;

            float4 main(PS_INPUT input) : SV_Target
            {
                return input.col * texture0.Sample(sampler0, input.uv);
            }
        ";

        internal ImGuiRendererObjects(ID3D11Device device)
        {
            Compiler.Compile(VertexShaderSource, "main", "vs", "vs_4_0", out VertexShaderBlob, out _);
            if (VertexShaderBlob == null)
                throw new Exception("Error compiling vertex shader.");

            VertexShader = device.CreateVertexShader(VertexShaderBlob);

            var inputElements = new[]
            {
                new InputElementDescription( "POSITION", 0, Format.R32G32_Float,   0, 0, InputClassification.PerVertexData, 0 ),
                new InputElementDescription( "TEXCOORD", 0, Format.R32G32_Float,   8,  0, InputClassification.PerVertexData, 0 ),
                new InputElementDescription( "COLOR",    0, Format.R8G8B8A8_UNorm, 16, 0, InputClassification.PerVertexData, 0 ),
            };

            InputLayout = device.CreateInputLayout(inputElements, VertexShaderBlob);
            var constBufferDesc = new BufferDescription(
                VertexConstantBufferSize,
                BindFlags.ConstantBuffer,
                ResourceUsage.Dynamic,
                CpuAccessFlags.Write
            );

            ConstantBuffer = device.CreateBuffer(constBufferDesc);

            Compiler.Compile(PixelShaderSource, "main", "ps", "ps_4_0", out PixelShaderBlob, out _);
            if (PixelShaderBlob == null)
                throw new Exception("Error compiling pixel shader.");

            PixelShader = device.CreatePixelShader(PixelShaderBlob);

            var blendDesc = new BlendDescription(Blend.SourceAlpha, Blend.InverseSourceAlpha, Blend.One, Blend.InverseSourceAlpha);
            BlendState = device.CreateBlendState(blendDesc);

            var rasterDesc = new RasterizerDescription(CullMode.None, FillMode.Solid) {
                MultisampleEnable = false,
                ScissorEnable = true
            };
            RasterizerState = device.CreateRasterizerState(rasterDesc);

            var depthDesc = new DepthStencilDescription(false, DepthWriteMask.All, ComparisonFunction.Always);
            DepthStencilState = device.CreateDepthStencilState(depthDesc);
        }

        public void Dispose()
        {
            BlendState.Release();
            DepthStencilState.Release();
            RasterizerState.Release();
            PixelShader.Release();
            PixelShaderBlob.Release();
            ConstantBuffer.Release();
            InputLayout.Release();
            VertexShader.Release();
            VertexShaderBlob.Release();
        }
    }
}
