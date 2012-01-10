//-----------------------------------------------
// XUI - FontIcon.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace XUI.UI
{

// class FontIcon
public class FontIcon
{
	// FontIcon
	public FontIcon( int textureIndex, float pu, float pv, float su, float sv )
	{
		Init();

		Texture.TextureIndex = textureIndex;
		Texture.PUV.X = pu;
		Texture.PUV.Y = pv;
		Texture.SUV.X = su;
		Texture.SUV.Y = sv;
	}

	// Init
	public void Init()
	{
		Texture.TextureIndex = 0;
		Texture.PUV = new Vector2( 0.0f );
		Texture.SUV = new Vector2( 1.0f );
		Color = 0xffffffff;
		AspectRatio = 1.0f;
		Scale = 1.0f;
		RenderState = new RenderState( (int)E_Effect.MultiTexture1, E_BlendState.AlphaBlend );
		LayerOffset = 0;
	}

	//
	public SpriteTexture		Texture;
	public SpriteColor			Color;
	public float				AspectRatio;
	public float				Scale;
	public RenderState			RenderState;
	public int					LayerOffset;
	//
};

// class Store_FontIcon
public class Store_FontIcon
{
	// Store_FontIcon
	public Store_FontIcon()
	{
		Names = new List< string >();
		Icons = new List< FontIcon >();
	}

	// Add
	public void Add( string name, FontIcon icon )
	{
		Names.Add( name );
		Icons.Add( icon );
	}

	// Get
	public int Get( StringUI name )
	{
		for ( int i = 0; i < Names.Count; ++i )
			if ( name.EqualTo( Names[ i ] ) )
				return i;

		return -1;
	}

	public FontIcon Get( int index )
	{
		return Icons[ index ];
	}

	//
	private List< string >			Names;
	private List< FontIcon >		Icons;
	//
};

}; // namespace UI
