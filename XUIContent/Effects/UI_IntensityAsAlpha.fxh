//-----------------------------------------------
// XUI - UI_IntensityAsAlpha.fxh
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

#include "UI_Common.fxh"

sampler2D	Texture0 : register( s0 );
#ifdef TEXTURE
sampler2D	Texture1 : register( s1 );
#endif

struct VS_Input
{
	float3 Position : POSITION0;
	float4 Color : COLOR0;
	float2 UV0 : TEXCOORD0;
#ifdef TEXTURE
	float2 UV1 : TEXCOORD1;
#endif
};

struct VS_Output
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
	float2 UV0 : TEXCOORD0;
#ifdef TEXTURE
	float2 UV1 : TEXCOORD1;
#endif
};

VS_Output VS( VS_Input input )
{
	VS_Output output;

	output.Position = Transform( input.Position.xyz );
	output.Color = input.Color;
	output.UV0 = input.UV0;

#ifdef TEXTURE
	output.UV1 = input.UV1;
#endif

	return output;
}

float4 PS( VS_Output input ) : COLOR0
{
	float4 c = input.Color;

	float4 tex0 = tex2D( Texture0, input.UV0 );
	tex0.a = ( tex0.r + tex0.g + tex0.b ) / 3.0f;
	tex0.rgb = 1.0f;

#ifdef PMA
	tex0.rgb *= tex0.a;
#endif

	c *= tex0;

#ifdef TEXTURE
	float4 tex1 = tex2D( Texture1, input.UV1 );
	c *= tex1;
#endif

	return c;
}

technique Technique0
{
	pass Pass0
	{
		VertexShader = compile vs_3_0 VS();
		PixelShader  = compile ps_3_0 PS();
	}
}
