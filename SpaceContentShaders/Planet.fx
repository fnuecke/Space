uniform extern texture SurfaceTexture;
uniform extern texture SurfaceNormals;
uniform extern bool HasNormals = false;
uniform extern texture SurfaceLights;
uniform extern bool HasLights = false;

uniform extern float4 SurfaceTint = 1;
uniform extern float4 AtmosphereTint = 1;

uniform extern float2 LightDirection = 0;
uniform extern float TextureOffset = 0;
uniform extern float RenderRadius = 1;
uniform extern float HorizontalScale = 1;

uniform extern float AtmosphereOuter = 0.1;
uniform extern float AtmosphereInner = 0.4;

uniform extern float AtmosphereInnerAlpha = 0.85;
uniform extern float AtmosphereOuterAlpha = 1;

// ------------------------------------------------------------------------- //

const float relativeAtmosphereArea = 0.05;

// ------------------------------------------------------------------------- //

SamplerState textureSampler = sampler_state
{
    Texture = <SurfaceTexture>;
    MipFilter = Linear;
    MagFilter = Anisotropic;
    MinFilter = Anisotropic;
    MaxAnisotropy = 16;
    AddressU = Clamp;
    AddressV = Clamp;
};

SamplerState lightsSampler = sampler_state
{
    Texture = <SurfaceLights>;
    MipFilter = Linear;
    MagFilter = Anisotropic;
    MinFilter = Anisotropic;
    MaxAnisotropy = 4;
    AddressU = Clamp;
    AddressV = Clamp;
};

SamplerState normalsSampler = sampler_state
{
    Texture = <SurfaceNormals>;
    MipFilter = Linear;
    MagFilter = Anisotropic;
    MinFilter = Anisotropic;
    MaxAnisotropy = 8;
    AddressU = Clamp;
    AddressV = Clamp;
};

// ------------------------------------------------------------------------- //

struct VSData
{
    float4 Position : POSITION0;
    float2 TextureCoordinate : TEXCOORD0;
};

VSData VertexShaderFunction(VSData input)
{
    return input;
}

// ------------------------------------------------------------------------- //

// Used to get dark tones for shadow rendering.
float3 threshold_low(float3 rgb, float level)
{
    float intensity = (rgb.r + rgb.g + rgb.b) / 3.0;
    if (intensity < level)
    {
        return rgb;
    }
    else
    {
        return 1;
    }
}

// Used to get bright tones for highlight rendering.
float3 threshold_high(float3 rgb, float level)
{
    float intensity = (rgb.r + rgb.g + rgb.b) / 3.0;
    if (intensity > level)
    {
        return rgb;
    }
    else
    {
        return 0;
    }
}

// Used to make sure texture coordinates are within [0, 1].
float2 wrap(float2 v) {
    if (v.x < 0) {
        v.x += 1;
    }
    if (v.y < 0) {
        v.y += 1;
    }
    if (v.x >= 1) {
        v.x -= 1;
    }
    if (v.x >= 1) {
        v.x -= 1;
    }
    return v;
}

// Gets basic variables used in all pixel shaders.
void getOffsets(float2 t, inout float2 pOuter, inout float2 pInner, inout float rInner, inout float rOffset) {
    // Outer point.
    pOuter = t;
    // Also get a version mapped down even further, to allow the
    // atmosphere to overflow the planet in the freed area.
    pInner = pOuter / (1 - relativeAtmosphereArea);
    // And lastly, the same thing, but offset away from the light
    // source, which we'll use for shadowing.
    float2 pOffset = pInner + LightDirection * 0.5;

    // Compute the squared radii for both variants.
    rInner = dot(pInner, pInner);
    rOffset = dot(pOffset, pOffset);
    // Increase the size of the shadow a bit, but steepen the fall-off.
    rOffset *= 0.5;
    rOffset *= rOffset;
}

// ------------------------------------------------------------------------- //

