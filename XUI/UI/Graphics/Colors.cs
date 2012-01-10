//-----------------------------------------------
// XUI - Colors.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System;
using Microsoft.Xna.Framework;

namespace XUI.UI
{

// struct SpriteColor
public struct SpriteColor
{
	// SpriteColor
	public SpriteColor( uint c )
	{
		A = (byte)( c >> 24 );
		R = (byte)( ( c >> 16 ) & 0xff );
		G = (byte)( ( c >> 8 ) & 0xff );
		B = (byte)( c & 0xff );
	}

	public SpriteColor( byte a, byte r, byte g, byte b )
	{
		A = a;
		R = r;
		G = g;
		B = b;
	}

	public SpriteColor( Color c )
	{
		A = c.A;
		R = c.R;
		G = c.G;
		B = c.B;
	}

	// Lerp
	public void Lerp( SpriteColor from, SpriteColor to, float f01 )
	{
		byte f = (byte)( f01 * 255.0f );

		A = (byte)( from.A + ( ( to.A - from.A ) * f ) / 255 );
		R = (byte)( from.R + ( ( to.R - from.R ) * f ) / 255 );
		G = (byte)( from.G + ( ( to.G - from.G ) * f ) / 255 );
		B = (byte)( from.B + ( ( to.B - from.B ) * f ) / 255 );
	}

	public static implicit operator SpriteColor( Color c )
	{
		return new SpriteColor( c );
	}

	public static implicit operator SpriteColor( uint c )
	{
		return new SpriteColor( c );
	}

	public static implicit operator uint( SpriteColor c )
	{
		return (uint)( ( c.A << 24 ) | ( c.R << 16 ) | ( c.G << 8 ) | c.B );
	}

	//
	public byte		A;
	public byte		R;
	public byte		G;
	public byte		B;
	//
};

// struct SpriteColors
public struct SpriteColors
{
	// SpriteColors
	public SpriteColors( SpriteColor c )
	{
		PreMultipliedAlpha = false;

		Color0 = c;
		Color1 = c;
		Color2 = c;
		Color3 = c;
	}

	public SpriteColors( SpriteColor c0, SpriteColor c1, SpriteColor c2, SpriteColor c3 )
	{
		PreMultipliedAlpha = false;

		Color0 = c0;
		Color1 = c1;
		Color2 = c2;
		Color3 = c3;
	}

	// Lerp
	public void Lerp( ref SpriteColors from, ref SpriteColors to, float f01 )
	{
		Color0.Lerp( from.Color0, to.Color0, f01 );
		Color1.Lerp( from.Color1, to.Color1, f01 );
		Color2.Lerp( from.Color2, to.Color2, f01 );
		Color3.Lerp( from.Color3, to.Color3, f01 );
	}

	// Set
	public void Set( ref SpriteColors colors, float alpha, float intensity )
	{
		alpha = MathHelper.Clamp( alpha, 0.0f, 1.0f );
		intensity = MathHelper.Clamp( intensity, 0.0f, 2.0f );

		PreMultipliedAlpha = false;

		byte a = (byte)( alpha * 255.0f );

		Color0.A = (byte)( ( colors.Color0.A * a ) / 255 );
		Color1.A = (byte)( ( colors.Color1.A * a ) / 255 );
		Color2.A = (byte)( ( colors.Color2.A * a ) / 255 );
		Color3.A = (byte)( ( colors.Color3.A * a ) / 255 );

		if ( intensity == 1.0f )
		{
			Color0.R = colors.Color0.R;
			Color0.G = colors.Color0.G;
			Color0.B = colors.Color0.B;
			
			Color1.R = colors.Color1.R;
			Color1.G = colors.Color1.G;
			Color1.B = colors.Color1.B;
			
			Color2.R = colors.Color2.R;
			Color2.G = colors.Color2.G;
			Color2.B = colors.Color2.B;
			
			Color3.R = colors.Color3.R;
			Color3.G = colors.Color3.G;
			Color3.B = colors.Color3.B;
		}
		else
		if ( intensity < 1.0f )
		{
			byte i = (byte)( intensity * 255.0f );

			Color0.R = (byte)( ( colors.Color0.R * i ) / 255 );
			Color0.G = (byte)( ( colors.Color0.G * i ) / 255 );
			Color0.B = (byte)( ( colors.Color0.B * i ) / 255 );
			
			Color1.R = (byte)( ( colors.Color1.R * i ) / 255 );
			Color1.G = (byte)( ( colors.Color1.G * i ) / 255 );
			Color1.B = (byte)( ( colors.Color1.B * i ) / 255 );
			
			Color2.R = (byte)( ( colors.Color2.R * i ) / 255 );
			Color2.G = (byte)( ( colors.Color2.G * i ) / 255 );
			Color2.B = (byte)( ( colors.Color2.B * i ) / 255 );
			
			Color3.R = (byte)( ( colors.Color3.R * i ) / 255 );
			Color3.G = (byte)( ( colors.Color3.G * i ) / 255 );
			Color3.B = (byte)( ( colors.Color3.B * i ) / 255 );
		}
		else // if ( intensity > 1.0f )
		{
			byte i = (byte)( ( intensity - 1.0f ) * 255.0f );

			Color0.R = (byte)Math.Min( colors.Color0.R + i, 255 );
			Color0.G = (byte)Math.Min( colors.Color0.G + i, 255 );
			Color0.B = (byte)Math.Min( colors.Color0.B + i, 255 );

			Color1.R = (byte)Math.Min( colors.Color1.R + i, 255 );
			Color1.G = (byte)Math.Min( colors.Color1.G + i, 255 );
			Color1.B = (byte)Math.Min( colors.Color1.B + i, 255 );

			Color2.R = (byte)Math.Min( colors.Color2.R + i, 255 );
			Color2.G = (byte)Math.Min( colors.Color2.G + i, 255 );
			Color2.B = (byte)Math.Min( colors.Color2.B + i, 255 );

			Color3.R = (byte)Math.Min( colors.Color3.R + i, 255 );
			Color3.G = (byte)Math.Min( colors.Color3.G + i, 255 );
			Color3.B = (byte)Math.Min( colors.Color3.B + i, 255 );
		}
	}

