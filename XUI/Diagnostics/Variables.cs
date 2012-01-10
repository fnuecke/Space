//-----------------------------------------------
// XUI - Variables.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using Microsoft.Xna.Framework;

// TODO - "disable" debug vars ifdef RELEASE

namespace XUI.Diagnostics
{

// E_VarType
public enum E_VarType
{
	Bool = 0,
	Int,
	Float,
	Enum,
	Function,
	String,
	Color,
	
	Count,
};

// class VarBaseD
public abstract class VarBaseD
{
	// VarBaseD
	public VarBaseD( E_VarType type )
	{
		Type = type;
		Name = null;
	}
	
	public VarBaseD( E_VarType type, string name )
		: this( type )
	{
		Init( name );
	}
	
	public void Init( string name )
	{
		Name = name;
		
		// validate name - may expand on this
		bool bValidName = true;
		
		for ( int i = 0; i < name.Length; ++i )
		{
			if ( ( ( i == 0 ) && ( name[ i ]  == '.' ) ) // can't begin with .
				|| ( ( i == ( name.Length - 1 ) ) && ( name[ i ]  == '.' ) ) // can't end with .
				|| ( ( i != ( name.Length - 1 ) ) && ( name[ i ]  == '.' ) && ( name[ i + 1 ]  == '.' ) ) ) // can't have two .'s together
			{
				bValidName = false;
				break;
			}
		}
		
		if ( bValidName )
			DebugVariables.Add( this );
	}
	
	//
	public E_VarType	Type;
	public string		Name;
	public VarBaseD		Next;
	//
};

// class BoolD
public class BoolD : VarBaseD
{
	// BoolD
	public BoolD( string name, bool v )
		: base( E_VarType.Bool, name )
	{
		Default = v;
		Value = Default;
	}

	public BoolD( string name )
		: base( E_VarType.Bool, name )
	{
		//
	}
	
	// operator bool
	public static implicit operator bool( BoolD v )
	{
		return v.Value;
	}
	
	//
	public bool		Default;
	public bool		Value;
	//
};

// class IntD
public class IntD : VarBaseD
{
	// IntD
	public IntD( string name, int v )
		: this( name, v, int.MinValue, int.MaxValue, 1 )
	{
		//
	}
	
	public IntD( string name, int v, int min, int max )
		: this( name, v, min, max, 1 )
	{
		//
	}
	
	public IntD( string name, int v, int min, int max, int step )
		: base( E_VarType.Int, name )
	{
		Default = v;
		Value = Default;
		Min = min;
		Max = max;
		Step = step;
	}

	public IntD( string name )
		: base( E_VarType.Int, name )
	{
		//
	}
	
	// operator int
	public static implicit operator int( IntD v )
	{
		return v.Value;
	}
	
	//
	public int		Default;
	public int		Value;
	public int		Min;
	public int		Max;
	public int		Step;
	//
};

// class FloatD
public class FloatD : VarBaseD
{
	// FloatD
	public FloatD( string name, float v )
		: this( name, v, float.MinValue, float.MaxValue, 1.0f )
	{
		//
	}
	
	public FloatD( string name, float v, float min, float max )
		: this( name, v, min, max, ( max - min ) / 10.0f )
	{
		//
	}
	
	public FloatD( string name, float v, float min, float max, float step )
		: base( E_VarType.Float, name )
	{
		Default = v;
		Value = Default;
		Min = min;
		Max = max;
		Step = step;
	}

	public FloatD( string name )
		: base( E_VarType.Float, name )
	{
		//
	}
	
	// operator float
	public static implicit operator float( FloatD v )
	{
		return v.Value;
	}
	
	//
	public float	Default;
	public float	Value;
	public float	Min;
	public float	Max;
	public float	Step;
	//
};

// class EnumOptionD
public class EnumOptionD
{
	// EnumOptionD
	public EnumOptionD( string name, int v )
	{
		Name = name;
		Value = v;
	}
	
#if TOOL
	// ToString
	public override string ToString()
	{
		return Name;
	}
#endif
	
