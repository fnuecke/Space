//-----------------------------------------------
// XUI - FontManager.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System.Collections.Generic;
using Microsoft.Xna.Framework;

// TODO - fixed width numbers
// TODO - optimise StringSize to try and not use the list

namespace XUI.UI
{

// E_FontProcessType
public enum E_FontProcessType
{
	Draw = 0,
	Size,

	Count,
};

// struct StringProxy
public struct StringProxy
{
	// StringProxy
	public StringProxy( StringUI s )
	{
		StringUI = s;
		String = null;
		Length = s.Length;
	}

	public StringProxy( string s )
	{
		String = s;
		StringUI = null;
		Length = s.Length;
	}

	//
	public StringUI		StringUI;
	public string		String;
	public int			Length;
	//
}

// class FontManager
public class FontManager
{
	// FontManager
	public FontManager()
	{
		Fonts = new List< Font >();
		SpriteList = new FontSpriteList();
		StringUI = new StringUI( 16 );
	}

	// Add
	public void Add( string path, string name )
	{
		Fonts.Add( new Font( path, name ) );
	}

	// Get
	public Font Get( string name )
	{
		if ( name == null )
			return null;

		for ( int i = 0; i < Fonts.Count; ++i )
			if ( Fonts[ i ].Name.Equals( name ) )
				return Fonts[ i ];

		return null;
	}

	// DoFontEffectsNext
	public void DoFontEffectsNext( List< FontEffect > fontEffects )
	{
		FontEffects = fontEffects;
	}

	// StringSize
	public Vector2 StringSize( string text, FontStyle fontStyle, float height, float wrapSizeX )
	{
		StringProxy stringProxy = new StringProxy( text );
		return Process( ref stringProxy, fontStyle, height, wrapSizeX, E_FontProcessType.Size );
	}

	public Vector2 StringSize( StringUI text, FontStyle fontStyle, float height, float wrapSizeX )
	{
		StringProxy stringProxy = new StringProxy( text );
		return Process( ref stringProxy, fontStyle, height, wrapSizeX, E_FontProcessType.Size );
	}

	// Draw
	public Vector2 Draw( string text, FontStyle fontStyle, int renderPass, int layer, ref Vector3 pos, float height, E_Align align, ref SpriteColors colors, float wrapSizeX )
	{
		StringProxy stringProxy = new StringProxy( text );
		return Process( ref stringProxy, fontStyle, renderPass, layer, ref pos, height, align, ref colors, wrapSizeX );
	}

	public Vector2 Draw( StringUI text, FontStyle fontStyle, int renderPass, int layer, ref Vector3 pos, float height, E_Align align, ref SpriteColors colors, float wrapSizeX )
	{
		StringProxy stringProxy = new StringProxy( text );
		return Process( ref stringProxy, fontStyle, renderPass, layer, ref pos, height, align, ref colors, wrapSizeX );
	}

	// Process
	private Vector2 Process( ref StringProxy text, FontStyle fontStyle, int renderPass, int layer, ref Vector3 pos, float height, E_Align align, ref SpriteColors colors, float wrapSizeX )
	{
		RenderPass = renderPass;
		Layer = layer;
		Position = pos;
		Align = align;
		Colors = colors;

		if ( ( fontStyle.RenderState.BlendState == E_BlendState.AlphaBlend ) && !Colors.PreMultipliedAlpha )
			Colors.ToPremultiplied();

		return Process( ref text, fontStyle, height, wrapSizeX, E_FontProcessType.Draw );
	}

