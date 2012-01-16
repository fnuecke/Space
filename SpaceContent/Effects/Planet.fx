uniform extern texture ScreenTexture;
uniform extern float DisplaySize;
uniform extern float TextureSize;

uniform extern float2 TextureOffset = 0;
uniform extern float TextureScale = 1;
uniform extern float2 LightDirection = 0;
uniform extern float4 PlanetTint = 1;
uniform extern float4 AtmosphereTint = 1;

const float relativeAtmosphereArea = 0.05;
const float relativeOuterAtmosphere = 0.1;
const float relativeInnerAtmosphere = 0.4;

const float outerAtmosphereAlpha = 0.45;
const float innerAtmosphereAlpha = 0.35;

SamplerState textureSampler = sampler_state
{
    Texture = <ScreenTexture>;
    MipFilter = Linear;
    MagFilter = Anisotropic;
    MinFilter = Anisotropic;
    MaxAnisotropy = 16;
    AddressU = Wrap;
    AddressV = Wrap;
};

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

// Renders the planet surface.
float4 PlanetShaderFunction(float2 uv : TEXCOORD0) : COLOR
{
    // Map position to [-1,1].
    float2 pOuter = 2 * uv - 1;
    // Also get a version mapped down even further, to allow the
    // atmosphere to overflow the planet in the freed area.
    float2 pInner = pOuter / (1 - relativeAtmosphereArea);
    // And lastly, the same thing, but offset away from the light
    // source, which we'll use for shadowing.
    float2 pOffset = pInner + LightDirection * 0.5;

    // Compute the squared radii for both variants.
    float rInner = dot(pInner, pInner);
    float rOffset = dot(pOffset, pOffset);
    // Increase the size of the shadow a bit, but steepen the fall-off.
    rOffset *= 0.5;
    rOffset *= rOffset;

    if (rInner > 1)
    {
        // Outside the sphere, do nothing.
        return 0;
    }
    else
    {
        // Compute the spherized coordinate of the pixel.
        float f = (1 - sqrt(1 - rInner)) / rInner;
        float2 uvSphere = (pInner * f + 1) / 2 + TextureOffset;

        // Actual color at position.
        float4 color = tex2D(textureSampler, uvSphere / TextureScale) * PlanetTint;

        // Emboss effect.
        float4 relief = float4(0.5, 0.5, 0.5, 1);
        float embossOffset = 1.0 / TextureSize;
        relief.rgb -= tex2D(textureSampler, (uvSphere + LightDirection * embossOffset) / TextureScale).rgb * 2;
        relief.rgb += tex2D(textureSampler, (uvSphere - LightDirection * embossOffset) / TextureScale).rgb * 2;
        // Make in monochrome.
        relief.rgb = (relief.r + relief.g + relief.b) / 3.0;

        // Get the 'shadows', which are the especially dark regions.
        float3 shadows = relief.rgb;
        shadows = threshold_low(shadows, 0.5);
        shadows = saturate(shadows + 0.5);
        // Multiply them to the base color.
        color.rgb = color.rgb * shadows;

        // Get the 'lights', which are the especially bright regions.
        float3 lights = relief.rgb;
        lights = threshold_high(lights, 0.65);
        lights *= 0.2;
        // Screen them to the base color.
        color.rgb = 1 - (1 - color.rgb) * (1 - lights);
        
        // Self-shadowing based on light source position.
        color.rgb *= saturate(rOffset);

        // Alpha for smoother border.
        float alpha = clamp((1 - rInner) * DisplaySize, 0, 1);
        return color * alpha;
    }
}

// Renders the atmosphere on top of the planet.
float4 AtmosphereShaderFunction(float2 uv : TEXCOORD0) : COLOR
{
    // Map position to [-1,1].
    float2 pOuter = 2 * uv - 1;
    // Also get a version mapped down even further, to allow the
    // atmosphere to overflow the planet in the freed area.
    float2 pInner = pOuter / (1 - relativeAtmosphereArea);
    // And lastly, the same thing, but offset away from the light
    // source, which we'll use for shadowing.
    float2 pOffset = pInner + LightDirection * 0.5;

    // Compute the squared radii for both variants.
    float rOuter = dot(pOuter, pOuter) - 1;
    float rInner = dot(pInner, pInner);
    float rOffset = dot(pOffset, pOffset);
    // Increase the size of the shadow a bit, but steepen the fall-off.
    rOffset *= 0.5;
    rOffset *= rOffset;

    // Outside of unit circle, outer atmosphere.
    float atmosphere = rOuter / relativeOuterAtmosphere + 1;
    float4 color = saturate(AtmosphereTint * outerAtmosphereAlpha * (1 - atmosphere * atmosphere));

    if (rInner > 1)
    {
        // Self-shadowing based on light source position, use to erase
        // some of the atmosphere.
        return color * saturate(rOffset);
    }

    // Inside the circle, get inner atmosphere.
    atmosphere = rOuter * relativeInnerAtmosphere + 1;
    color = 1 - (1 - color) * saturate(1 - AtmosphereTint * innerAtmosphereAlpha * (1 - atmosphere * atmosphere));
    
    // Self-shadowing based on light source position.
    color.rgb *= saturate(rOffset);

    // Screen with the opposite.
    rOffset = rOffset * rOffset * rOffset;
    color.rgb = 1 - ((1 - color.rbg) * (1 - saturate(rOffset) * 0.15));

    return color;
}

technique Render
{
    pass Planet
    {
        PixelShader = compile ps_2_0 PlanetShaderFunction();
    }
    pass Atmosphere
    {
        AlphaBlendEnable = True;
        DestBlend = One;
        PixelShader = compile ps_2_0 AtmosphereShaderFunction();
    }
}
