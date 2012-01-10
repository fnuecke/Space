//-----------------------------------------------
// XUI - Menu.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System;
using System.Text;

namespace XUI.Diagnostics
{

// class MenuItem
public class MenuItem
{
	// MenuItem
	public MenuItem( string name )
	{
		Name = name;
	}
	
	public MenuItem( string name, VarBaseD var )
		: this( name )
	{
		Var = var;
	}
	
	// Add
	public void Add( MenuItem v )
	{
		MenuItem i = Child1;
		MenuItem iPrev = null;
		
		while ( i != null )
		{
			if ( String.Compare( v.Name, i.Name ) < 0 ) // alpha sort
				break;
			
			iPrev = i;
			i = i.Next;
		}
		
		// add in-between iPrev and i
		v.Prev = iPrev;
		v.Next = i;
		
		if ( iPrev != null )
			iPrev.Next = v;
		else
			Child1 = v;
		
		if ( i != null )
			i.Prev = v;
		
		v.Parent = this;
	}
	
	// AsString
	public virtual StringBuilder AsString( StringBuilder s )
	{
		s.Length = 0;

		if ( Var == null )
			s.Append( ( Child1 != null ) ? "=>" : "" );
		else
		{
			switch ( Var.Type )
			{
				case E_VarType.Bool:		{ BoolD v = (BoolD)Var;			s.Append( v.Value ? "true" : "false" );								break; }
				case E_VarType.Int:			{ IntD v = (IntD)Var;			s.AppendFormat( "{0:#,0}", v.Value );								break; }
				case E_VarType.Float:		{ FloatD v = (FloatD)Var;		s.AppendFormat( "{0:#,0.0####}", v.Value );							break; }
				case E_VarType.Enum:		{ EnumD v = (EnumD)Var;			s.Append( v.Options[ v.Value ].Name );								break; }
				case E_VarType.Function:	{								s.Append( "-call-" );												break; }
				case E_VarType.String:		{ StringD v = (StringD)Var;		s.Append( v.Value );												break; }
				case E_VarType.Color:		{ ColorD v = (ColorD)Var;		s.AppendFormat( "A:{0} R:{1} G:{2} B:{3}", v.A, v.R, v.G, v.B );	break; }
			}
		}

		return s;
	}
	
	//
	public string		Name;
	public VarBaseD		Var;

	public MenuItem		Next;
	public MenuItem		Prev;
	public MenuItem		Parent;
	public MenuItem		Child1;
	//
};

// class Menu
public class Menu
{
	// Menu
	public Menu()
	{
		Active = false;
		
		// create root
		Root = new MenuItem( "Root" );
		Current = Root;

		Init();
	}
	
	// Init
	public void Init()
	{
		// add the vars
		for ( VarBaseD v = DebugVariables.Root; v != null; v = v.Next )
		{
			string[] items = v.Name.Split( '.' );
			MenuItem c = Root;
			
			// drill down - adding new levels if required
			for ( int i = 0; i < items.Length; ++i )
			{
				// attempt to find at this level
				MenuItem ci = null;
				
				for ( ci = c.Child1; ci != null; ci = ci.Next )
				{
					if ( items[ i ] == ci.Name )
					{
						if ( ( i == ( items.Length - 1 ) ) || ( c.Child1 == null ) )
							ci = null; // we've got a duplicate between a level and var or two vars with the same name
						
						break;
					}
				}
				
				if ( ci == null )
				{
					// add new
					if ( i == ( items.Length - 1 ) )
						ci = new MenuItem( items[ i ], v ); // item
					else
						ci = new MenuItem( items[ i ] ); // submenu
					
					c.Add( ci );
				}
				
				// move down to new level
				c = ci;
			}
		}
	}

	// Reset
	public void Reset()
	{
		Root.Child1 = null;
		Init();
	}
	
	// Update
	public bool Update( float frameTime )
	{
		bool oldActive = Active;
		
		OnUpdate( frameTime );
		
		return ( Active || oldActive ); // frame delayed exit as most games use the start button
	}
	
	// OnUpdate
	protected virtual void OnUpdate( float frameTime )
	{
		//
	}
	
	// Next
	protected void Next()
	{
		if ( Current.Child1 != null )
		{
			Current = Current.Child1;
			OnNext();
		}
		else
		{
			if ( ( Current.Var != null ) && ( Current.Var.Type == E_VarType.Function ) )
			{
				FunctionD v = (FunctionD)Current.Var;

				if ( v.F != null )
					v.F();
			}
		}
	}
	
	// OnNext
	protected virtual void OnNext()
	{
		//
	}
	
	// Back
	protected void Back()
	{
		if ( Current.Parent != null )
		{
			Current = Current.Parent;
			OnBack();
		}
		else
			Active = false;
	}
	
