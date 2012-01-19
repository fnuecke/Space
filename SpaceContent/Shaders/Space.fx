uniform extern texture Stars;
uniform extern texture DarkMatter;
uniform extern texture DebrisSmall;
uniform extern texture DebrisLarge;

uniform extern float4 DebrisSmallTint = 1;
uniform extern float4 DebrisLargeTint = 1;

uniform extern float2 Position = 0;

const float darkMatterAlpha = 0.95;
const float debrisSmallAlpha = 0.75;
const float debrisLargeAlpha = 0.25;

const float starSpeed = 0.05;
const float darkMatterSpeed = 0.1;
const float debrisSmallSpeed = 0.65;
const float debrisLargeSpeed = 0.95;

SamplerState starSampler = sampler_state
{
    Texture = <Stars>;
    MipFilter = Linear;
    MagFilter = Anisotropic;
    MinFilter = Anisotropic;
    MaxAnisotropy = 8;
    AddressU = Wrap;
    AddressV = Wrap;
};

SamplerState darkMatterSampler = sampler_state
{
    Texture = <DarkMatter>;
    MipFilter = Linear;
    MagFilter = Anisotropic;
    MinFilter = Anisotropic;
    MaxAnisotropy = 8;
    AddressU = Wrap;
    AddressV = Wrap;
};

SamplerState debrisSmallSampler = sampler_state
{
    Texture = <DebrisSmall>;
    MipFilter = Linear;
    MagFilter = Anisotropic;
    MinFilter = Anisotropic;
    MaxAnisotropy = 8;
    AddressU = Wrap;
    AddressV = Wrap;
};

SamplerState debrisLargeSampler = sampler_state
{
    Texture = <DebrisLarge>;
    MipFilter = Linear;
    MagFilter = Anisotropic;
    MinFilter = Anisotropic;
    MaxAnisotropy = 8;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VertexShaderData
{
    float4 Position : POSITION0;
    float2 TextureCoordinate : TEXCOORD0;
};

VertexShaderData VertexShaderFunction(VertexShaderData input)
{
    return input;
}

float4 StarShader(VertexShaderData input) : COLOR0
{
    return tex2D(starSampler, input.TextureCoordinate + Position * starSpeed);
}

float4 DarkMatterShader(VertexShaderData input) : COLOR0
{
    return tex2D(darkMatterSampler, input.TextureCoordinate + Position * darkMatterSpeed) * darkMatterAlpha;
}

float4 DebrisSmallShader(VertexShaderData input) : COLOR0
{
    return tex2D(debrisSmallSampler, input.TextureCoordinate + Position * debrisSmallSpeed) * debrisSmallAlpha * DebrisSmallTint;
}

float4 DebrisLargeShader(VertexShaderData input) : COLOR0
{
    return tex2D(debrisLargeSampler, input.TextureCoordinate + Position * debrisLargeSpeed) * debrisLargeAlpha * DebrisLargeTint;
}

technique Background
{
    pass Stars
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 StarShader();
    }
    pass DarkMatter
    {
        AlphaBlendEnable = True;
		DestBlend = InvSrcAlpha;
        PixelShader = compile ps_2_0 DarkMatterShader();
    }
    pass DebrisSmall
    {
        BlendOp = Add;
		DestBlend = One;
        PixelShader = compile ps_2_0 DebrisSmallShader();
    }
    pass DebrisLarge
    {
        PixelShader = compile ps_2_0 DebrisLargeShader();
    }
}
