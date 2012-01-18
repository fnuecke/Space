uniform extern float4 Color = float4(1, 0, 1, 1);
uniform extern float Gradient = 1;

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
    float a = clamp((1 - max(abs(input.TextureCoordinate.x), abs(input.TextureCoordinate.y))) / Gradient, 0, 1);
    return Color * a;
}

technique Render
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
