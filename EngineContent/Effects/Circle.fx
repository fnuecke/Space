uniform extern float4 Color = float4(0.25, 1, 0, 1);
uniform extern float Thickness = 1;

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
    float a = (input.TextureCoordinate.x * input.TextureCoordinate.x +
               input.TextureCoordinate.y * input.TextureCoordinate.y - 1) / Thickness + 1;
    return Color * (1 - a * a);
}

technique Render
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
