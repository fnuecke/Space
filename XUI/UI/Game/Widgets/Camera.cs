//-----------------------------------------------
// XUI - Camera.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using Microsoft.Xna.Framework;

// TODO - different types for 3d camera

namespace XUI.UI
{

// E_CameraType
public enum E_CameraType
{
	_2D = 0,
	_3D,

	Count,
};

// class WidgetCamera
public class WidgetCamera : WidgetBase
{
	// WidgetCamera
	public WidgetCamera()
		: this( E_CameraType._2D )
	{
		//
	}

	public WidgetCamera( E_CameraType type )
		: base()
	{
		Type = type;
	}

	// CopyTo
	protected override void CopyTo( WidgetBase o )
	{
		base.CopyTo( o );

		WidgetCamera oo = (WidgetCamera)o;

		oo.Type = Type;
	}

	// OnInit
	protected override void OnInit()
	{
		if ( Type == E_CameraType._2D )
		{
			CameraSettings2D settings = _UI.Camera2D;

			Position.X = settings.Offset.X;
			Position.Y = settings.Offset.Y;

			Scale = settings.Scale;

			Rotation.X = MathHelper.ToDegrees( settings.Rotation.X );
			Rotation.Y = MathHelper.ToDegrees( settings.Rotation.Y );
			Rotation.Z = MathHelper.ToDegrees( settings.Rotation.Z );
		}
		else
		if ( Type == E_CameraType._3D )
		{
			CameraSettings3D settings = _UI.Camera3D;

			Position = settings.Position;

			Rotation.X = MathHelper.ToDegrees( settings.Rotation.X );
			Rotation.Y = MathHelper.ToDegrees( settings.Rotation.Y );
			Rotation.Z = MathHelper.ToDegrees( settings.Rotation.Z );
		}
	}

	// OnUpdate
	protected override void OnUpdate( float frameTime )
	{
		if ( Type == E_CameraType._2D )
		{
		#if !RELEASE
			if ( CameraSettings2D.d_Camera2dDebug )
				return;
		#endif

			CameraSettings2D settings = _UI.Camera2D;

			settings.Offset.X = Position.X;
			settings.Offset.Y = Position.Y;

			settings.Scale.X = Scale.X;
			settings.Scale.Y = Scale.Y;

			settings.Rotation.X = MathHelper.ToRadians( Rotation.X );
			settings.Rotation.Y = MathHelper.ToRadians( Rotation.Y );
			settings.Rotation.Z = MathHelper.ToRadians( Rotation.Z );

			settings.UpdateTransformMatrix();
		}
		else
		if ( Type == E_CameraType._3D )
		{
		#if !RELEASE
			if ( CameraSettings3D.d_Camera3dDebug )
				return;
		#endif

			CameraSettings3D settings = _UI.Camera3D;

			settings.Position = Position;

			settings.Rotation.X = MathHelper.ToRadians( Rotation.X );
			settings.Rotation.Y = MathHelper.ToRadians( Rotation.Y );
			settings.Rotation.Z = MathHelper.ToRadians( Rotation.Z );

			settings.CalculateViewMatrix();
			settings.UpdateTransformMatrix();
		}
	}

	//
	private E_CameraType		Type;
	//
};

}; // namespace UI
