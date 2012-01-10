//-----------------------------------------------
// XUI - CameraSettings.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// TODO - different types for 3d camera

namespace XUI.UI
{

// class CameraSettings3D
public class CameraSettings3D
{
#if !RELEASE
	public  static Diagnostics.BoolD			d_Camera3dDebug			= new Diagnostics.BoolD( "UI.Camera.3dDebug", false );
	private static Diagnostics.FloatD			d_Camera3dOffsetSpeed	= new Diagnostics.FloatD( "UI.Camera.3dOffsetSpeed", 75.0f, 0.0f, 1000.0f );
	private static Diagnostics.FloatD			d_Camera3dRotateSpeed	= new Diagnostics.FloatD( "UI.Camera.3dRotateSpeed", 120.0f, 0.0f, 10.0f );
	private static Diagnostics.FloatD			d_Camera3dSpeedSlow		= new Diagnostics.FloatD( "UI.Camera.3dSpeedSlow", 0.25f, 0.0f, 10.0f );
	private static Diagnostics.FloatD			d_Camera3dSpeedFast		= new Diagnostics.FloatD( "UI.Camera.3dSpeedFast", 2.0f, 0.0f, 10.0f );
	private static Diagnostics.FunctionD		d_Camera3dReset			= new Diagnostics.FunctionD( "UI.Camera.3dReset", CameraReset );

	public static void CameraReset()	{ _UI.Camera3D.Reset(); }
#endif

	// CameraSettings3D
	public CameraSettings3D()
	{
		PresentationParameters pp = _UI.Game.GraphicsDevice.PresentationParameters;
		ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView( MathHelper.PiOver4, (float)pp.BackBufferWidth / pp.BackBufferHeight, 0.001f, 1500.0f );

		Reset();
	}

	// Reset
	public void Reset()
	{
		Position = new Vector3( 0.0f, 0.0f, 50.0f );
		Rotation = new Vector3( 0.0f );

		ViewMatrix = Matrix.CreateLookAt( Position, Vector3.Forward, Vector3.Up );

		UpdateTransformMatrix();
	}

#if !RELEASE
	// DebugUpdate
	public void DebugUpdate( float frameTime, Input input )
	{
		if ( !d_Camera3dDebug )
			return;

		if ( input.ButtonJustPressed( (int)E_UiButton.LeftStick ) )
			Reset();
		else
		{
			float amount = d_Camera3dOffsetSpeed * frameTime;

			if ( input.ButtonDown( (int)E_UiButton.LeftTrigger ) )
				amount *= d_Camera3dSpeedSlow;
			else
			if ( input.ButtonDown( (int)E_UiButton.RightTrigger ) )
				amount *= d_Camera3dSpeedFast;

			Position += ( Vector3.Right * amount * input.AxisValue( (int)E_UiAxis.LeftStickX ) );
			Position += ( Vector3.Forward * amount * input.AxisValue( (int)E_UiAxis.LeftStickY ) );
			Position -= ( Vector3.Up * amount * ( -input.ButtonValue( (int)E_UiButton.LeftShoulder ) + input.ButtonValue( (int)E_UiButton.RightShoulder ) ) );

			amount = MathHelper.ToRadians( d_Camera3dRotateSpeed ) * frameTime;

			Rotation.X += ( amount * input.AxisValue( (int)E_UiAxis.RightStickY ) );
			Rotation.Y -= ( amount * input.AxisValue( (int)E_UiAxis.RightStickX ) );

			CalculateViewMatrix();
		}

		UpdateTransformMatrix();
	}
#endif

	// CalculateViewMatrix
	public void CalculateViewMatrix()
	{
		// should do fine for a debug cam
		Matrix m = Matrix.Identity;

		m.Right = Vector3.Right;
		m.Up = Vector3.Up;
		m.Forward = Vector3.Forward;

		Matrix r = Matrix.CreateFromYawPitchRoll( Rotation.Y, Rotation.X, Rotation.Z );
		Matrix.Multiply( ref m, ref r, out m );

		ViewMatrix = Matrix.Invert( m );
		ViewMatrix.Translation = -Position;
	}

	// UpdateTransformMatrix
	public void UpdateTransformMatrix()
	{
		TransformMatrix = ViewMatrix * ProjectionMatrix;
	}

	//
	private Matrix		ViewMatrix;
	private Matrix		ProjectionMatrix;
	public  Matrix		TransformMatrix;

	public  Vector3		Position;
	public  Vector3		Rotation;
	//
};

// class CameraSettings2D
public class CameraSettings2D
{
#if !RELEASE
	public  static Diagnostics.BoolD			d_Camera2dDebug			= new Diagnostics.BoolD( "UI.Camera.2dDebug", false );
	private static Diagnostics.FloatD			d_Camera2dOffsetSpeed	= new Diagnostics.FloatD( "UI.Camera.2dOffsetSpeed", 300.0f, 0.0f, 1000.0f );
	private static Diagnostics.FloatD			d_Camera2dScaleSpeed	= new Diagnostics.FloatD( "UI.Camera.2dScaleSpeed", 2.5f, 0.0f, 10.0f );
	private static Diagnostics.FunctionD		d_Camera2dReset			= new Diagnostics.FunctionD( "UI.Camera.2dReset", CameraReset );

	public static void CameraReset()	{ _UI.Camera2D.Reset(); }
#endif

	// CameraSettings2D
	public CameraSettings2D()
	{
		Reset();
	}

	// Reset
	public void Reset()
	{
		Offset = new Vector2();
		Scale = new Vector2( 1.0f );
		Rotation = new Vector3();
		TransformMatrix = Matrix.Identity;
	}

#if !RELEASE
	// DebugUpdate
	public void DebugUpdate( float frameTime, Input input )
	{
		if ( !d_Camera2dDebug )
			return;

		if ( input.ButtonJustPressed( (int)E_UiButton.LeftStick ) )
			Reset();
		else
		{
			float amount = d_Camera2dOffsetSpeed * frameTime;

			Offset.X -= amount * input.AxisValue( (int)E_UiAxis.LeftStickX );
			Offset.Y += amount * input.AxisValue( (int)E_UiAxis.LeftStickY );

			amount = d_Camera2dScaleSpeed * frameTime;

			Scale.X -= amount * input.ButtonValue( (int)E_UiButton.LeftTrigger );
			Scale.X += amount * input.ButtonValue( (int)E_UiButton.RightTrigger );

			if ( Scale.X < 0.0f )
				Scale.X = 0.0f;

			Scale.Y = Scale.X;
		}

		UpdateTransformMatrix();
	}
#endif

	// UpdateTransformMatrix
	public void UpdateTransformMatrix()
	{
		Vector3 pivot = new Vector3( _UI.XM - Offset.X, _UI.YM - Offset.Y, 0.0f );

		TransformMatrix = Matrix.Identity;

		TransformMatrix.Translation = TransformMatrix.Translation - pivot;
		TransformMatrix = TransformMatrix * Matrix.CreateFromYawPitchRoll( Rotation.Y, Rotation.X, Rotation.Z );
		TransformMatrix = TransformMatrix * Matrix.CreateScale( Scale.X, Scale.Y, 1.0f );
		TransformMatrix.Translation = TransformMatrix.Translation + pivot;

		TransformMatrix.Translation = TransformMatrix.Translation + new Vector3( Offset, 0.0f );
	}

	//
	public Vector2		Offset;
	public Vector2		Scale;
	public Vector3		Rotation;
	public Matrix		TransformMatrix;
	//
};

}; // namespace UI
