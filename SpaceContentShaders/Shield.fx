uniform extern float4 Color = float4(1, 0, 1, 1);
uniform extern float Coverage = 1;
uniform extern texture Structure;
uniform extern bool HasStructure;
uniform extern float MinAlpha = 0.2;
uniform extern float StructureRotation = 0;
uniform extern float RenderRadius = 1;

SamplerState structureSampler = sampler_state
{
    Texture = <Structure>;
    MipFilter = Linear;
    MagFilter = Anisotropic;
    MinFilter = Anisotropic;
    MaxAnisotropy = 8;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VSIn
{
    float4 Position : POSITION0;
    float2 TextureCoordinate : TEXCOORD0;
};

struct VSOut
{
    float4 Position : POSITION0;
    float2 TextureCoordinate : TEXCOORD0;
    float2 StructureCoordinate : TEXCOORD1;
};

VSOut VertexShaderFunction(VSIn input)
{
    VSOut output;
    output.Position = input.Position;
    output.TextureCoordinate = input.TextureCoordinate;

    float cosRadians, sinRadians;
    sincos(StructureRotation, cosRadians, sinRadians);

    output.StructureCoordinate.x = input.TextureCoordinate.x * cosRadians - input.TextureCoordinate.y * sinRadians;
    output.StructureCoordinate.y = input.TextureCoordinate.x * sinRadians + input.TextureCoordinate.y * cosRadians;

    return output;
}

float4 PixelShaderFunction(VSOut input) : COLOR0
{
    // Get intensity as a falloff, strongest at the edge, weakest in the middle.
    float rInner = dot(input.TextureCoordinate, input.TextureCoordinate);
    if (rInner > 1) {
        // Outside the circle, bail.
        discard;
    }

    // See if we're in the covered angle (plus a buffer for fading out
    // in case we're drawing).
    float angle = abs(atan2(input.TextureCoordinate.y, input.TextureCoordinate.x));
    if (angle > Coverage + 0.3) {
        discard;
    }

    // Fade out towards sides.
    angle = min(1, 1 - (angle - Coverage) / 0.3);

    // See if we have some structure.
    float4 color = Color;
    if (HasStructure) {
        // We got some structure, compute the spherized coordinate of the pixel.
        float f = (1 - sqrt(1 - rInner)) / rInner;
        float2 uvSphere = (input.StructureCoordinate * 0.5 * f + 1);
        // Front side of the sphere.
        float front = tex2D(structureSampler, uvSphere - 0.5).r;
        // Back side of the sphere.
        float back = tex2D(structureSampler, -uvSphere - 0.5).r;
        // Apply front and back (front just weighted stronger).
        color *= 0.7 * front + 0.3 * back;
    }

    // Alpha for smoother border and scaled to minimum opacity.
    float alpha = clamp(0.5 * (1 - rInner) * RenderRadius, 0, 1);

    // Scale to minimum opacity.
    rInner = MinAlpha + rInner * (1 - MinAlpha);

    return float4(color.rgb, color.a * alpha * rInner * angle);
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
