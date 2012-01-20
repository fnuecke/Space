uniform extern texture ScreenTexture;
uniform extern float ScaleFactor = 1.075;

// ------------------------------------------------------------------------- //

SamplerState blendSampler = sampler_state
{
    Texture = <ScreenTexture>;
    MipFilter = Linear;
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

// ------------------------------------------------------------------------- //

float4 AdditiveBlendShader(float2 uv: TEXCOORD0) : COLOR0
{
    float3 c0 = tex2D(blendSampler, uv).rgb;
    return float4(c0 * ScaleFactor, 1.0);
}

// ------------------------------------------------------------------------- //

technique AdditiveBlend
{
    pass Pass0
    {
        AlphaBlendEnable = True;
        DestBlend = One;
        SrcBlend = One;
        PixelShader = compile ps_2_0 AdditiveBlendShader();
    }
}