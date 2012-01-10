//-----------------------------------------------
// XUI - FontEffect.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System;

// TODO - start time param

namespace XUI.UI
{

// class FontEffect
public abstract class FontEffect
{
	// FontEffect
	public FontEffect()
		: this( 0.5f, 0.25f, 1.0f )
	{
		//
	}

	public FontEffect( float spriteChangeTime, float spriteUpdateTime, float waitTime )
	{
		SpriteTimes01 = new float[ _UI.Settings.Font_SpriteCount ];

		SpriteCount = -1;
		
		SpriteChangeTime = spriteChangeTime;
		SpriteUpdateTime = spriteUpdateTime;

		WaitTime = waitTime;

		if ( WaitTime == 0.0f )
			WaitTime = 0.001f;
		else
		if ( WaitTime < 0.0f )
			WaitTime = -1.0f;

		WaitTimer = 0.001f; // to force initial reset but only if selected
	}

	// Copy
	public FontEffect Copy()
	{
		FontEffect o = (FontEffect)Activator.CreateInstance( GetType() );

		CopyTo( o );

		return o;
	}

	// CopyTo
	protected virtual void CopyTo( FontEffect o )
	{
		o.SpriteChangeTime = SpriteChangeTime;
		o.SpriteUpdateTime = SpriteUpdateTime;
		
		for ( int i = 0; i < SpriteTimes01.Length; ++i )
			o.SpriteTimes01[ i ] = SpriteTimes01[ i ];

		o.CurrentSprite = CurrentSprite;
		// Widget - doesn't copy over

		o.CurrentChangeTime = CurrentChangeTime;

		o.WaitTime = WaitTime;
		o.WaitTimer = WaitTimer;

		o.SpriteCount = SpriteCount;
	}

	// Bind
	public void Bind( WidgetText widget )
	{
		Widget = widget;
	}

	// Reset
	public void Reset()
	{
		for ( int i = 0; i < SpriteTimes01.Length; ++i )
			SpriteTimes01[ i ] = 0.0f;

		CurrentSprite = 0;
		CurrentChangeTime = 0.0f;

		WaitTimer = 0.0f;
	}

	// ResetWait
	public void ResetWait()
	{
		if ( WaitTimer > 0.0f )
			WaitTimer = 0.001f;
	}

	// Update
	public void Update( float frameTime )
	{
		if ( SpriteCount == -1 )
			return;

		for ( int i = 0; i < SpriteCount; ++i )
		{
			if ( SpriteTimes01[ i ] == 1.0f )
				continue;

			if ( ( ( i <= CurrentSprite ) && ( WaitTimer == 0.0f ) ) || ( SpriteTimes01[ i ] > 0.0f ) )
				SpriteTimes01[ i ] += ( ( 1.0f / SpriteUpdateTime ) * frameTime );

			if ( SpriteTimes01[ i ] > 1.0f )
				SpriteTimes01[ i ] = 1.0f;
		}

		if ( WaitTimer == -1.0f )
			return; // manual reset only

		if ( WaitTimer != 0.0f )
		{
			WaitTimer -= frameTime;

			if ( ( WaitTimer < 0.0f ) && Widget.Selected() )
				Reset();
		}
		else
		{
			CurrentChangeTime += frameTime;

			if ( CurrentChangeTime >= SpriteChangeTime )
			{
				++CurrentSprite;

				if ( CurrentSprite == SpriteCount )
					WaitTimer = WaitTime;
				else
				{
					SpriteTimes01[ CurrentSprite ] = 0.0f;
					CurrentChangeTime -= SpriteChangeTime;
				}
			}
		}
	}

	// Process
	public void Process( FontSpriteList list )
	{
		SpriteCount = list.RenderEnd - list.RenderStart;

		OnProcess( list );
	}

	// OnProcess
	protected virtual void OnProcess( FontSpriteList list )
	{
		//
	}

	//
	private float				SpriteChangeTime;
	private float				SpriteUpdateTime;

	protected float[]			SpriteTimes01;
	protected int				CurrentSprite;
	protected WidgetText		Widget;

	private float				CurrentChangeTime;

	private float				WaitTime;
	private float				WaitTimer;

