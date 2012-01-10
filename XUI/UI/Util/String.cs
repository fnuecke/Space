//-----------------------------------------------
// XUI - String.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

// TODO - convert to commas etc ...

namespace XUI.UI
{

// class StringUI
public class StringUI
{
	// StringUI
	public StringUI( int size )
	{
		Size = size;
		Chars = new char[ Size ];
	}

	public StringUI( StringUI o )
		: this( o.Size )
	{
		Add( o );
	}

	// Clear
	public unsafe void Clear()
	{
		Length = 0;

		fixed ( char* pString = &Chars[ 0 ] )
			*pString = '\0';
	}

	// AddChars
	private unsafe void AddChars( char* value )
	{
		if ( Length == ( Size - 1 ) )
			return;

		fixed ( char* pString = &Chars[ Length ] )
		{
			char* s = pString;
			char* v = value;

			while ( ( *v != '\0' ) && ( Length != ( Size - 1 ) ) )
			{
				*s++ = *v++;
				++Length;
			}

			*s = '\0';
		}
	}

	// Add
	public unsafe void Add( char value )
	{
		if ( Length == ( Size - 1 ) )
			return;

		fixed ( char* pString = &Chars[ Length++ ] )
		{
			*pString = value;
			*( pString + 1 ) = '\0';
		}
	}

	public unsafe void Add( string value )
	{
		fixed ( char* pValue = value )
			AddChars( pValue );
	}

	public unsafe void Add( StringUI value )
	{
		AddChars( value );
	}

	public void Add( int value )
	{
		Add( value, 1 );
	}

	private unsafe void Add( int value, int minLength )
	{
		if ( Length == ( Size - 1 ) )
			return;

		bool negative = ( value < 0 );

		if ( negative )
			value = -value;

		fixed ( char* pString = &Chars[ Length ] )
		{
			char* s = pString;

			// add in backwards
			do
			{
				*s++ = (char)( '0' + ( value % 10 ) );
				++Length;
			}
			while ( ( ( value /= 10 ) != 0 ) && ( Length != ( Size - 1 ) ) );

			// leading zeros
			int numZeros = minLength - (int)( s - pString );

			for ( int i = 0; i < numZeros; ++i )
			{
				if ( Length == ( Size - 1 ) )
					break;

				*s++ = '0';
				++Length;
			}

			if ( negative && ( Length != ( Size - 1 ) ) )
			{
				*s++ = '-';
				++Length;
			}

			*s = '\0';

			// reverse
			for ( char* s0 = pString, sn = ( s - 1 ); s0 < sn; ++s0, --sn )
			{
				char t = *s0;
				*s0 = *sn;
				*sn = t;
			}
		}
	}

	public void Add( float value )
	{
		Add( value, -1 );
	}

	// NOTE: this isn't perfect but it should do fine for the range of values we want to use it for
	public unsafe void Add( float value, int precision )
	{
		if ( Length == ( Size - 1 ) )
			return;

		// decompose
		int bvalue = *((int*)&value);
		int sign = bvalue >> 31;
		int exp = ( bvalue >> 23 ) & 0xff;
		int exp2 = (int)( exp - 127 );
		int mant = bvalue & 0x7fffff;

		// zero
		if ( ( exp == 0 ) && ( mant == 0 ) )
		{
			if ( sign == 1 )
				Add( '-' );

			Add( '0' );

			if ( precision > 0 )
			{
				Add( '.' );

				for ( int i = 0; i < precision; ++i )
					Add( '0' );
			}

			return;
		}

		// infinity and NaN
		if ( exp == 255 )
		{
			if ( mant == 0 )
			{
				if ( sign == 1 )
					Add( "-Inf" );
				else
					Add( "Inf" );
			}
			else
			{
				mant >>= 22;

				if ( mant == 1 )
					Add( "QNaN" );
				else
					Add( "SNaN" );
			}

			return;
		}

		if ( exp2 >= 31 )
		{
			Add( '>' );
			return; // TODO - too large
		}

		if ( exp2 < -23 )
		{
			Add( '<' );
			return; // TODO - too small
		}

		if ( sign == 1 )
			Add( '-' );

		if ( exp != 0 )
			mant |= 0x800000; // normalised

		int num = 0;
		int frac = 0;

		if ( exp2 >= 23 )
			num = (int)( mant << ( exp2 - 23 ) );
		else
		if ( exp2 >= 0 )
		{
			num = (int)( mant >> ( 23 - exp2 ) );
			frac = (int)( ( mant << ( exp2 + 1 ) ) & 0xffffff );
		}
		else
			frac = (int)( ( mant & 0xffffff ) >> -( exp2 + 1 ) );

		int oldLength = Length;

		if ( num == 0 )
			Add( '0' );
		else
			Add( num, 1 );

		int diffLength = Length - oldLength;

		Add( '.' );

		int max = 7;

		if ( ( precision != -1 ) && ( precision < max ) )
			max = precision;

		if ( ( precision == -1 ) && ( exp2 > 0 ) && ( ( exp2 - diffLength ) < max ) )
			max = ( exp2 - diffLength );

		for ( int i = 0; i < max; ++i )
		{
			frac = ( frac << 3 ) + ( frac << 1 );
			char c = (char)( ( frac >> 24 ) + '0' );
			Add( c );
			frac &= 0xffffff;

			if ( i == ( max - 1 ) ) // round last digit
			{
				int frac2 = ( frac << 3 ) + ( frac << 1 );
				int c2 = ( frac2 >> 24 );

				if ( ( c2 >= 5 ) && ( c != '9' ) )
					fixed ( char* pChar = &Chars[ Length - 1 ] )
						++*pChar;
			}
		}
	}

	// EqualTo
	public unsafe bool EqualTo( string other )
	{
		if ( Length != other.Length )
			return false;

		fixed ( char* pString = &Chars[ 0 ] )
		fixed ( char* pOther = other )
		{
			char* s = pString;
			char* o = pOther;

			for ( int i = 0; i < Length; ++i )
				if ( *s++ != *o++ )
					return false;
		}

		return true;
	}

	// indexer
	public char this[ int index ]		{ get { return Chars[ index ]; } }

	// char*
	public static unsafe implicit operator char*( StringUI s )
	{
		fixed ( char* pString = &s.Chars[ 0 ] )
			return pString;
	}

	//
	private int			Size;
	private char[]		Chars;
	public  int			Length		{ get; private set; }
	//
};

}; // namespace UI
