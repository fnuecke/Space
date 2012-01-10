//-----------------------------------------------
// XUI - TextureManager.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace XUI.UI
{

// class TextureBundle
public class TextureBundle
{
	// TextureBundle
	public TextureBundle()
	{
		Content = new ContentManager( _UI.Game.Services, "Content" );
		Textures = new List< Texture2D >();
		TextureNameMap = new Dictionary< string, int >();
	}

	// Destroy
	public void Destroy()
	{
		Content.Unload();
	}

	// Add
	public void Add( string path, string name )
	{
		Textures.Add( Content.Load< Texture2D >( path ) );
		TextureNameMap.Add( name, Textures.Count - 1 );
	}

	// Get
	public int Get( string name )
	{
		int result;

		if ( TextureNameMap.TryGetValue( name, out result ) )
			return result;

		return -1;
	}

	public Texture2D Get( int index )
	{
		return Textures[ index ];
	}

	//
	private ContentManager					Content;
	private List< Texture2D >				Textures;
	private Dictionary< string, int >		TextureNameMap;
	//
};

// class TextureManager
public class TextureManager
{
	// TextureManager
	public TextureManager()
	{
		Bundles = new TextureBundle[ _UI.Settings.Texture_BundleCount ];

		// default bundle
		Bundles[ 0 ] = new TextureBundle();
		Bundles[ 0 ].Add( "Textures\\UI_Null", "null" );
	}

	// CreateBundle
	public int CreateBundle()
	{
		for ( int i = 0; i < Bundles.Length; ++i )
		{
			if ( Bundles[ i ] != null )
				continue;

			Bundles[ i ] = new TextureBundle();

			return i;
		}

		return -1;
	}

	// DestroyBundle
	public void DestroyBundle( int index )
	{
		if ( index == -1 )
		{
			// destroy all
			for ( int i = 0; i < Bundles.Length; ++i )
			{
				if ( Bundles[ i ] == null )
					continue;

				Bundles[ i ].Destroy();
				Bundles[ i ] = null;
			}
		}
		else
		{
			Bundles[ index ].Destroy();
			Bundles[ index ] = null;
		}
	}

	// Add
	public void Add( int bundleIndex, string path, string name )
	{
		Bundles[ bundleIndex ].Add( path, name );
	}

	// Get
	public int Get( string name )
	{
		for ( int i = 0; i < Bundles.Length; ++i )
		{
			if ( Bundles[ i ] == null )
				continue;

			int result = Bundles[ i ].Get( name );

			if ( result != -1 )
				return ( ( i << 16 ) | result );
		}

		return 0; // null
	}

	public Texture2D Get( int textureIndex )
	{
		return Bundles[ textureIndex >> 16 ].Get( textureIndex & 0xffff );
	}

	//
	private TextureBundle[]		Bundles;
	//
};

// struct SpriteTexture
public struct SpriteTexture
{
	// SpriteTexture
	public SpriteTexture( string name, ref Vector2 puv, ref Vector2 suv )
	{
		TextureIndex = _UI.Texture.Get( name );
		PUV = puv;
		SUV = suv;
	}

	public SpriteTexture( string name, float pu, float pv, float su, float sv )
	{
		TextureIndex = _UI.Texture.Get( name );
		PUV = new Vector2( pu, pv );
		SUV = new Vector2( su, sv );
	}

	public Vector2			PUV;
	public Vector2			SUV;
	public int				TextureIndex;

	public static int		TextureCount = 3;
};

}; // namespace UI
