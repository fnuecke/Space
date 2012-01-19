#define MAX_POINTS 3

uniform extern float4 Color = float4(1, 0, 1, 1);
uniform extern int NumValues;
uniform extern float4 Colors[MAX_POINTS];
uniform extern float Points[MAX_POINTS];

uniform extern float Gradient;

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
    // Abort if we have no gradient points.
    if (NumValues <= 0)
    {
        return float4(1, 0, 1, 1);
    }

    // Get the general alpha at this point.
    float a = clamp((1 - max(abs(input.TextureCoordinate.x), abs(input.TextureCoordinate.y))) / Gradient, 0, 1);

    // Get our x texture coordianate in an interval of [0, 1].
    float u = (input.TextureCoordinate.y + 1) * 0.5;

    // Find our interval.
    for (int i = 0; i < NumValues - 1; ++i)
    {
        float next = Points[i + 1];
        if (next > u)
        {
            // Found it. Interpolate to next one.
            u = (u - Points[i]) / (next - Points[i]);

            return lerp(Colors[i], Colors[i + 1], u) * a;
        }
    }

    // If we got here, we're past the last color.
    return Colors[NumValues - 1];
}

technique Render
{
    pass Pass1
    {
        AlphaBlendEnable = True;
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
