uniform extern float4 Color = float4(1, 0, 1, 1);
uniform extern float2 GridSmall = float2(16, 16);
uniform extern float2 GridLarge = float2(5, 5);

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
    float x = abs((abs(input.TextureCoordinate.x) % (GridSmall.x * 2)) - GridSmall.x) / GridSmall.x;
    float y = abs((abs(input.TextureCoordinate.y) % (GridSmall.y * 2)) - GridSmall.y) / GridSmall.y;
    float xt = abs((abs(input.TextureCoordinate.x) % (GridLarge.x * 2)) - GridLarge.x) / GridLarge.x;
    float yt = abs((abs(input.TextureCoordinate.y) % (GridLarge.y * 2)) - GridLarge.y) / GridLarge.y;
    float xy = max(x, y);
    xy = xy * xy;
    xy = xy * xy;
    float xyt = max(xt, yt);
    xyt = xyt * xyt;
    xyt = xyt * xyt;
    xyt = xyt * xyt;
    xyt = xyt * xyt;
    xyt = xyt * xyt;
    xyt = xyt * xyt;
    return Color * (xy * 2 + xyt) * 0.25;
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
