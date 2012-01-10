//-----------------------------------------------
// XUI - Screen.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System.Collections.Generic;

namespace XUI.UI
{

// class Screen
public abstract class Screen
{
	// Screen
	public Screen()
		: this( "<no name>" )
	{
		//
	}

	public Screen( string name )
	{
		Name = name;
		AllowInput = true;
		Widgets = new List< WidgetBase >();
	}

	// Init
	public void Init()
	{
		SetScreenTimers( 0.0f, 0.0f ); // default

		for ( int i = 0; i < Widgets.Count; ++i )
			Widgets[ i ].Init();

		OnInit();

		for ( int i = 0; i < Widgets.Count; ++i )
			Widgets[ i ].PostInit();

		OnPostInit();
	}

	// OnInit
	protected virtual void OnInit()
	{
		//
	}

	// OnPostInit
	protected virtual void OnPostInit()
	{
		//
	}

	// StartLoop
	public void StartLoop( float frameTime )
	{
		for ( int i = 0; i < Widgets.Count; ++i )
			Widgets[ i ].Update( frameTime );

		OnStartLoop( frameTime );
	}

	// OnStartLoop
	protected virtual void OnStartLoop( float frameTime )
	{
		//
	}

	// ProcessInput
	public void ProcessInput( Input input )
	{
		for ( int i = 0; i < Widgets.Count; ++i )
			Widgets[ i ].ProcessInput( input );

		OnProcessInput( input );
	}

	// OnProcessInput
	protected virtual void OnProcessInput( Input input )
	{
		//
	}

	// ProcessMessage
	public void ProcessMessage( ref ScreenMessage message )
	{
		OnProcessMessage( ref message );
	}

	// OnProcessMessage
	protected virtual void OnProcessMessage( ref ScreenMessage message )
	{
		//
	}

	// Update
	public void Update( float frameTime )
	{
		for ( int i = 0; i < Widgets.Count; ++i )
			Widgets[ i ].Update( frameTime );

		OnUpdate( frameTime );
	}

	// OnUpdate
	protected virtual void OnUpdate( float frameTime )
	{
		//
	}

	// EndLoop
	public void EndLoop( float frameTime )
	{
		for ( int i = 0; i < Widgets.Count; ++i )
			Widgets[ i ].Update( frameTime );

		OnEndLoop( frameTime );
	}

	// OnEndLoop
	protected virtual void OnEndLoop( float frameTime )
	{
		//
	}

	// End
	public void End()
	{
		OnEnd();
	}

	// OnEnd
	protected virtual void OnEnd()
	{
		//
	}

	// Render
	public void Render()
	{
		for ( int i = 0; i < Widgets.Count; ++i )
			Widgets[ i ].Render();

		OnRender();
	}

	// OnRender
	protected virtual void OnRender()
	{
		//
	}

	// SetScreenTimers
	protected void SetScreenTimers( float startTime, float endTime )
	{
		_UI.Screen.SetScreenTimers( startTime, endTime );
	}

	// Add
	public void Add( WidgetBase widget )
	{
		Widgets.Add( widget );
	}

	// TimelineActive
	public void TimelineActive( string name, bool value, bool children )
	{
		for ( int i = 0; i < Widgets.Count; ++i )
			Widgets[ i ].TimelineActive( name, value, children );
	}

	// TimelineReset
	public void TimelineReset( string name, bool value, float time, bool children )
	{
		for ( int i = 0; i < Widgets.Count; ++i )
			Widgets[ i ].TimelineReset( name, value, time, children );
	}

	//
	public  string					Name;
	public  bool					AllowInput;
	private List< WidgetBase >		Widgets;
	//
};

}; // namespace UI
