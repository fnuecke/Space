//-----------------------------------------------
// XUI - ScreenControl.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using Microsoft.Xna.Framework;

// TODO - expand on ProcessInput if we get profile management in one day

namespace XUI.UI
{

// E_ScreenState
public enum E_ScreenState
{
	None = 0,
	Init,
	Start,
	Update,
	End,

	Count,
};

// class ScreenControl
public class ScreenControl
{
	// ScreenControl
	public ScreenControl()
	{
		State = E_ScreenState.None;
	}

	// Update
	public void Update( float frameTime )
	{
		if ( CurrentScreen == null )
			return;

		switch ( State )
		{
			// Init
			case E_ScreenState.Init:
			{
				CurrentScreen.Init();
				CurrentScreen.TimelineActive( "start", true, false );
				
				State = E_ScreenState.Start;
				Time = 0.0f;

				goto case E_ScreenState.Start; //break;
			}

			// Start
			case E_ScreenState.Start:
			{
				CurrentScreen.StartLoop( frameTime );
				
				if ( Time >= StartTimer )
				{
					State = E_ScreenState.Update;
					Time = 0.0f;
				}
				else
					Time = MathHelper.Min( Time + frameTime, StartTimer );

				break;
			}

			// Update
			case E_ScreenState.Update:
			{
				CurrentScreen.Update( frameTime );
				Time += frameTime;

				break;
			}

			// End
			case E_ScreenState.End:
			{
				CurrentScreen.EndLoop( frameTime );
				
				if ( Time >= EndTimer )
				{
					CurrentScreen.End();

					// switch to the next screen
					CurrentScreen = NextScreen;
					NextScreen = null;

					if ( CurrentScreen != null )
					{
						State = E_ScreenState.Init;
						Update( frameTime );
					}
					else
						State = E_ScreenState.None;
				}
				else
					Time = MathHelper.Min( Time + frameTime, EndTimer );

				break;
			}
		}
	}

	// ProcessInput
	public void ProcessInput()
	{
		if ( State != E_ScreenState.Update )
			return;

		if ( _UI.PrimaryPad == -1 )
		{
			for ( int i = 0; i < GameInput.NumPads; ++i )
			{
				Input input = _UI.GameInput.GetInput( i );
				CurrentScreen.ProcessInput( input );
			}
		}
		else
		{
			Input input = _UI.GameInput.GetInput( _UI.PrimaryPad );
			CurrentScreen.ProcessInput( input );
		}
	}

	// ProcessMessage
	public void ProcessMessage( ref ScreenMessage message )
	{
		if ( State != E_ScreenState.Update )
			return;

		CurrentScreen.ProcessMessage( ref message );
	}

	// Render
	public void Render()
	{
		if ( ( State == E_ScreenState.None ) || ( State == E_ScreenState.Init ) )
			return;

		CurrentScreen.Render();
	}
	
	// SetNextScreen
	public void SetNextScreen( Screen nextScreen )
	{
		if ( CurrentScreen == null )
		{
			// initial screen
			CurrentScreen = nextScreen;
			State = E_ScreenState.Init;
		}
		else
		{
			// next screen
			NextScreen = nextScreen;
			State = E_ScreenState.End;
			Time = 0.0f;

			CurrentScreen.TimelineActive( "start", false, false );
			CurrentScreen.TimelineActive( "end", true, false );
		}
	}

	public float StartTime01()
	{
		return ( ( State == E_ScreenState.Start ) ? ( Time / StartTimer ) : 1.0f );
	}

	public float EndTime01()
	{
		return ( ( State == E_ScreenState.End ) ? ( Time / EndTimer ) : 0.0f );
	}

	//
	public  E_ScreenState		State				{ get; private set; }

	public  Screen				CurrentScreen		{ get; private set; }
	private Screen				NextScreen;

	private float				Time;

	public  float				StartTimer;
	public  float				EndTimer;
	//
};

}; // namespace UI
