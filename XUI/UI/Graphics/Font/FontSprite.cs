//-----------------------------------------------
// XUI - FontSprite.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using Microsoft.Xna.Framework;

namespace XUI.UI
{

// E_LetterBreakType
public enum E_LetterBreakType
{
	None = 0,
	Wrap, // potential place to wrap
	Line, // actual place of wrap

	Count,
};

// struct FontSprite
public struct FontSprite
{
	//
	public char						Character;
	public Vector3					Position;
	public Vector2					Size;
	public SpriteColors				Colors;
	public SpriteTexture			Texture;
	public E_LetterBreakType		BreakType;
	public int						IconIndex;
	//
};

// class FontSpriteList
public class FontSpriteList
{
	// FontSpriteList
	public FontSpriteList()
	{
		Sprites = new FontSprite[ _UI.Settings.Font_SpriteCount ];
	}

	// CopyTo
	public void CopyTo( FontSpriteList o )
	{
		o.RenderPass = RenderPass;
		o.Layer = Layer;

		for ( int i = 0; i < Sprites.Length; ++i )
			o.Sprites[ i ] = Sprites[ i ];

		o.RenderStart = RenderStart;
		o.RenderEnd = RenderEnd;
	}

	// Reset
	public void Reset()
	{
		RenderStart = 0;
		RenderEnd = 0;
	}

	//
	public int				RenderPass;
	public int				Layer;
	public FontSprite[]		Sprites;
	public int				RenderStart;
	public int				RenderEnd;
	//
};

}; // namespace UI