	private int					SpriteCount;
	//
};

// class FontEffect_Scale
public class FontEffect_Scale: FontEffect
{
	// FontEffect_Scale
	public FontEffect_Scale()
		: base()
	{
		//
	}

	public FontEffect_Scale( float spriteChangeTime, float spriteUpdateTime, float waitTime, float scaleFromX, float scaleToX, float scaleFromY, float scaleToY, E_LerpType lerpType )
		: base( spriteChangeTime, spriteUpdateTime, waitTime )
	{
		ScaleFromX = scaleFromX;
		ScaleToX = scaleToX;
		ScaleFromY = scaleFromY;
		ScaleToY = scaleToY;
		LerpType = lerpType;
	}

	// CopyTo
	protected override void CopyTo( FontEffect o )
	{
		base.CopyTo( o );

		FontEffect_Scale oo = (FontEffect_Scale)o;

		oo.ScaleFromX = ScaleFromX;
		oo.ScaleToX = ScaleToX;
		oo.ScaleFromY = ScaleFromY;
		oo.ScaleToY = ScaleToY;
		oo.LerpType = LerpType;
	}

	// OnProcess
	protected unsafe override void OnProcess( FontSpriteList list )
	{
		float multY = Widget.UpY;

		fixed ( FontSprite* pSpriteStart = &list.Sprites[ 0 ] )
		{
			for ( int i = list.RenderStart; i < list.RenderEnd; ++i )
			{
				FontSprite* pSprite = ( pSpriteStart + i );

				float time01 = Lerp.Adjust( SpriteTimes01[ i ], LerpType );

				float scaleX = ScaleFromX + ( ScaleToX - ScaleFromX ) * time01;
				float scaleY = ScaleFromY + ( ScaleToY - ScaleFromY ) * time01;

				pSprite->Position.X -= ( pSprite->Size.X * ( scaleX - 1.0f ) * 0.5f );
				pSprite->Position.Y -= ( pSprite->Size.Y * ( scaleY - 1.0f ) * 0.5f ) * multY;

				pSprite->Size.X *= scaleX;
				pSprite->Size.Y *= scaleY;
			}
		}
	}

	//
	private float			ScaleFromX;
	private float			ScaleToX;
	private float			ScaleFromY;
	private float			ScaleToY;
	private E_LerpType		LerpType;
	//
};

// class FontEffect_TypeOut
public class FontEffect_TypeOut : FontEffect
{
	// FontEffect_TypeOut
	public FontEffect_TypeOut()
		: base()
	{
		//
	}

	public FontEffect_TypeOut( float spriteChangeTime, float spriteUpdateTime, float waitTime )
		: base( spriteChangeTime, spriteUpdateTime, waitTime )
	{
		//
	}

	// OnProcess
	protected override void OnProcess( FontSpriteList list )
	{
		list.RenderEnd = list.RenderStart + CurrentSprite;
	}
};

// class FontEffect_Alpha
public class FontEffect_Alpha : FontEffect
{
	// FontEffect_Alpha
	public FontEffect_Alpha()
		: base()
	{
		//
	}

	public FontEffect_Alpha( float spriteChangeTime, float spriteUpdateTime, float waitTime, E_LerpType lerpType )
		: base( spriteChangeTime, spriteUpdateTime, waitTime )
	{
		LerpType = lerpType;
	}

	// CopyTo
	protected override void CopyTo( FontEffect o )
	{
		base.CopyTo( o );

		FontEffect_Alpha oo = (FontEffect_Alpha)o;

		oo.LerpType = LerpType;
	}

	// OnProcess
	protected unsafe override void OnProcess( FontSpriteList list )
	{
		fixed ( FontSprite* pSpriteStart = &list.Sprites[ 0 ] )
			for ( int i = list.RenderStart; i < list.RenderEnd; ++i )
				( pSpriteStart + i )->Colors.MultA( (byte)( Lerp.Adjust( SpriteTimes01[ i ], LerpType ) * 255.0f ) );
	}

	//
	private E_LerpType		LerpType;
	//
};

// class FontEffect_ColorLerp
public class FontEffect_ColorLerp : FontEffect
{
	// FontEffect_ColorLerp
	public FontEffect_ColorLerp()
		: base()
	{
		//
	}