	private unsafe Vector2 Process( ref StringProxy text, FontStyle fontStyle, float height, float wrapSizeX, E_FontProcessType processType )
	{
		if ( text.Length == 0 )
			return new Vector2();

		SpriteList.Reset();

		SpriteList.RenderPass = RenderPass;
		SpriteList.Layer = Layer;

		FontData fontData = fontStyle.Font.FontData;

		float startX = Position.X;
		float x = startX;
		float y = Position.Y;

		float scale = height / fontData.FontHeight;
		float charHeight = fontData.LineHeight * scale;
		float lineHeight = charHeight * fontStyle.HeightPercentage;
		float multY = _UI.Sprite.IsRenderPass3D( RenderPass ) ? -1.0f : 1.0f;

		int numLines = 1;

		fixed ( char* pString = text.String )
		fixed ( FontSprite* pSpriteStart = &SpriteList.Sprites[ 0 ] )
		fixed ( FontDataChar* pCharDataStart = &fontData.Characters[ 0 ] )
		{
			char* s = ( pString != null ) ? pString : text.StringUI;

			for ( int i = 0; i < text.Length; ++i )
			{
				char c = s[ i ];

				if ( c == '\n' )
				{
					++numLines;
					y += ( lineHeight * multY );
					x = startX;

					( pSpriteStart + SpriteList.RenderEnd - 1 )->BreakType = E_LetterBreakType.Line;

					continue;
				}

				int characterIndex = fontData.GetCharacterIndex( c );
				FontDataChar* pCharData = ( pCharDataStart + characterIndex );

				if ( c == ' ' )
				{
					// mark as potential wrap point unless we've already wrapped here
					FontSprite* pSpriteEnd = ( pSpriteStart + SpriteList.RenderEnd - 1 );
					
					if ( ( SpriteList.RenderEnd != 0 ) && ( pSpriteEnd->BreakType != E_LetterBreakType.Line ) )
						pSpriteEnd->BreakType = E_LetterBreakType.Wrap;

					// advance
					x += ( pCharData->AdvanceX * scale );
				}
				else
				{
					// check for icons
					if ( ( ( i + 5 ) <= text.Length ) && ( c == '[' ) && ( s[ i + 1 ] == '[' ) )
					{
						int j = i + 2;

						StringUI.Clear();

						while ( ( j < text.Length ) && ( s[ j ] != ']' ) )
							StringUI.Add( s[ j++ ] );

						if ( ( ( j + 1 ) < text.Length ) && ( s[ j + 1 ] == ']' ) )
						{
							// valid format
							int fontIconIndex = _UI.Store_FontIcon.Get( StringUI );

							if ( fontIconIndex != -1 )
							{
								// add to list
								FontIcon fontIcon = _UI.Store_FontIcon.Get( fontIconIndex );

								FontSprite* pSprite = ( pSpriteStart + SpriteList.RenderEnd++ );

								pSprite->Character = (char)0xffff;

								pSprite->Position.X = x;
								pSprite->Position.Y = y - ( ( ( charHeight * fontIcon.Scale ) - charHeight ) * 0.5f ) * multY;
								pSprite->Position.Z = Position.Z;

								float heightScaled = charHeight * fontIcon.Scale;
								pSprite->Size.X = heightScaled * fontIcon.AspectRatio;
								pSprite->Size.Y = heightScaled;

								fixed ( SpriteColor* pColor = &fontIcon.Color )
								{
									pSprite->Colors.CopyA( ref Colors );
									pSprite->Colors.MultA( pColor->A );

									pSprite->Colors.R( pColor->R );
									pSprite->Colors.G( pColor->G );
									pSprite->Colors.B( pColor->B );

									if ( Colors.PreMultipliedAlpha )
										pSprite->Colors.ToPremultiplied();
								}

								// set base texture
								pSprite->Texture.TextureIndex = fontIcon.Texture.TextureIndex;
								pSprite->Texture.PUV = fontIcon.Texture.PUV;
								pSprite->Texture.SUV = fontIcon.Texture.SUV;

								pSprite->BreakType = E_LetterBreakType.None;

								pSprite->IconIndex = fontIconIndex;

								// advance
								x += pSprite->Size.X;

								// advance string past the icon characters
								i += ( 4 + StringUI.Length - 1 );
							}
						}
					}
					else
					{
						// add to list
						FontSprite* pSprite = ( pSpriteStart + SpriteList.RenderEnd++ );

						pSprite->Character = c;

						pSprite->Position.X = x + ( pCharData->OffsetX * scale );
						pSprite->Position.Y = y + ( pCharData->OffsetY * scale ) * multY;
						pSprite->Position.Z = Position.Z;

						pSprite->Size.X = pCharData->Width * scale;
						pSprite->Size.Y = pCharData->Height * scale;

						pSprite->Colors = Colors;

						// set base texture
						pSprite->Texture.TextureIndex = fontStyle.Font.TextureIndex;
						pSprite->Texture.PUV.X = (float)pCharData->X / fontData.TextureWidth;
						pSprite->Texture.PUV.Y = (float)pCharData->Y / fontData.TextureHeight;
						pSprite->Texture.SUV.X = (float)pCharData->Width / fontData.TextureWidth;
						pSprite->Texture.SUV.Y = (float)pCharData->Height / fontData.TextureHeight;

						// second (optional) texture is set through the FontStyle when rendering

						pSprite->BreakType = E_LetterBreakType.None;

						pSprite->IconIndex = -1;

						// advance
						x += ( pCharData->AdvanceX * scale );

						// kerning
						if ( i != ( text.Length - 1 ) )
							x += ( fontData.GetKerningAmount( c, s[ i + 1 ] ) * scale );
					}
				}

				// check for wrapping
				if ( ( c != ' ' ) && ( wrapSizeX != 0.0f ) && ( ( x - startX ) > wrapSizeX ) )
				{
					int iWrap = SpriteList.RenderEnd - 1;
					FontSprite* pSpriteWrap = ( pSpriteStart + iWrap );
				
					// search backwards until we find a place to wrap
					while ( ( iWrap >= 0 ) && ( pSpriteWrap->BreakType != E_LetterBreakType.Wrap ) && ( pSpriteWrap->BreakType != E_LetterBreakType.Line ) )
						pSpriteWrap = ( pSpriteStart + --iWrap );

					bool wrapOnLine = false;

					if ( iWrap != -1 )
					{
						if ( pSpriteWrap->BreakType == E_LetterBreakType.Line )
							wrapOnLine = true; // word is longer than the wrap size
						else
							pSpriteWrap->BreakType = E_LetterBreakType.Line;
					}
					else
					{
						if ( SpriteList.RenderEnd > 1 )
							wrapOnLine = true; // long first word with leading spaces, wtf
					}

					++pSpriteWrap;

					float diffX = pSpriteWrap->Position.X - startX;
					float offsetX = x - pSpriteWrap->Position.X;

					// shift the characters after the break point
					for ( int j = ( iWrap + 1 ); j < SpriteList.RenderEnd; ++j )
					{
						pSpriteWrap->Position.X -= diffX;

						if ( !wrapOnLine )
							pSpriteWrap->Position.Y += ( lineHeight * multY );

						++pSpriteWrap;
					}

					++numLines;

					if ( !wrapOnLine )
						y += ( lineHeight * multY );

					x = startX + offsetX;
				}

				// tracking
				x += ( height * fontStyle.TrackingPercentage );
			}

			( pSpriteStart + SpriteList.RenderEnd - 1 )->BreakType = E_LetterBreakType.Line;

			float totalHeight = ( ( numLines - 1 ) * lineHeight ) + charHeight;

			if ( processType == E_FontProcessType.Draw )
			{
				// align
				if ( ( Align != E_Align.TopLeft ) && ( Align != E_Align.None ) )
				{
					Vector2 offset = _UI.Sprite.GetVertexOffsetAligned( 0, Align );

					if ( ( Align == E_Align.MiddleLeft ) || ( Align == E_Align.BottomLeft ) )
					{
						// only just need to offset y
						FontSprite* pSpriteToOffset = pSpriteStart;
					
						for ( int i = 0; i < SpriteList.RenderEnd; ++i, ++pSpriteToOffset )
							pSpriteToOffset->Position.Y -= ( totalHeight * offset.Y ) * multY;
					}
					else
					{
						// need to offset both x and y
						int iLineStart = 0;

						FontSprite* pSpriteLineEnd = pSpriteStart;

						for ( int i = 0; i < SpriteList.RenderEnd; ++i, ++pSpriteLineEnd )
						{
							if ( pSpriteLineEnd->BreakType != E_LetterBreakType.Line )
								continue;

							float lineWidth = ( pSpriteLineEnd->Position.X + pSpriteLineEnd->Size.X ) - startX;

							// offset every sprite on this line
							FontSprite* pSpriteToOffset = ( pSpriteStart + iLineStart );

							for ( int j = iLineStart; j <= i; ++j, ++pSpriteToOffset )
							{
								pSpriteToOffset->Position.X -= ( lineWidth * offset.X );
								pSpriteToOffset->Position.Y -= ( totalHeight * offset.Y ) * multY;
							}

							iLineStart = i + 1;
						}
					}
				}

				// pre-render
				if ( FontEffects != null )
				{
					for ( int i = 0; i < FontEffects.Count; ++i )
						FontEffects[ i ].Process( SpriteList );

					FontEffects = null;
				}

				// actual render
				fontStyle.Render( SpriteList, height );
			}

			return new Vector2( ( numLines == 1 ) ? ( x - startX ) : wrapSizeX, totalHeight );
		}
	}

	//
	private List< Font >			Fonts;
	public  FontSpriteList			SpriteList		{ get; private set; }
	private StringUI				StringUI;
	private List< FontEffect >		FontEffects;

	private int						RenderPass;
	private int						Layer;
	private Vector3					Position;
	private E_Align					Align;
	private SpriteColors			Colors;
	//
};

}; // namespace UI