	// OnBack
	protected virtual void OnBack()
	{
		//
	}
	
	// Up
	protected void Up()
	{
		if ( Current.Prev != null )
			Current = Current.Prev;
		else
			while ( Current.Next != null )
				Current = Current.Next; // wrap to bottom
	}
	
	// Down
	protected void Down()
	{
		if ( Current.Next != null )
			Current = Current.Next;
		else
			while ( Current.Prev != null )
				Current = Current.Prev; // wrap to top
	}

	// CanIncrease
	protected bool CanIncrease()
	{
		if ( Current.Var == null )
			return false;

		bool canIncrease = false;

		switch ( Current.Var.Type )
		{
			case E_VarType.Bool:	{										canIncrease = true;										break; }
			case E_VarType.Int:		{ IntD v = (IntD)Current.Var;			canIncrease = ( v.Value < v.Max );						break; }
			case E_VarType.Float:	{ FloatD v = (FloatD)Current.Var;		canIncrease = ( v.Value < v.Max );						break; }
			case E_VarType.Enum:	{ EnumD v = (EnumD)Current.Var;			canIncrease = ( v.Value < ( v.Options.Length - 1 ) );	break; }
		}

		return canIncrease;
	}
	
	// Increase
	protected void Increase()
	{
		if ( !CanIncrease() )
			return;

		switch ( Current.Var.Type )
		{
			case E_VarType.Bool:	{ BoolD v = (BoolD)Current.Var;			v.Value = !v.Value;											break; }
			case E_VarType.Int:		{ IntD v = (IntD)Current.Var;			v.Value += v.Step; if ( v.Value > v.Max ) v.Value = v.Max;	break; }
			case E_VarType.Float:	{ FloatD v = (FloatD)Current.Var;		v.Value += v.Step; if ( v.Value > v.Max ) v.Value = v.Max;	break; }
			case E_VarType.Enum:	{ EnumD v = (EnumD)Current.Var;			++v.Value;													break; }
		}
	}

	// CanDecrease
	protected bool CanDecrease()
	{
		if ( Current.Var == null )
			return false;

		bool canDecrease = false;

		switch ( Current.Var.Type )
		{
			case E_VarType.Bool:	{										canDecrease = true;					break; }
			case E_VarType.Int:		{ IntD v = (IntD)Current.Var;			canDecrease = ( v.Value > v.Min );	break; }
			case E_VarType.Float:	{ FloatD v = (FloatD)Current.Var;		canDecrease = ( v.Value > v.Min );	break; }
			case E_VarType.Enum:	{ EnumD v = (EnumD)Current.Var;			canDecrease = ( v.Value > 0 );		break; }
		}

		return canDecrease;
	}
	
	// Decrease
	protected void Decrease()
	{
		if ( !CanDecrease() )
			return;

		switch ( Current.Var.Type )
		{
			case E_VarType.Bool:	{ BoolD v = (BoolD)Current.Var;			v.Value = !v.Value;											break; }
			case E_VarType.Int:		{ IntD v = (IntD)Current.Var;			v.Value -= v.Step; if ( v.Value < v.Min ) v.Value = v.Min;	break; }
			case E_VarType.Float:	{ FloatD v = (FloatD)Current.Var;		v.Value -= v.Step; if ( v.Value < v.Min ) v.Value = v.Min;	break; }
			case E_VarType.Enum:	{ EnumD v = (EnumD)Current.Var;			--v.Value;													break; }
		}
	}
	
	// ResetToDefault
	protected void ResetToDefault()
	{
		if ( Current.Var == null )
			return;

		switch ( Current.Var.Type )
		{
			case E_VarType.Bool:	{ BoolD v = (BoolD)Current.Var;			v.Value = v.Default;	break; }
			case E_VarType.Int:		{ IntD v = (IntD)Current.Var;			v.Value = v.Default;	break; }
			case E_VarType.Float:	{ FloatD v = (FloatD)Current.Var;		v.Value = v.Default;	break; }
			case E_VarType.Enum:	{ EnumD v = (EnumD)Current.Var;			v.Value = v.Default;	break; }
			case E_VarType.String:	{ StringD v = (StringD)Current.Var;		v.Value = v.Default;	break; }
			case E_VarType.Color:	{ ColorD v = (ColorD)Current.Var;		v.Value = v.Default;	break; }
		}
	}
	
	// Render
	public void Render()
	{
		if ( !Active )
			return;
		
		OnRender();
	}
	
	// OnRender
	protected virtual void OnRender()
	{
		//
	}
	
	//
	protected bool			Active;
	public    MenuItem		Root;
	protected MenuItem		Current;
	//
};

}; // namespace Debug