	public FontEffect_ColorLerp( float spriteChangeTime, float spriteUpdateTime, float waitTime, SpriteColors colors, E_LerpType lerpType )
		: base( spriteChangeTime, spriteUpdateTime, waitTime )
	{
		Colors = colors;
		LerpType = lerpType;
	}

	// CopyTo
	protected override void CopyTo( FontEffect o )
	{
		base.CopyTo( o );

		FontEffect_ColorLerp oo = (FontEffect_ColorLerp)o;

		oo.Colors = Colors;
		oo.LerpType = LerpType;
	}

	// OnProcess
	protected unsafe override void OnProcess( FontSpriteList list )
	{
		fixed ( FontSprite* pSpriteStart = &list.Sprites[ 0 ] )
		{
			for ( int i = list.RenderStart; i < list.RenderEnd; ++i )
			{
				FontSprite* pSprite = ( pSpriteStart + i );

				SpriteColors c = Colors;
				c.MultA( ref pSprite->Colors );

				if ( pSprite->Colors.PreMultipliedAlpha )
					c.ToPremultiplied();

				pSprite->Colors.Lerp( ref pSprite->Colors, ref c, Lerp.Adjust( SpriteTimes01[ i ], LerpType ) );
			}
		}
	}

	//
	private SpriteColors		Colors;
	private E_LerpType			LerpType;
	//
};

// class FontEffect_TypeOutCharacterSwitch
public class FontEffect_TypeOutCharacterSwitch : FontEffect
{
	// FontEffect_TypeOutCharacterSwitch
	public FontEffect_TypeOutCharacterSwitch()
		: base()
	{
		//
	}

	public FontEffect_TypeOutCharacterSwitch( float spriteChangeTime, float spriteUpdateTime, float waitTime )
		: base( spriteChangeTime, spriteUpdateTime, waitTime )
	{
		//
	}

	// OnProcess
	protected unsafe override void OnProcess( FontSpriteList list )
	{
		list.RenderEnd = list.RenderStart + CurrentSprite;

		FontData fontData = Widget.FontStyle.Font.FontData;

		float scale = Widget.RenderSizeY / fontData.FontHeight;
		float multY = Widget.UpY;

		fixed ( FontDataChar* pCharDataStart = &fontData.Characters[ 0 ] )
		fixed ( FontSprite* pSpriteStart = &list.Sprites[ 0 ] )
		{
			for ( int i = list.RenderStart; i < list.RenderEnd; ++i )
			{
				FontSprite* pSprite = ( pSpriteStart + i );

				if ( pSprite->IconIndex != -1 )
					continue;

				float time01 = SpriteTimes01[ i ];

				if ( time01 == 1.0f )
					continue;

				char c = (char)Rand.Next( 97, 123 );

				int characterIndex = fontData.GetCharacterIndex( pSprite->Character );
				FontDataChar* pCharData = ( pCharDataStart + characterIndex );

				int characterIndexRandom = fontData.GetCharacterIndex( c );
				FontDataChar* pCharDataRandom = ( pCharDataStart + characterIndexRandom );

				pSprite->Position.X -= ( pCharData->OffsetX * scale );
				pSprite->Position.Y -= ( pCharData->OffsetY * scale ) * multY;

				pSprite->Position.X += ( pCharDataRandom->OffsetX * scale );
				pSprite->Position.Y += ( pCharDataRandom->OffsetY * scale ) * multY;

				pSprite->Size.X = pCharDataRandom->Width * scale;
				pSprite->Size.Y = pCharDataRandom->Height * scale;

				pSprite->Texture.PUV.X = (float)pCharDataRandom->X / fontData.TextureWidth;
				pSprite->Texture.PUV.Y = (float)pCharDataRandom->Y / fontData.TextureHeight;
				pSprite->Texture.SUV.X = (float)pCharDataRandom->Width / fontData.TextureWidth;
				pSprite->Texture.SUV.Y = (float)pCharDataRandom->Height / fontData.TextureHeight;
			}
		}
	}

	//
	private static Random	Rand = new Random();
	//
};

}; // namespace UI
