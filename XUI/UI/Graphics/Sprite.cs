//-----------------------------------------------
// XUI - Sprite.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using Microsoft.Xna.Framework;

// TODO - Custom E_SamplerState

namespace XUI.UI
{

// E_SpriteType
public enum E_SpriteType
{
	Sprite = 0,
	BlendState,
	DepthStencilState,
	RasterizerState,

	Count,
};

// E_Align
public enum E_Align
{
	None = 0,
	TopLeft,
	TopCentre,
	TopRight,
	MiddleLeft,
	MiddleCentre,
	MiddleRight,
	BottomLeft,
	BottomCentre,
	BottomRight,

	Count,
};

// E_BlendState
public enum E_BlendState
{
	AlphaBlend = 0,
	Opaque,
	Additive,
	NonPremultiplied,
	Custom,

	Count,
};

// E_SamplerState
public enum E_SamplerState
{
	AnisotropicClamp = 0,
	AnisotropicWrap,
	LinearClamp,
	LinearWrap,
	PointClamp,
	PointWrap,

	Count,
};

// struct RenderState
public struct RenderState
{
	// RenderState
	public RenderState( int effect, E_BlendState blendState )
	{
		Effect = effect;
		BlendState = blendState;
		SamplerState0 = E_SamplerState.LinearClamp;
		SamplerState1 = E_SamplerState.LinearClamp;
		SamplerState2 = E_SamplerState.LinearClamp;
		EffectParamF = 0.0f;
		EffectParamI = 0;
	}

	//
	public E_BlendState			BlendState;
	public int					Effect;
	public E_SamplerState		SamplerState0;
	public E_SamplerState		SamplerState1;
	public E_SamplerState		SamplerState2;
	public float				EffectParamF;
	public int					EffectParamI;
	//
};

// struct Sprite
public struct Sprite
{
	//
	public E_SpriteType			Type;
	public Vector3				Position;
	public Vector2				Size;
	public E_Align				Align;
	public SpriteColors			Colors;
	public int					PreTransformIndex;
	public int					PostTransformIndex;
	public SpriteTexture		Texture0;
	public SpriteTexture		Texture1;
	public SpriteTexture		Texture2;
	public RenderState			RenderState;
	public int					BatchCount;
	//
};

}; // namespace UI
