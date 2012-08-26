uniform extern float4 Color = float4(1, 0, 1, 1);
uniform extern float2 Thickness = float2(1, 1);

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
    float a = max((abs(input.TextureCoordinate.x) - 1) / Thickness.x,
                  (abs(input.TextureCoordinate.y) - 1) / Thickness.y) + 1;
    return Color * (1 - a * a);
}

technique Render
{
    pass Pass1
    {
        AlphaBlendEnable = True;
        DestBlend = InvSrcAlpha;
        SrcBlend = SrcAlpha;
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
