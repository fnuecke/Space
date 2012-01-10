//-----------------------------------------------
// XUI - FontStyle.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace XUI.UI
{

// E_FontStyleTextureMode
public enum E_FontStyleTextureMode
{
	ZeroToOne = 0,
	Character,
	Shift,
	PositionShift,

	Count,
};

// class FontStyleRenderPass
public class FontStyleRenderPass
{
	// FontStyleRenderPass
	public FontStyleRenderPass()
	{
		AlphaMult = 1.0f;
		TextureIndex = -1;
		TextureMode = E_FontStyleTextureMode.PositionShift;
	}

	// Copy
	public FontStyleRenderPass Copy()
	{
		FontStyleRenderPass o = new FontStyleRenderPass();

		o.ColorOverride = ColorOverride;
		o.AlphaMult = AlphaMult;
		o.Offset = Offset;
		o.OffsetProportional = OffsetProportional;
		o.TextureIndex = TextureIndex;
		o.TextureMode = TextureMode;

		return o;
	}

	//
	public SpriteColor?				ColorOverride;
	public float					AlphaMult;
	public Vector3					Offset;
	public bool						OffsetProportional;
	public int						TextureIndex;
	public E_FontStyleTextureMode	TextureMode;
	//
};

// class FontStyle
public class FontStyle
{
	// FontStyle
	public FontStyle()
		: this( null )
	{
		//
	}

	public FontStyle( string fontName )
	{
		Font = _UI.Font.Get( fontName );
		RenderState = new RenderState( (int)E_Effect.IntensityAsAlpha_PMA, E_BlendState.AlphaBlend );

		TrackingPercentage = 0.0f;
		HeightPercentage = 1.0f;

		RenderPasses = new List< FontStyleRenderPass >();
	}

	// Copy
	public FontStyle Copy()
	{
		FontStyle o = new FontStyle();

		o.Font = Font;
		o.RenderState = RenderState;

		o.TrackingPercentage = TrackingPercentage;
		o.HeightPercentage = HeightPercentage;

		for ( int i = 0; i < RenderPasses.Count; ++i )
			o.AddRenderPass( RenderPasses[ i ].Copy() );

		return o;
	}

	// AddRenderPass
	public void AddRenderPass( FontStyleRenderPass renderPass )
	{
		RenderPasses.Add( renderPass );
	}

	// Render
	public unsafe void Render( FontSpriteList list, float height )
	{
		fixed ( FontSprite *pSpriteStart = &list.Sprites[ 0 ] )
		{
			for ( int i = 0; i < RenderPasses.Count; ++i )
			{
				FontStyleRenderPass renderPass = RenderPasses[ i ];

				FontSprite *pSprite = ( pSpriteStart + list.RenderStart );

				for ( int j = list.RenderStart; j < list.RenderEnd; ++j, ++pSprite )
				{
					bool isIcon = ( pSprite->IconIndex != -1 );

					SpriteColors colors;
				
					if ( !renderPass.ColorOverride.HasValue )
						colors = pSprite->Colors;
					else
					{
						colors = renderPass.ColorOverride.Value;
						colors.MultA( ref pSprite->Colors );

						if ( pSprite->Colors.PreMultipliedAlpha )
							colors.ToPremultiplied();
					}

					if ( renderPass.AlphaMult != 1.0f )
						colors.MultA( (byte)( renderPass.AlphaMult * 255.0f ) );

					// should this be auto-adjusted for 3d? I'm not convinced atm ...
					Vector3 position = pSprite->Position + ( renderPass.OffsetProportional ? ( renderPass.Offset * height ) : renderPass.Offset );

					if ( isIcon )
					{
						FontIcon fontIcon = _UI.Store_FontIcon.Get( pSprite->IconIndex );
						_UI.Sprite.AddSprite( list.RenderPass, list.Layer + fontIcon.LayerOffset, ref position, pSprite->Size.X, pSprite->Size.Y, E_Align.TopLeft, ref colors, ref fontIcon.RenderState );
					}
					else
						_UI.Sprite.AddSprite( list.RenderPass, list.Layer, ref position, pSprite->Size.X, pSprite->Size.Y, E_Align.TopLeft, ref colors, ref RenderState );

					_UI.Sprite.AddTexture( 0, ref pSprite->Texture );

					if ( !isIcon && ( renderPass.TextureIndex != -1 ) )
					{
						Vector2 puv = new Vector2();
						Vector2 suv = new Vector2( 1.0f );

						switch ( renderPass.TextureMode )
						{
							case E_FontStyleTextureMode.Character:
							{
								puv.X = pSprite->Texture.PUV.X;
								puv.Y = pSprite->Texture.PUV.Y;
								suv.X = pSprite->Texture.SUV.X;
								suv.Y = pSprite->Texture.SUV.Y;
								break;
							}
							case E_FontStyleTextureMode.Shift:
							{
								puv.X = pSprite->Texture.PUV.X;
								suv.X = pSprite->Size.X / pSprite->Size.Y;
								break;
							}
							case E_FontStyleTextureMode.PositionShift:
							{
								puv.X = pSprite->Texture.PUV.X + ( pSprite->Position.X * 0.5f );
								suv.X = pSprite->Size.X / pSprite->Size.Y;
								break;
							}
						}

						_UI.Sprite.AddTexture( 1, renderPass.TextureIndex, ref puv, ref suv );
					}
				}
			}
		}
	}

	//
	public  Font			Font;
	public  RenderState		RenderState;

	public  float			TrackingPercentage;
	public  float			HeightPercentage;

	private List< FontStyleRenderPass >		RenderPasses;
	//
};

}; // namespace UI
