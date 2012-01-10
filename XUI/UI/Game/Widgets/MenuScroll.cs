//-----------------------------------------------
// XUI - MenuScroll.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System;
using Microsoft.Xna.Framework;

namespace XUI.UI
{

// class WidgetMenuScroll
public class WidgetMenuScroll : WidgetMenuBase
{
	// WidgetMenuScroll
	public WidgetMenuScroll()
		: this( E_MenuType.Vertical )
	{
		//
	}

	public WidgetMenuScroll( E_MenuType type )
		: base( type )
	{
		//
	}

	// CopyTo
	protected override void CopyTo( WidgetBase o )
	{
		base.CopyTo( o );

		WidgetMenuScroll oo = (WidgetMenuScroll)o;

		oo.Direction = Direction;
		oo.Speed = Speed;
		oo.Padding = Padding;

		oo.CurrentOffset = CurrentOffset;
		oo.TargetOffset = TargetOffset;
	}

	// OnInit
	protected override void OnInit()
	{
		base.OnInit();

		if ( Type == E_MenuType.Horizontal )
			Direction = new Vector3( 1.0f, 0.0f, 0.0f );
		else
		if ( Type == E_MenuType.Vertical )
			Direction = new Vector3( 0.0f, 1.0f * UpY, 0.0f );
	}

	// OnUpdate
	protected override void OnUpdate( float frameTime )
	{
		base.OnUpdate( frameTime );

		CalculateTargetOffset();

		if ( ( Speed != 0.0f ) && ( CurrentOffset != TargetOffset ) )
		{
			float amount = Speed * frameTime;

			CurrentOffset.X += ( TargetOffset.X - CurrentOffset.X ) * amount;
			CurrentOffset.Y += ( TargetOffset.Y - CurrentOffset.Y ) * amount;
			CurrentOffset.Z += ( TargetOffset.Z - CurrentOffset.Z ) * amount;

			float diffX = Math.Abs( TargetOffset.X - CurrentOffset.X );
			float diffY = Math.Abs( TargetOffset.Y - CurrentOffset.Y );
			float diffZ = Math.Abs( TargetOffset.Z - CurrentOffset.Z );

			float diffE = 0.001f;

			if ( diffX < diffE )
				CurrentOffset.X = TargetOffset.X;
			if ( diffY < diffE )
				CurrentOffset.Y = TargetOffset.Y;
			if ( diffZ < diffE )
				CurrentOffset.Z = TargetOffset.Z;
		}

		Vector3 offset = new Vector3();

		for ( int i = 0; i < Nodes.Count; ++i )
		{
			WidgetMenuNode node = Nodes[ i ];

			node.Position.X = CurrentOffset.X + offset.X;
			node.Position.Y = CurrentOffset.Y + offset.Y;
			node.Position.Z = CurrentOffset.Z + offset.Z;

			offset.X += ( node.Size.X + Padding ) * Direction.X;
			offset.Y += ( node.Size.Y + Padding ) * Direction.Y;
			offset.Z += ( node.Size.Z + Padding ) * Direction.Z;
		}
	}

	// CalculateTargetOffset
	private void CalculateTargetOffset()
	{
		TargetOffset.X = 0.0f;
		TargetOffset.Y = 0.0f;
		TargetOffset.Z = 0.0f;

		for ( int i = 0; i < CurrentNode; ++i )
		{
			WidgetMenuNode node = Nodes[ i ];

			TargetOffset.X -= ( node.Size.X + Padding ) * Direction.X;
			TargetOffset.Y -= ( node.Size.Y + Padding ) * Direction.Y;
			TargetOffset.Z -= ( node.Size.Z + Padding ) * Direction.Z;
		}
	}

	//
	public  Vector3		Direction;
	public  float		Speed;
	public  float		Padding;

	private Vector3		CurrentOffset;
	private Vector3		TargetOffset;
	//
};

}; // namespace UI
