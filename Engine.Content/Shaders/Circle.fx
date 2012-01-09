uniform extern float4 Color = float4(0.25, 1, 0, 1);
uniform extern float Thickness = 1;

struct QuadVertex
{
    float4 Position : POSITION0;
    float2 Tex0 : TexCoord0;
};

//////////////////////////////////////////////////////////////////////////////////////////////////////////

// common vertex shader, simple passthrough
QuadVertex ShadeVertex(QuadVertex input)
{
    return input;
}

// simple pixel shader to render the texture
float4 ShadePixel(QuadVertex input) : COLOR0
{
	float a = (input.Tex0.x * input.Tex0.x + input.Tex0.y * input.Tex0.y - 1) / Thickness + 1;
    return Color * (1 - a * a);
}

// basic technique to render the texture
technique Render
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 ShadeVertex();
        PixelShader = compile ps_2_0 ShadePixel();
    }
}
