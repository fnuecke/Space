//-----------------------------------------------
// XUI - UI_GrayScale.fx
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

#include "UI_Common.fxh"

sampler2D	Texture0 : register( s0 );

struct VS_Input
{
	float3 Position : POSITION0;
	float4 Color : COLOR0;
	float2 UV0 : TEXCOORD0;
};

struct VS_Output
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
	float2 UV0 : TEXCOORD0;
};

VS_Output VS( VS_Input input )
{
	VS_Output output;

	output.Position = Transform( input.Position.xyz );
	output.Color = input.Color;
	output.UV0 = input.UV0;

	return output;
}

float4 PS( VS_Output input ) : COLOR0
{
	float4 c = input.Color;

	float4 tex0 = tex2D( Texture0, input.UV0 );
	tex0.rgb = dot( tex0.rgb, float3( 0.3f, 0.59f, 0.11f ) );

	c *= tex0;

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