	//
	public string	Name;
	public int		Value;
	//
};

// class EnumD
public class EnumD : VarBaseD
{
	// EnumD
	public EnumD( string name, int v, params EnumOptionD[] args )
		: base( E_VarType.Enum, name )
	{
		Options = new EnumOptionD[ args.Length ];
		
		for ( int i = 0; i < args.Length; ++i )
		{
			Options[ i ] = args[ i ];
			
			if ( Options[ i ].Value == v )
				Value = i;
		}
		
		Default = Value;
	}

	public EnumD( string name )
		: base( E_VarType.Enum, name )
	{
		//
	}
	
	// operator int
	public static implicit operator int( EnumD v )
	{
		return v.Options[ v.Value ].Value;
	}
	
	//
	public int					Default;
	public int					Value;
	public EnumOptionD[]		Options;
	//
};

// class FunctionD
public class FunctionD : VarBaseD
{
	//
	public delegate void Func();
	//
	
	// FunctionD
	public FunctionD( string name, Func f )
		: base( E_VarType.Function, name )
	{
		F = f;
	}

	public FunctionD( string name )
		: this( name, null )
	{
		//
	}
	
	//
	public Func		F;
	//
};

// class StringD
public class StringD : VarBaseD
{
	// StringD
	public StringD( string name, string v )
		: base( E_VarType.String, name )
	{
		Default = v;
		Value = Default;
	}

	public StringD( string name )
		: this( name, null )
	{
		//
	}

	// operator string
	public static implicit operator string( StringD v )
	{
		return v.Value;
	}

	//
	public string	Default;
	public string	Value;
	//
};

// class ColorD
public class ColorD : VarBaseD
{
	// ColorD
	public ColorD( string name, uint v )
		: base( E_VarType.Color, name )
	{
		Default = v;
		Value = Default;
	}

	public ColorD( string name, byte a, byte r, byte g, byte b )
		: this( name, (uint)( ( a << 24 ) | ( r << 16 ) | ( g << 8 ) | b ) )
	{
		//
	}

	public ColorD( string name )
		: this( name, 0xffffffff )
	{
		//
	}

	// operator Color
	public static implicit operator Color( ColorD v )
	{
		return new Color( v.R, v.G, v.B, v.A );
	}

	public byte		A			{ get { return (byte)( Value >> 24 ); }					set { Value &= 0x00ffffff; Value |= ( (uint)value << 24 ); }  }
	public byte		R			{ get { return (byte)( ( Value >> 16 ) & 0xff ); }		set { Value &= 0xff00ffff; Value |= ( (uint)value << 16 ); }  }
	public byte		G			{ get { return (byte)( ( Value >> 8 ) & 0xff ); }		set { Value &= 0xffff00ff; Value |= ( (uint)value << 8 ); }  }
	public byte		B			{ get { return (byte)( Value & 0xff ); }				set { Value &= 0xffffff00; Value |= value; }  }

	public byte		DefaultA	{ get { return (byte)( Default >> 24 ); }				}
	public byte		DefaultR	{ get { return (byte)( ( Default >> 16 ) & 0xff ); }	}
	public byte		DefaultG	{ get { return (byte)( ( Default >> 8 ) & 0xff ); }		}
	public byte		DefaultB	{ get { return (byte)( Default & 0xff ); }				}

	//
	public uint		Default;
	public uint		Value;
	//
};

// class DebugVariables
public static class DebugVariables
{
	// Add
	public static void Add( VarBaseD v )
	{
		if ( Root == null )
			Root = v;
		else
		{
			v.Next = Root;
			Root = v;
		}
	}
	
	// Clear
	public static void Clear()
	{
		Root = null;
	}
	
	//
	public static VarBaseD		Root;
	//
};

}; // namespace Debug
