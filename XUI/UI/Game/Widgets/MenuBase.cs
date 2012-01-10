//-----------------------------------------------
// XUI - MenuBase.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System;
using System.Collections.Generic;

// TODO - handle locked flag in MenuNode once it's in

namespace XUI.UI
{

// E_MenuType
public enum E_MenuType
{
	Horizontal,
	Vertical,

	Count,
};

// class WidgetMenuBase
public abstract class WidgetMenuBase : WidgetBase
{
	// WidgetMenuBase
	public WidgetMenuBase()
		: this( E_MenuType.Vertical )
	{
		//
	}

	public WidgetMenuBase( E_MenuType type )
		: base()
	{
		Type = type;
		Loop = false;

		Nodes = new List< WidgetMenuNode >();
		CurrentNode = 0;

		DeactivateArrows = true;
	}

	// CopyTo
	protected override void CopyTo( WidgetBase o )
	{
		base.CopyTo( o );

		WidgetMenuBase oo = (WidgetMenuBase)o;

		oo.Type = Type;
		oo.Loop = Loop;

		// Nodes - don't copy over

		oo.CurrentNode = CurrentNode;

		oo.DeactivateArrows = DeactivateArrows;

		// ArrowDecrease - doesn't copy over
		// ArrowIncrease - doesn't copy over
	}

	// OnInit
	protected override void OnInit()
	{
		for ( int i = 0; i < Children.Count; ++i )
			if ( Children[ i ] is WidgetMenuNode )
				Nodes.Add( (WidgetMenuNode)Children[ i ] );
	}

	// OnPostInit
	protected override void OnPostInit()
	{
		for ( int i = 0; i < Nodes.Count; ++i )
			Nodes[ i ].Selected( ( i == CurrentNode ), true, true );

		UpdateArrows( 0 );
	}

	// OnProcessInput
	protected override void OnProcessInput( Input input )
	{
		float delay = _UI.AutoRepeatDelay;
		float repeat = _UI.AutoRepeatRepeat;

		if ( input.ButtonAutoRepeat( (int)E_UiButton.Up, delay, repeat ) )
		{
			if ( Type == E_MenuType.Vertical )
				DecreaseCurrent();
		}
		else
		if ( input.ButtonAutoRepeat( (int)E_UiButton.Down, delay, repeat ) )
		{
			if ( Type == E_MenuType.Vertical )
				IncreaseCurrent();
		}
		else
		if ( input.ButtonAutoRepeat( (int)E_UiButton.Left, delay, repeat ) )
		{
			if ( Type == E_MenuType.Horizontal )
				DecreaseCurrent();
		}
		else
		if ( input.ButtonAutoRepeat( (int)E_UiButton.Right, delay, repeat ) )
		{
			if ( Type == E_MenuType.Horizontal )
				IncreaseCurrent();
		}
	}

	// OnUpdate
	protected override void OnUpdate( float frameTime )
	{
		UpdateArrows( 0 );
	}

	// IncreaseCurrent
	private void IncreaseCurrent()
	{
		int oldNode = CurrentNode++;

		if ( CurrentNode == Nodes.Count )
		{
			if ( Loop )
				CurrentNode = 0;
			else
				CurrentNode = oldNode;
		}

		if ( CurrentNode != oldNode )
			OnChange( oldNode, CurrentNode );
	}

	// DecreaseCurrent
	private void DecreaseCurrent()
	{
		int oldNode = CurrentNode--;

		if ( CurrentNode == -1 )
		{
			if ( Loop )
				CurrentNode = ( Nodes.Count - 1 );
			else
				CurrentNode = oldNode;
		}

		if ( CurrentNode != oldNode )
			OnChange( oldNode, CurrentNode );
	}

	// OnChange
	protected virtual void OnChange( int oldNode, int newNode )
	{
		Nodes[ oldNode ].Selected( false, true, true );
		Nodes[ newNode ].Selected( true, true, true );

		UpdateArrows( newNode - oldNode );
	}

	// UpdateArrows
	private void UpdateArrows( int amount )
	{
		if ( ( ArrowDecrease == null ) || ( ArrowIncrease == null ) )
			return;

		bool canDecrease = IsSelected && ( Loop || ( CurrentNode != 0 ) );
		bool canIncrease = IsSelected && ( Loop || ( CurrentNode != ( Nodes.Count - 1 ) ) );

		ArrowDecrease.Selected( canDecrease, false, true );
		ArrowIncrease.Selected( canIncrease, false, true );

		if ( DeactivateArrows )
		{
			ArrowDecrease.Active( canDecrease, false );
			ArrowIncrease.Active( canIncrease, false );
		}
		else
		{
			if ( Loop && ( Math.Abs( amount ) == ( Nodes.Count - 1 ) ) )
				amount = -amount;

			if ( amount < 0 )
				ArrowDecrease.TimelineReset( "nudge", true, 0.0f, false );
			else
			if ( amount > 0 )
				ArrowIncrease.TimelineReset( "nudge", true, 0.0f, false );
		}
	}

	// SetByIndex
	public void SetByIndex( int index )
	{
		int oldNode = CurrentNode;

		if ( ( index >= 0 ) && ( index < Nodes.Count ) )
			CurrentNode = index;

		if ( oldNode != CurrentNode )
			OnChange( oldNode, CurrentNode );
	}

	// SetByValue
	public void SetByValue( int value )
	{
		int oldNode = CurrentNode;

		for ( int i = 0; i < Nodes.Count; ++i )
		{
			if ( Nodes[ i ].Value == value )
			{
				CurrentNode = i;
				break;
			}
		}

		if ( oldNode != CurrentNode )
			OnChange( oldNode, CurrentNode );
	}

	// GetByIndex
	public int GetByIndex()
	{
		return CurrentNode;
	}

	// GetByValue
	public int GetByValue()
	{
		return Nodes[ CurrentNode ].Value;
	}

	// OnSelected
	protected override void OnSelected( bool value )
	{
		Nodes[ CurrentNode ].Selected( value, true, true );
		UpdateArrows( 0 );
	}

	//
	protected E_MenuType					Type;
	public    bool							Loop;

	protected List< WidgetMenuNode >		Nodes;
	protected int							CurrentNode;

	public    bool							DeactivateArrows;
	public    WidgetBase					ArrowDecrease;
	public    WidgetBase					ArrowIncrease;
	//
};

}; // namespace UI
