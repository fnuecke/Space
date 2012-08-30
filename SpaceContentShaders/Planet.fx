uniform extern texture SurfaceTexture;
uniform extern texture SurfaceSpecular;
uniform extern texture SurfaceNormals;
uniform extern texture SurfaceLights;
uniform extern texture CloudTexture;
uniform extern bool HasNormals = false;

uniform extern float4 SurfaceTint = 1;
uniform extern float4 AtmosphereTint = 1;

uniform extern float2 LightDirection = 0;
uniform extern float TextureOffset = 0;
uniform extern float RenderRadius = 1;
uniform extern float HorizontalScale = 1;
uniform extern float SpecularAlpha = 1;
uniform extern float SpecularExponent = 10;
uniform extern float SpecularOffset = 1;

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

SamplerState specularSampler = sampler_state
{
    Texture = <SurfaceSpecular>;
    MipFilter = Linear;
    MagFilter = Linear;
    MinFilter = Linear;
    //MaxAnisotropy = 4;
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

SamplerState cloudSampler = sampler_state
{
    Texture = <CloudTexture>;
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
    float2 uvCloud = uvSphere;
    uvSphere.x = ((uvSphere.x + TextureOffset) * HorizontalScale) % 1;
    uvSphere = wrap(uvSphere);

    // Actual color at position.
    float4 color = tex2D(textureSampler, uvSphere) * SurfaceTint;
    if (HasNormals) {
        color.rgb *= 4 * dot((tex2D(normalsSampler, uvSphere) - 0.5), LightDirection) + 1;
    }

    // Get cloud shadow.
    uvCloud += LightDirection / RenderRadius;
    uvCloud.x = ((uvCloud.x - TextureOffset) * HorizontalScale) % 1;
    uvCloud = wrap(uvCloud);
    float cloud = 1 - tex2D(cloudSampler, uvCloud).a;

    // Self-shadowing based on light source position.
    color.rgb *= saturate(rOffset) * cloud;

    // Alpha for smoother border.
    float alpha = clamp(0.5 * (1 - rInner) * RenderRadius, 0, 1);
    return float4(color.rgb, alpha);
}

// ------------------------------------------------------------------------- //

// Renders specular highlight.
float4 SpecularPS(VSData input) : COLOR0
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

    // Specular highlight.
    float x = (uvSphere.x - 0.5);
    float y = (uvSphere.y - 0.5);
    float z = 1 - 2 * sqrt(x * x + y * y);
    float3 uv3d = float3(2 * (uvSphere - 0.5), z);
    uv3d = normalize(uv3d);
    float3 l3d = float3(LightDirection, SpecularOffset);
    l3d = normalize(l3d);

    float specular = dot(uv3d, l3d);
    if (specular <= 0) {
        discard;
    }

    uvSphere.x = ((uvSphere.x + TextureOffset) * HorizontalScale) % 1;
    uvSphere = wrap(uvSphere);
    specular = pow(specular, SpecularExponent) * tex2D(specularSampler, uvSphere).r * SpecularAlpha;

    float alpha = clamp(0.5 * (1 - rInner) * RenderRadius, 0, 1);
    return float4(specular, specular, specular, alpha);
}

// ------------------------------------------------------------------------- //

// Renders the atmosphere on top of the planet.
float4 LightsPS(VSData input) : COLOR0
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
    uvSphere.x = ((uvSphere.x + TextureOffset) * 0.5) % 1;
    uvSphere = wrap(uvSphere);

    // Actual lights at position.
    float4 color = tex2D(lightsSampler, uvSphere);

    // Show lights where there is shadow.
    color.rgb *= 1 - saturate(rOffset);

    float alpha = clamp(0.5 * (1 - rInner) * RenderRadius, 0, 1);
    return float4(color.rgb, alpha);
}

// ------------------------------------------------------------------------- //

// Renders clouds.
float4 CloudsPS(VSData input) : COLOR0
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
    uvSphere.x = ((uvSphere.x - TextureOffset) * HorizontalScale) % 1;
    uvSphere = wrap(uvSphere);

    // Actual color at position.
    float4 color = tex2D(cloudSampler, uvSphere);

    // Alpha for smoother border.
    float alpha = clamp(0.5 * (1 - rInner) * RenderRadius, 0, 1) * color.a;

    // Self-shadowing based on light source position.
    color = 2 * color * saturate(rOffset);

    return float4(color.rgb, alpha);
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

    float alpha = clamp(0.5 * (1 - rInner) * RenderRadius, 0, 1);
    return float4(color * AtmosphereTint.a * AtmosphereInnerAlpha, alpha);
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
    pass Specular
    {
        AlphaBlendEnable = True;
        DestBlend = One;
        SrcBlend = One;
        PixelShader = compile ps_2_0 SpecularPS();
    }
    pass Lights
    {
        AlphaBlendEnable = True;
        DestBlend = One;
        SrcBlend = One;
        PixelShader = compile ps_2_0 LightsPS();
    }
    pass Clouds
    {
        AlphaBlendEnable = True;
        DestBlend = InvSrcAlpha;
        SrcBlend = SrcAlpha;
        PixelShader = compile ps_2_0 CloudsPS();
    }
    pass Atmosphere
    {
        AlphaBlendEnable = True;
        DestBlend = One;
        SrcBlend = One;
        PixelShader = compile ps_2_0 AtmospherePS();
    }
}
