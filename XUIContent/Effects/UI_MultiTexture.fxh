//-----------------------------------------------
// XUI - UI_MultiTexture.fxh
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

#include "UI_Common.fxh"

sampler2D	Texture0 : register( s0 );
#if defined( TEXTURES_2 ) || defined( TEXTURES_3 )
sampler2D	Texture1 : register( s1 );
#endif
#ifdef TEXTURES_3
sampler2D	Texture2 : register( s2 );
#endif

struct VS_Input
{
	float3 Position : POSITION0;
	float4 Color : COLOR0;
	float2 UV0 : TEXCOORD0;
#if defined( TEXTURES_2 ) || defined( TEXTURES_3 )
	float2 UV1 : TEXCOORD1;
#endif
#ifdef TEXTURES_3
	float2 UV2 : TEXCOORD2;
#endif
};

struct VS_Output
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
	float2 UV0 : TEXCOORD0;
#if defined( TEXTURES_2 ) || defined( TEXTURES_3 )
	float2 UV1 : TEXCOORD1;
#endif
#ifdef TEXTURES_3
	float2 UV2 : TEXCOORD2;
#endif
};

VS_Output VS( VS_Input input )
{
	VS_Output output;

	output.Position = Transform( input.Position.xyz );
	output.Color = input.Color;
	output.UV0 = input.UV0;

#if defined( TEXTURES_2 ) || defined( TEXTURES_3 )
	output.UV1 = input.UV1;
#endif
#ifdef TEXTURES_3
	output.UV2 = input.UV2;
#endif

	return output;
}

float4 PS( VS_Output input ) : COLOR0
{
	float4 c = input.Color;

	float4 tex0 = tex2D( Texture0, input.UV0 );
	c *= tex0;

#if defined( TEXTURES_2 ) || defined( TEXTURES_3 )
	float4 tex1 = tex2D( Texture1, input.UV1 );
	c *= tex1;
#endif
#ifdef TEXTURES_3
	float4 tex2 = tex2D( Texture2, input.UV2 );
	tex2 *= tex2.r;
	c.rgb = saturate( c.rgb + tex2.rgb ) * c.a; // assume PMA
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
