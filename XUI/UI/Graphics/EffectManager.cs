//-----------------------------------------------
// XUI - EffectManager.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using Microsoft.Xna.Framework.Graphics;

namespace XUI.UI
{

// E_Effect
public enum E_Effect
{
	MultiTexture1 = 0,
	MultiTexture2,
	MultiTexture3,
	IntensityAsAlpha,
	IntensityAsAlpha_PMA,
	IntensityAsAlphaTexture,
	IntensityAsAlphaTexture_PMA,
	GrayScale,

	Count,
};

//
public delegate void Effect_SetCustomParams( ref RenderState renderState );
//

// class EffectManager
public class EffectManager
{
	// EffectManager
	public EffectManager()
	{
		Effects = new Effect[ (int)_UI.Settings.Effect_Count ];

		Effects[ (int)E_Effect.MultiTexture1 ] = _UI.Content.Load< Effect >( "Effects\\UI_MultiTexture1" );
		Effects[ (int)E_Effect.MultiTexture2 ] = _UI.Content.Load< Effect >( "Effects\\UI_MultiTexture2" );
		Effects[ (int)E_Effect.MultiTexture3 ] = _UI.Content.Load< Effect >( "Effects\\UI_MultiTexture3" );
		Effects[ (int)E_Effect.IntensityAsAlpha ] = _UI.Content.Load< Effect >( "Effects\\UI_IntensityAsAlpha" );
		Effects[ (int)E_Effect.IntensityAsAlpha_PMA ] = _UI.Content.Load< Effect >( "Effects\\UI_IntensityAsAlpha_PMA" );
		Effects[ (int)E_Effect.IntensityAsAlphaTexture ] = _UI.Content.Load< Effect >( "Effects\\UI_IntensityAsAlphaTexture" );
		Effects[ (int)E_Effect.IntensityAsAlphaTexture_PMA ] = _UI.Content.Load< Effect >( "Effects\\UI_IntensityAsAlphaTexture_PMA" );
		Effects[ (int)E_Effect.GrayScale ] = _UI.Content.Load< Effect >( "Effects\\UI_GrayScale" );

		Param_UiTransform = new EffectParameter[ (int)Effects.Length ];

		for ( int i = 0; i < (int)E_Effect.Count; ++i )
			Param_UiTransform[ i ] = Effects[ i ].Parameters[ "UiTransformMatrix" ];
	}

	// Add
	public void Add( int index, string path )
	{
		if ( index < (int)E_Effect.Count )
			return;

		Effects[ index ] = _UI.Content.Load< Effect >( path );
		Param_UiTransform[ index ] = Effects[ index ].Parameters[ "UiTransformMatrix" ];
	}

	// Get
	public Effect Get( int index )
	{
		return Effects[ index ];
	}

	// GetParam_UiTransform
	public EffectParameter GetParam_UiTransform( int index )
	{
		return Param_UiTransform[ index ];
	}

	// SetParams
	public void SetParams( ref RenderState renderState )
	{
		// custom params
		if ( Event_SetCustomParams != null )
			Event_SetCustomParams( ref renderState );
	}

	//
	private Effect[]						Effects;
	private EffectParameter[]				Param_UiTransform;

	public event Effect_SetCustomParams		Event_SetCustomParams;
	//
};

}; // namespace UI
