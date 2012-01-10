//-----------------------------------------------
// XUI - TimelineEffect.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System;

// TODO - custom curves for lerping

namespace XUI.UI
{

// E_LerpType
public enum E_LerpType
{
	Linear,
	SmoothStep,
	SmootherStep,
	Sin,
	BounceOnceSmooth,

	Count,
};

// class Lerp
public static class Lerp
{
	// Adjust
	public static float Adjust( float time01, E_LerpType lerpType )
	{
		switch( lerpType )
		{
			case E_LerpType.SmoothStep:			time01 = time01 * time01 * ( 3 - ( 2 * time01 ) );									break;
			case E_LerpType.SmootherStep:		time01 = time01 * time01 * time01 * ( time01 * ( ( time01 * 6 ) - 15 ) + 10 );		break;
			case E_LerpType.Sin:				time01 = (float)Math.Sin( time01 * Math.PI );										break;

			case E_LerpType.BounceOnceSmooth:
			{
				bool down = ( time01 >= 0.5f );

				if ( down )
					time01 -= 0.5f;

				time01 *= 2.0f;

				if ( down )
					time01 = 1.0f - time01;

				time01 = time01 * time01 * ( 3 - ( 2 * time01 ) );

				break;
			}
		}

		return time01;
	}
};

// class TimelineEffect
public abstract class TimelineEffect
{
	// TimelineEffect
	public TimelineEffect()
		: this( 0.0f, 1.0f, E_LerpType.Linear )
	{
		//
	}
	
	public TimelineEffect( float from, float to, E_LerpType lerpType )
	{
		From = from;
		To = to;
		Value = 0.0f;
		LerpType = lerpType;
	}

	// Copy
	public TimelineEffect Copy()
	{
		TimelineEffect o = (TimelineEffect)Activator.CreateInstance( GetType() );

		CopyTo( o );

		return o;
	}

	// CopyTo
	protected virtual void CopyTo( TimelineEffect o )
	{
		// Widget - doesn't copy over
		o.From = From;
		o.To = To;
		o.Value = Value;
		o.LerpType = LerpType;
	}

	// Bind
	public void Bind( WidgetBase widget )
	{
		Widget = widget;

		OnBind();
	}

	// OnBind
	protected virtual void OnBind()
	{
		//
	}

	// Update
	public void Update( float time01 )
	{
		float oldValue = Value;
		Value = From + ( To - From ) * Lerp.Adjust( time01, LerpType );

		OnChange( Value - oldValue );
	}

	protected abstract void		OnChange( float valueDiff );

	//
	protected WidgetBase		Widget;
	protected float				From;
	protected float				To;
	protected float				Value;
	protected E_LerpType		LerpType;
	//
};

// class TimelineEffect_PositionX
public class TimelineEffect_PositionX : TimelineEffect
{
	public TimelineEffect_PositionX()
		: base() {  }

	public TimelineEffect_PositionX( float from, float to, E_LerpType lerpType )
		: base( from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ Widget.Position.X += valueDiff; }
};

// class TimelineEffect_PositionY
public class TimelineEffect_PositionY : TimelineEffect
{
	public TimelineEffect_PositionY()
		: base() {  }

	public TimelineEffect_PositionY( float from, float to, E_LerpType lerpType )
		: base( from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ Widget.Position.Y += valueDiff; }
};

// class TimelineEffect_PositionZ
public class TimelineEffect_PositionZ : TimelineEffect
{
	public TimelineEffect_PositionZ()
		: base() {  }

	public TimelineEffect_PositionZ( float from, float to, E_LerpType lerpType )
		: base( from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ Widget.Position.Z += valueDiff; }
};

// class TimelineEffect_SizeX
public class TimelineEffect_SizeX : TimelineEffect
{
	public TimelineEffect_SizeX()
		: base() {  }

	public TimelineEffect_SizeX( float from, float to, E_LerpType lerpType )
		: base( from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ Widget.Size.X += valueDiff; }
};

// class TimelineEffect_SizeY
public class TimelineEffect_SizeY : TimelineEffect
{
	public TimelineEffect_SizeY()
		: base() {  }

	public TimelineEffect_SizeY( float from, float to, E_LerpType lerpType )
		: base( from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ Widget.Size.Y += valueDiff; }
};

// class TimelineEffect_SizeZ
public class TimelineEffect_SizeZ : TimelineEffect
{
	public TimelineEffect_SizeZ()
		: base() {  }

