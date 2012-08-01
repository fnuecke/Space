uniform extern float4 Color = float4(1, 0, 1, 1);
uniform extern float2 Gradient = float2(1, 1);

struct VertexShaderData
{
    float4 Position : POSITION0;
    float2 TextureCoordinate : TEXCOORD0;
};

VertexShaderData VertexShaderFunction(VertexShaderData input)
{
    return input;
}

float4 PixelShaderFunction(VertexShaderData input) : COLOR0
{
    float a = clamp(max(1 - abs(input.TextureCoordinate.x) / Gradient.x,
                        1 - abs(input.TextureCoordinate.y) / Gradient.y), 0, 1);
    return Color * a;
}

technique Render
{
    pass Pass1
    {
        AlphaBlendEnable = True;
        DestBlend = One;
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
