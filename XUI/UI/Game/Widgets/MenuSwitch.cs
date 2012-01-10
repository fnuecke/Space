//-----------------------------------------------
// XUI - MenuSwitch.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

namespace XUI.UI
{

// class WidgetMenuSwitch
public class WidgetMenuSwitch : WidgetMenuBase
{
	// WidgetMenuSwitch
	public WidgetMenuSwitch()
		: this( E_MenuType.Vertical )
	{
		//
	}

	public WidgetMenuSwitch( E_MenuType type )
		: base( type )
	{
		DeactivateNodes = true;
	}

	// CopyTo
	protected override void CopyTo( WidgetBase o )
	{
		base.CopyTo( o );

		WidgetMenuSwitch oo = (WidgetMenuSwitch)o;

		oo.DeactivateNodes = DeactivateNodes;
	}

	// OnInit
	protected override void OnInit()
	{
		base.OnInit();

		if ( DeactivateNodes )
			for ( int i = 0; i < Nodes.Count; ++i )
				Nodes[ i ].Active( ( i == CurrentNode ), true );
	}

	// OnChange
	protected override void OnChange( int oldNode, int newNode )
	{
		base.OnChange( oldNode, newNode );

		if ( DeactivateNodes )
		{
			for ( int i = 0; i < Nodes.Count; ++i )
			{
				Nodes[ oldNode ].Active( false, true );
				Nodes[ newNode ].Active( true, true );
			}
		}
	}

	//
	public bool		DeactivateNodes;
	//
};

}; // namespace UI