	// A
	public void A( byte a )
	{
		Color0.A = a;
		Color1.A = a;
		Color2.A = a;
		Color3.A = a;
	}

	// R
	public void R( byte r )
	{
		Color0.R = r;
		Color1.R = r;
		Color2.R = r;
		Color3.R = r;
	}

	// G
	public void G( byte g )
	{
		Color0.G = g;
		Color1.G = g;
		Color2.G = g;
		Color3.G = g;
	}

	// B
	public void B( byte b )
	{
		Color0.B = b;
		Color1.B = b;
		Color2.B = b;
		Color3.B = b;
	}

	// HasAlpha
	public bool HasAlpha()
	{
		return ( ( Color0.A != 0 ) || ( Color1.A != 0 ) || ( Color2.A != 0 ) || ( Color3.A != 0 ) );
	}

	// ToPremultiplied
	public void ToPremultiplied()
	{
		PreMultipliedAlpha = true;
		MultRGB( Color0.A, Color1.A, Color2.A, Color3.A );
	}

	// MultRGB
	private void MultRGB( byte f0, byte f1, byte f2, byte f3 )
	{
		Color0.R = (byte)( ( Color0.R * f0 ) / 255 );
		Color0.G = (byte)( ( Color0.G * f0 ) / 255 );
		Color0.B = (byte)( ( Color0.B * f0 ) / 255 );

		Color1.R = (byte)( ( Color1.R * f1 ) / 255 );
		Color1.G = (byte)( ( Color1.G * f1 ) / 255 );
		Color1.B = (byte)( ( Color1.B * f1 ) / 255 );

		Color2.R = (byte)( ( Color2.R * f2 ) / 255 );
		Color2.G = (byte)( ( Color2.G * f2 ) / 255 );
		Color2.B = (byte)( ( Color2.B * f2 ) / 255 );

		Color3.R = (byte)( ( Color3.R * f3 ) / 255 );
		Color3.G = (byte)( ( Color3.G * f3 ) / 255 );
		Color3.B = (byte)( ( Color3.B * f3 ) / 255 );
	}

	// MultA
	public void MultA( byte f )
	{
		Color0.A = (byte)( ( Color0.A * f ) / 255 );
		Color1.A = (byte)( ( Color1.A * f ) / 255 );
		Color2.A = (byte)( ( Color2.A * f ) / 255 );
		Color3.A = (byte)( ( Color3.A * f ) / 255 );

		if ( PreMultipliedAlpha )
			MultRGB( f, f, f, f );
	}

	public void MultA( ref SpriteColors c )
	{
		Color0.A = (byte)( ( Color0.A * c.Color0.A ) / 255 );
		Color1.A = (byte)( ( Color1.A * c.Color1.A ) / 255 );
		Color2.A = (byte)( ( Color2.A * c.Color2.A ) / 255 );
		Color3.A = (byte)( ( Color3.A * c.Color3.A ) / 255 );

		if ( PreMultipliedAlpha )
			MultRGB( c.Color0.A, c.Color1.A, c.Color2.A, c.Color3.A );
	}

	// CopyA
	public void CopyA( ref SpriteColors c )
	{
		Color0.A = c.Color0.A;
		Color1.A = c.Color1.A;
		Color2.A = c.Color2.A;
		Color3.A = c.Color3.A;
	}

	public static implicit operator SpriteColors( SpriteColor c )
	{
		return new SpriteColors( c );
	}

	public static implicit operator SpriteColors( Color c )
	{
		return new SpriteColors( c );
	}

	public static implicit operator SpriteColors( uint c )
	{
		return new SpriteColors( c );
	}

	//
	public bool				PreMultipliedAlpha;

	public SpriteColor		Color0;
	public SpriteColor		Color1;
	public SpriteColor		Color2;
	public SpriteColor		Color3;
	//
};

}; // namespace UI