// Renders the planet surface.
float4 SurfacePS(VSData input) : COLOR0
{
    float2 pOuter, pInner;
    float rInner, rOffset;
    getOffsets(input.TextureCoordinate, pOuter, pInner, rInner, rOffset);

    if (rInner > 1)
    {
        // Outside the sphere, do nothing.
        discard;
    }

    // Compute the spherized coordinate of the pixel.
    float f = (1 - sqrt(1 - rInner)) / rInner;
    float2 uvSphere = (pInner * f + 1) / 2;
    uvSphere.x = ((uvSphere.x + TextureOffset) * HorizontalScale) % 1;
    uvSphere = wrap(uvSphere);

    // Actual color at position.
    float4 color = tex2D(textureSampler, uvSphere) * SurfaceTint;
    if (HasNormals) {
        color.rgb *= 4 * dot((tex2D(normalsSampler, uvSphere) - 0.5), LightDirection) + 1;
    }

    // Self-shadowing based on light source position.
    color.rgb *= saturate(rOffset);

    // Alpha for smoother border.
    float alpha = clamp(0.5 * (1 - rInner) * RenderRadius, 0, 1);
    return float4(color.rgb, alpha);
}

// ------------------------------------------------------------------------- //

// Renders the atmosphere on top of the planet.
float4 LightsPS(VSData input) : COLOR0
{
    if (!HasNormals) {
        discard;
    }

    float2 pOuter, pInner;
    float rInner, rOffset;
    getOffsets(input.TextureCoordinate, pOuter, pInner, rInner, rOffset);

    if (rInner > 1)
    {
        // Outside the sphere, do nothing.
        discard;
    }

    // Compute the spherized coordinate of the pixel.
    float f = (1 - sqrt(1 - rInner)) / rInner;
    float2 uvSphere = (pInner * f + 1) / 2;
    uvSphere.x = ((uvSphere.x + TextureOffset) * 0.5) % 1;
    uvSphere = wrap(uvSphere);

    // Actual lights at position.
    float4 color = tex2D(lightsSampler, uvSphere);

    // Show lights where there is shadow.
    color.rgb *= 1 - saturate(rOffset);

    return color;
}

// ------------------------------------------------------------------------- //

// Renders the atmosphere on top of the planet.
float4 AtmospherePS(VSData input) : COLOR0
{
    float2 pOuter, pInner;
    float rInner, rOffset;
    getOffsets(input.TextureCoordinate, pOuter, pInner, rInner, rOffset);

    // Outside of unit circle, outer atmosphere.
    float rOuter = dot(pOuter, pOuter) - 1;
    float atmosphere = rOuter / AtmosphereOuter + 1;
    float3 color = saturate(AtmosphereTint.rgb * (1 - atmosphere * atmosphere));

    if (rInner > 1)
    {
        // Self-shadowing based on light source position, use to erase
        // some of the atmosphere.
        return float4(color * saturate(rOffset) * AtmosphereTint.a * AtmosphereOuterAlpha, 1);
    }

    // Inside the circle, get inner atmosphere.
    atmosphere = rOuter * AtmosphereInner + 1;
    color = 1 - (1 - color) * saturate(1 - AtmosphereTint.rgb * (1 - atmosphere * atmosphere));

    // Self-shadowing based on light source position.
    color *= saturate(rOffset);

    // Screen with the opposite.
    color = 1 - ((1 - color) * (1 - saturate(rOffset * rOffset * rOffset) * 0.15));

    return float4(color * AtmosphereTint.a * AtmosphereInnerAlpha, 1);
}

// ------------------------------------------------------------------------- //

technique Planet
{
    pass Surface
    {
        AlphaBlendEnable = True;
        DestBlend = InvSrcAlpha;
        SrcBlend = SrcAlpha;
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 SurfacePS();
    }
    pass Lights
    {
        AlphaBlendEnable = True;
        DestBlend = One;
        SrcBlend = One;
        PixelShader = compile ps_2_0 LightsPS();
    }
    pass Atmosphere
    {
        AlphaBlendEnable = True;
        DestBlend = One;
        SrcBlend = One;
        PixelShader = compile ps_2_0 AtmospherePS();
    }
}