	public TimelineEffect_SizeZ( float from, float to, E_LerpType lerpType )
		: base( from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ Widget.Size.Z += valueDiff; }
};

// class TimelineEffect_ScaleX
public class TimelineEffect_ScaleX : TimelineEffect
{
	public TimelineEffect_ScaleX()
		: base() {  }

	public TimelineEffect_ScaleX( float from, float to, E_LerpType lerpType )
		: base( from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ Widget.Scale.X += valueDiff; }
};

// class TimelineEffect_ScaleY
public class TimelineEffect_ScaleY : TimelineEffect
{
	public TimelineEffect_ScaleY()
		: base() {  }

	public TimelineEffect_ScaleY( float from, float to, E_LerpType lerpType )
		: base( from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ Widget.Scale.Y += valueDiff; }
};

// class TimelineEffect_RotationX
public class TimelineEffect_RotationX : TimelineEffect
{
	public TimelineEffect_RotationX()
		: base() {  }

	public TimelineEffect_RotationX( float from, float to, E_LerpType lerpType )
		: base( from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ Widget.Rotation.X += valueDiff; }
};

// class TimelineEffect_RotationY
public class TimelineEffect_RotationY : TimelineEffect
{
	public TimelineEffect_RotationY()
		: base() {  }

	public TimelineEffect_RotationY( float from, float to, E_LerpType lerpType )
		: base( from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ Widget.Rotation.Y += valueDiff; }
};

// class TimelineEffect_RotationZ
public class TimelineEffect_RotationZ : TimelineEffect
{
	public TimelineEffect_RotationZ()
		: base() {  }

	public TimelineEffect_RotationZ( float from, float to, E_LerpType lerpType )
		: base( from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ Widget.Rotation.Z += valueDiff; }
};

// class TimelineEffect_ColorLerp
public class TimelineEffect_ColorLerp : TimelineEffect
{
	public TimelineEffect_ColorLerp()
		: base() {  }

	public TimelineEffect_ColorLerp( SpriteColors colorTo, E_LerpType lerpType )
		: base( 0.0f, 1.0f, lerpType )	{ ColorTo = colorTo; }

	// CopyTo
	protected override void CopyTo( TimelineEffect o )
	{
		base.CopyTo( o );

		TimelineEffect_ColorLerp oo = (TimelineEffect_ColorLerp)o;

		oo.ColorFrom = ColorFrom;
		oo.ColorTo = ColorTo;
	}

	protected override void OnBind()						{ ColorFrom = Widget.ColorBase; }	// TODO - won't be correct if you manually change the base color after binding - humph
	protected override void OnChange( float valueDiff )		{ Widget.ColorBase.Lerp( ref ColorFrom, ref ColorTo, Value ); Widget.FlagSet( E_WidgetFlag.DirtyColor ); } // ColorLerp effects don't stack

	//
	private SpriteColors		ColorFrom;
	private SpriteColors		ColorTo;
	//
};

// class TimelineEffect_Alpha
public class TimelineEffect_Alpha : TimelineEffect
{
	public TimelineEffect_Alpha()
		: base() {  }

	public TimelineEffect_Alpha( float from, float to, E_LerpType lerpType )
		: base( from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ Widget.Alpha += valueDiff; }
};

// class TimelineEffect_Intensity
public class TimelineEffect_Intensity : TimelineEffect
{
	public TimelineEffect_Intensity()
		: base() {  }

	public TimelineEffect_Intensity( float from, float to, E_LerpType lerpType )
		: base( from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ Widget.Intensity += valueDiff; }
};

// class TimelineEffect_EffectParamF
public class TimelineEffect_EffectParamF : TimelineEffect
{
	public TimelineEffect_EffectParamF()
		: base() {  }

	public TimelineEffect_EffectParamF( float from, float to, E_LerpType lerpType )
		: base( from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ Widget.RenderState.EffectParamF += valueDiff; }
}

// class TimelineEffect_TextureUV
public abstract class TimelineEffect_TextureUV : TimelineEffect
{
	public TimelineEffect_TextureUV()
		: base() {  }

	public TimelineEffect_TextureUV( int slot, float from, float to, E_LerpType lerpType )
		: base( from, to, lerpType )	{ Slot = slot; }

