//-----------------------------------------------
// XUI - ScreenManager.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System.Collections.Generic;

// TODO - expand on message system

namespace XUI.UI
{

// struct ScreenMessage
public struct ScreenMessage
{
	// ScreenMessage
	public ScreenMessage( int type, int data )
	{
		Type = type;
		Data = data;
	}

	//
	public int		Type;
	public int		Data;
	//
};

// class ScreenManager
public class ScreenManager
{
#if !RELEASE
	static Debug.BoolD		d_ShowScreens	= new Debug.BoolD( "UI.ShowScreens", false );
	static Debug.BoolD		d_ShowSafeArea	= new Debug.BoolD( "UI.ShowSafeArea", false );
	static Debug.FloatD		d_TimeScale		= new Debug.FloatD( "UI.TimeScale", 1.0f, 0.0f, 5.0f, 0.05f );
#endif

	// ScreenManager
	public ScreenManager()
	{
		ScreenControls = new List< ScreenControl >();
		CurrentScreenControl = -1;

		MessagePool = new ScreenMessage[ _UI.Settings.Screen_MessageCount ];
		MessageCount = 0;
	}
	
	// Update
	public void Update( float frameTime )
	{
	#if !RELEASE
		frameTime *= d_TimeScale;
	#endif

		int indexInput = -1;

		// determine which screens gets input
		for ( int i = ( ScreenControls.Count - 1 ); i >= 0; --i )
		{
			if ( ScreenControls[ i ].CurrentScreen.AllowInput )
			{
				indexInput = i;
				break;
			}
		}

		// process each screen
		// - input and update all first
		for ( int i = 0; i < ScreenControls.Count; ++i )
		{
			CurrentScreenControl = i;

			ScreenControl screenControl = ScreenControls[ i ];

			if ( i == indexInput )
				screenControl.ProcessInput();

			screenControl.Update( frameTime );
		}

		// - messages and render second
		for ( int i = 0; i < ScreenControls.Count; ++i )
		{
			CurrentScreenControl = i;

			ScreenControl screenControl = ScreenControls[ i ];

			for ( int j = 0; j < MessageCount; ++j )
				screenControl.ProcessMessage( ref MessagePool[ j ] );

			screenControl.Render();
		}

		// remove any dead screens
		for ( int i = ( ScreenControls.Count - 1 ); i >= 0; --i )
			if ( ScreenControls[ i ].State == E_ScreenState.None )
				ScreenControls.RemoveAt( i );
		
		CurrentScreenControl = -1;
		MessageCount = 0;

	#if !RELEASE
		if ( d_ShowSafeArea )
		{
			SpriteColors c = 0x40c0c000;
			RenderState renderState = new RenderState( (int)E_Effect.MultiTexture1, E_BlendState.AlphaBlend );
			_UI.Sprite.AddSprite( 0, _UI.Sprite.TopLayer, _UI.SXL, _UI.SYT, 0.0f, _UI.SSX, _UI.SSY, E_Align.TopLeft, ref c, ref renderState );
			_UI.Sprite.AddTexture( 0, _UI.Texture.Get( "null" ) );
		}

		if ( d_ShowScreens )
		{
			SpriteColors c = Microsoft.Xna.Framework.Color.DarkOrange;

			for ( int i = 0; i < ScreenControls.Count; ++i )
			{
				var p = new Microsoft.Xna.Framework.Vector3( _UI.SXM, _UI.SYB - ( 20.0f * i ), 0.0f );

				StringUI.Clear();
				StringUI.Add( i );
				StringUI.Add( "  -  " );
				StringUI.Add( ScreenControls[ i ].CurrentScreen.Name );

				_UI.Font.Draw( StringUI, _UI.Store_FontStyle.Get( "Default" ), 0, _UI.Sprite.TopLayer, ref p, 20.0f, E_Align.BottomCentre, ref c, 0.0f );
			}
		}
	#endif
	}

	// AddMessage
	public void AddMessage( int type, int data )
	{
		if ( MessageCount == _UI.Settings.Screen_MessageCount )
			return;

		MessagePool[ MessageCount++ ] = new ScreenMessage( type, data );
	}

	// SetScreenTimers
	public void SetScreenTimers( float startTime, float endTime )
	{
		ScreenControl currentScreenControl = ScreenControls[ CurrentScreenControl ];

		currentScreenControl.StartTimer = startTime;
		currentScreenControl.EndTimer = endTime;
	}

	// GetState
	public E_ScreenState GetState()
	{
		return ScreenControls[ CurrentScreenControl ].State;
	}

	// SetNextScreen
	public void SetNextScreen( Screen screen )
	{
		ScreenControls[ CurrentScreenControl ].SetNextScreen( screen );
	}

	// AddScreen
	public void AddScreen( Screen screen )
	{
		ScreenControl screenControl = new ScreenControl();
		screenControl.SetNextScreen( screen );

		ScreenControls.Add( screenControl );
	}

	//
	private List< ScreenControl >		ScreenControls;
	private int							CurrentScreenControl;

	private ScreenMessage[]				MessagePool;
	private int							MessageCount;

#if !RELEASE
	private StringUI					StringUI = new StringUI( 32 );
#endif
	//
};

}; // namespace UI
