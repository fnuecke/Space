uniform extern float2 Offset = 0;
uniform extern float Scale = 1;
uniform extern float2 LightDirection = 0;
uniform extern float4 AtmosphereColor = 0;
uniform extern int TextureSize;
uniform extern texture ScreenTexture;

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

float4 PlanetShaderFunction(float2 uv : TEXCOORD0) : COLOR
{
    // First cut out the image, only compute more for pixels actually displayed.
    float2 pOuter = 2 * uv - 1;
	float rOuter = dot(pOuter, pOuter);
    float2 pInner = pOuter / (1 - (16.0 / TextureSize));
    float rInner = dot(pInner, pInner);
	
	float2 pOffset = 2 * (uv + LightDirection * 0.25) - 1;
	float rOffset = dot(pOffset, pOffset);

    // What's worse, flow control ord the remaining computations? Hmm...
    if (rInner >= 1)
    {
        // Outside of unit circle, only atmosphere.
		float atmosphere = (rOuter - 1) * TextureSize / 64 + 1;
		float4 color = AtmosphereColor * 0.55 * (1 - atmosphere * atmosphere);

		// Self-shadowing based on light source position, use to erase equality.
		rOffset *= 0.5;
		rOffset *= rOffset;
		color -= (1 - saturate(rOffset)) * 0.5;

		return color;
    }
	else
	{
		// Compute the spherized coordinate of the pixel.
		float f = (1 - sqrt(1 - rInner)) / rInner;
		float2 uvSphere = (pInner * f + 1) / 2 + Offset;

		// Emboss effect.
		float4 relief = float4(0.5, 0.5, 0.5, 1);
		float embossOffset = 1.0 / TextureSize;
		relief.rgb -= tex2D(textureSampler, (uvSphere + LightDirection * embossOffset) / Scale).rgb * 2;
		relief.rgb += tex2D(textureSampler, (uvSphere - LightDirection * embossOffset) / Scale).rgb * 2;
		// Make in monochrome.
		relief.rgb = (relief.r + relief.g + relief.b) / 3.0;

		// Actual color at position.
		float4 color = tex2D(textureSampler, uvSphere / Scale);

		// Multiply with shadows.
		float3 shadows = relief.rgb;
		shadows = threshold_low(shadows, 0.5);
		shadows = saturate(shadows + 0.5);
		color.rgb = color.rgb * shadows;

		// Screen with lights.
		float3 lights = relief.rgb;
		lights = threshold_high(lights, 0.65);
		lights *= 0.2;
		color.rgb = 1 - (1 - color.rgb) * (1 - lights);

		// Alpha for smoother border.
		float alpha = clamp((1 - rInner) * (TextureSize / 8), 0, 1);

		// Atmosphere.
		float atmosphere = (rOuter - 1) * TextureSize / 80 + 1;
		float4 atmosphereColor = AtmosphereColor * 0.45 * (1 - atmosphere * atmosphere);
		color = 1 - (1 - color) * (1 - atmosphereColor);
		color = lerp(atmosphereColor, color, alpha);

		// Self-shadowing based on light source position.
		rOffset *= 0.5;
		rOffset *= rOffset;
		color.rgb *= saturate(rOffset);

		// Screen with the opposite.
		rOffset *= rOffset;
		color.rgb = 1 - ((1 - color.rbg) * (1 - saturate(rOffset)));

		return color;
	}
}


float4 AtmosphereShaderFunction(float2 uv : TEXCOORD0) : COLOR
{
    // First cut out the image, only compute more for pixels actually displayed.
    float2 pOuter = 2 * uv - 1;
	float rOuter = dot(pOuter, pOuter);
    float2 pInner = pOuter / (1 - (16.0 / TextureSize));
    float rInner = dot(pInner, pInner);
	
	float2 pOffset = 2 * (uv + LightDirection * 0.25) - 1;
	float rOffset = dot(pOffset, pOffset);

    // What's worse, flow control ord the remaining computations? Hmm...
    if (rInner >= 1)
    {
        // Outside of unit circle, only atmosphere.
		float atmosphere = (rOuter - 1) * TextureSize / 64 + 1;
		float4 color = AtmosphereColor * 0.55 * (1 - atmosphere * atmosphere);

		// Self-shadowing based on light source position, use to erase equality.
		rOffset *= 0.5;
		rOffset *= rOffset;
		color -= (1 - saturate(rOffset)) * 0.5;

		return color;
    }
	else
	{
		// Compute the spherized coordinate of the pixel.
		float f = (1 - sqrt(1 - rInner)) / rInner;
		float2 uvSphere = (pInner * f + 1) / 2 + Offset;

		// Emboss effect.
		float4 relief = float4(0.5, 0.5, 0.5, 1);
		float embossOffset = 1.0 / TextureSize;
		relief.rgb -= tex2D(textureSampler, (uvSphere + LightDirection * embossOffset) / Scale).rgb * 2;
		relief.rgb += tex2D(textureSampler, (uvSphere - LightDirection * embossOffset) / Scale).rgb * 2;
		// Make in monochrome.
		relief.rgb = (relief.r + relief.g + relief.b) / 3.0;

		// Actual color at position.
		float4 color = tex2D(textureSampler, uvSphere / Scale);

		// Multiply with shadows.
		float3 shadows = relief.rgb;
		shadows = threshold_low(shadows, 0.5);
		shadows = saturate(shadows + 0.5);
		color.rgb = color.rgb * shadows;

		// Screen with lights.
		float3 lights = relief.rgb;
		lights = threshold_high(lights, 0.65);
		lights *= 0.2;
		color.rgb = 1 - (1 - color.rgb) * (1 - lights);

		// Alpha for smoother border.
		float alpha = clamp((1 - rInner) * (TextureSize / 8), 0, 1);

		// Atmosphere.
		float atmosphere = (rOuter - 1) * TextureSize / 80 + 1;
		float4 atmosphereColor = AtmosphereColor * 0.45 * (1 - atmosphere * atmosphere);
		color = 1 - (1 - color) * (1 - atmosphereColor);
		color = lerp(atmosphereColor, color, alpha);

		// Self-shadowing based on light source position.
		rOffset *= 0.5;
		rOffset *= rOffset;
		color.rgb *= saturate(rOffset);

		// Screen with the opposite.
		rOffset *= rOffset;
		color.rgb = 1 - ((1 - color.rbg) * (1 - saturate(rOffset)));

		return color;
	}
}

technique Render
{
    pass Planet
    {
        PixelShader = compile ps_2_0 PlanetShaderFunction();
    }
    pass Atmosphere
    {
        PixelShader = compile ps_2_0 AtmosphereShaderFunction();
    }
}
