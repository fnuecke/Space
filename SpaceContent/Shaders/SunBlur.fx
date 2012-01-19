uniform extern texture ScreenTexture;
uniform extern float TextureSize;

// ------------------------------------------------------------------------- //

SamplerState gaussBlurSampler = sampler_state
{
    Texture = <ScreenTexture>;
    MipFilter = Linear;
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

const float weights3x3[3][3] = {
    { 0.0625, 0.1250, 0.0625 },
    { 0.1250, 0.2500, 0.1250 },
    { 0.0625, 0.1250, 0.0625 }
};

// ------------------------------------------------------------------------- //

float4 GaussianBlurShader(float2 uv: TEXCOORD0) : COLOR0
{
    float offsetX = (1.0 / TextureSize) * 1.5;
    float offsetY = (1.0 / TextureSize) * 1.5;
    float4 color = (float4)0;
    float2 position;
    
    for (int y = 0; y < 3; ++y)
    {
        for (int x = 0; x < 3; ++x)
        {
            position.x = uv.x + (x - 1) * offsetX;
            position.y = uv.y + (y - 1) * offsetY;
            color += tex2D(gaussBlurSampler, position) * weights3x3[x][y];
        }
    }

    return float4(color.rgb, 1);
}

// ------------------------------------------------------------------------- //

technique GaussianBlur
{
    pass Pass0
    {
        PixelShader = compile ps_2_0 GaussianBlurShader();
    }
}