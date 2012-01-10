//-----------------------------------------------
// XUI - UI_Common.fxh
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

float4x4	UiTransformMatrix	: register( c0 );

// Transform
float4 Transform( float3 position )
{
	float4 p = mul( float4( position, 1.0f ), UiTransformMatrix );
	return p;
}
