// ------------------------------------------------------------------------- //

uniform extern float4 Color;

uniform extern texture Surface;
uniform extern texture TurbulenceOne;
uniform extern texture TurbulenceTwo;
uniform extern texture TurbulenceColor;

uniform extern float RenderRadius = 1;

uniform extern float2 SurfaceOffset;
uniform extern float2 TurbulenceOneOffset;
uniform extern float2 TurbulenceTwoOffset;
uniform extern float TextureScale = 1;

// ------------------------------------------------------------------------- //

SamplerState baseSampler = sampler_state
{
    Texture = <Surface>;
    MipFilter = Linear;
    MagFilter = Anisotropic;
    MinFilter = Anisotropic;
    MaxAnisotropy = 16;
    AddressU = Wrap;
    AddressV = Wrap;
};

SamplerState turbulenceOneSampler = sampler_state
{
    Texture = <TurbulenceOne>;
    MipFilter = Linear;
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

SamplerState turbulenceTwoSampler = sampler_state
{
    Texture = <TurbulenceTwo>;
    MipFilter = Linear;
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

SamplerState gradientSampler = sampler_state
{
    Texture = <TurbulenceColor>;
    MipFilter = Linear;
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap; 
};

// ------------------------------------------------------------------------- //

struct VertexShaderData
{
    float4 Position : POSITION0;
    float2 TextureCoordinate : TEXCOORD0;
};

VertexShaderData VertexShaderFunction(VertexShaderData input)
{
    return input;
}

// ------------------------------------------------------------------------- //

float4 SurfaceShader(VertexShaderData input) : COLOR0
{
    float2 p = input.TextureCoordinate;
    float r = dot(p, p);
    if (r > 1)
    {
        // Outside the sphere, do nothing.
        return 0;
    }
    else
    {
        // Compute the spherized coordinate of the pixel.
        float f = (1 - sqrt(1 - r)) / r;
        float2 uvSphere = (p * f + 1) / 2 + SurfaceOffset;

        // Actual color at position.
        float4 color = tex2D(baseSampler, uvSphere / TextureScale) * Color;
        
        // Alpha for smoother border.
        float alpha = clamp((1 - r) * RenderRadius, 0, 1);
        return color * alpha;
    }
}

// ------------------------------------------------------------------------- //

const float Threshold = 0.25;
const float Brightness = 8;

float Luminance( float4 Color )
{
    return
        0.3 * Color.r +
        0.6 * Color.g +
        0.1 * Color.b;
}

float4 TurbulenceShader(VertexShaderData input) : COLOR0
{
    float2 p = input.TextureCoordinate;

    // Also get a version mapped down even further, to allow the
    // atmosphere to overflow the planet in the freed area.
    //float2 pInner = pOuter / (1 - relativeAtmosphereArea);

    float r = dot(p, p);

    if (r > 1)
    {
        // Outside the sphere, do nothing.
        return 0;
    }
    else
    {
        // Compute the spherized coordinate of the pixel.
        float f = (1 - sqrt(1 - r)) / r;
        float2 uvSphereBase = (p * f + 1) / 2 + SurfaceOffset;
        float2 uvSphereOne = (p * f + 1) / 2 + TurbulenceOneOffset;
        float2 uvSphereTwo = (p * f + 1) / 2 + TurbulenceTwoOffset;

        // Actual colors at position.
        float4 color = tex2D(baseSampler, uvSphereBase / TextureScale) * 2 * Color +
                       tex2D(turbulenceOneSampler, uvSphereOne / TextureScale) +
                       tex2D(turbulenceTwoSampler, uvSphereTwo / TextureScale) - 2;
        
        float lum = Luminance( color );
        if( lum >= Threshold )
        {
            float pos = lum - Threshold;
            if( pos > 0.98 ) pos = 0.98;
            if( pos < 0.02 ) pos = 0.02;

            //color = tex2D( gradientSampler, pos ) * Brightness * Color;
            color = pos * Brightness * Color;

            return float4( color.rgb, 1 );
        }
        else
        {
            return float4( 0.0, 0.0, 0.0, 1.0 );
        }
    }
}

// ------------------------------------------------------------------------- //

technique Sun
{
    pass Base
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 SurfaceShader();
    }
    pass Turbulence
    {
        PixelShader = compile ps_2_0 TurbulenceShader();
    }
}