	// CopyTo
	protected override void CopyTo( TimelineEffect o )
	{
		base.CopyTo( o );

		TimelineEffect_TextureUV oo = (TimelineEffect_TextureUV)o;

		oo.Slot = Slot;
	}

	//
	protected int		Slot;
	//
};

// class TimelineEffect_TexturePU
public class TimelineEffect_TexturePU : TimelineEffect_TextureUV
{
	public TimelineEffect_TexturePU()
		: base() {  }

	public TimelineEffect_TexturePU( int slot, float from, float to, E_LerpType lerpType )
		: base( slot, from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ SpriteTexture t = Widget.GetTexture( Slot ); t.PUV.X += valueDiff; Widget.ChangeTexture( Slot, ref t ); }
};

// class TimelineEffect_TexturePV
public class TimelineEffect_TexturePV : TimelineEffect_TextureUV
{
	public TimelineEffect_TexturePV()
		: base() {  }

	public TimelineEffect_TexturePV( int slot, float from, float to, E_LerpType lerpType )
		: base( slot, from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ SpriteTexture t = Widget.GetTexture( Slot ); t.PUV.Y += valueDiff; Widget.ChangeTexture( Slot, ref t ); }
};

// class TimelineEffect_TextureSU
public class TimelineEffect_TextureSU : TimelineEffect_TextureUV
{
	public TimelineEffect_TextureSU()
		: base() {  }

	public TimelineEffect_TextureSU( int slot, float from, float to, E_LerpType lerpType )
		: base( slot, from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ SpriteTexture t = Widget.GetTexture( Slot ); t.SUV.X += valueDiff; Widget.ChangeTexture( Slot, ref t ); }
};

// class TimelineEffect_TextureSV
public class TimelineEffect_TextureSV : TimelineEffect_TextureUV
{
	public TimelineEffect_TextureSV()
		: base() {  }

	public TimelineEffect_TextureSV( int slot, float from, float to, E_LerpType lerpType )
		: base( slot, from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ SpriteTexture t = Widget.GetTexture( Slot ); t.SUV.Y += valueDiff; Widget.ChangeTexture( Slot, ref t ); }
};

// class TimelineEffect_MenuDirectionX
public class TimelineEffect_MenuDirectionX : TimelineEffect
{
	public TimelineEffect_MenuDirectionX()
		: base() {  }

	public TimelineEffect_MenuDirectionX( float from, float to, E_LerpType lerpType )
		: base( from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ ((WidgetMenuScroll)Widget).Direction.X += valueDiff; }
};

// class TimelineEffect_MenuDirectionY
public class TimelineEffect_MenuDirectionY : TimelineEffect
{
	public TimelineEffect_MenuDirectionY()
		: base() {  }

	public TimelineEffect_MenuDirectionY( float from, float to, E_LerpType lerpType )
		: base( from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ ((WidgetMenuScroll)Widget).Direction.Y += valueDiff; }
};

// class TimelineEffect_MenuDirectionZ
public class TimelineEffect_MenuDirectionZ : TimelineEffect
{
	public TimelineEffect_MenuDirectionZ()
		: base() {  }

	public TimelineEffect_MenuDirectionZ( float from, float to, E_LerpType lerpType )
		: base( from, to, lerpType )	{  }

	protected override void OnChange( float valueDiff )		{ ((WidgetMenuScroll)Widget).Direction.Z += valueDiff; }
};

// class TimelineEffect_FontStyleTrackingPercentage
public class TimelineEffect_FontStyleTrackingPercentage : TimelineEffect
{
	public TimelineEffect_FontStyleTrackingPercentage()
		: base() {  }

	public TimelineEffect_FontStyleTrackingPercentage( FontStyle fontStyle, float from, float to, E_LerpType lerpType )
		: base( from, to, lerpType )	{ FontStyle = fontStyle; }

	// CopyTo
	protected override void CopyTo( TimelineEffect o )
	{
		base.CopyTo( o );

		TimelineEffect_FontStyleTrackingPercentage oo = (TimelineEffect_FontStyleTrackingPercentage)o;

		oo.FontStyle = FontStyle;
	}

	protected override void OnChange( float valueDiff )		{ FontStyle.TrackingPercentage += valueDiff; }

	//
	private FontStyle		FontStyle;
	//
};

}; // namespace UI
