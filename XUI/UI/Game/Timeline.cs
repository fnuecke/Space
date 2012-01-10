//-----------------------------------------------
// XUI - Timeline.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

// TODO - callback triggers

namespace XUI.UI
{

// E_TimerType
public enum E_TimerType
{
	Stop = 0,
	Wrap,
	Bounce,

	Count,
};

// E_TimerDirection
public enum E_TimerDirection
{
	None = 0,
	Increase,
	Decrease,

	Count,
};

// E_RestType
public enum E_RestType
{
	None = 0,
	Start,
	End,

	Count,
};

// class Timeline
public class Timeline
{
	// Timeline
	public Timeline()
	{
		//
	}

	public Timeline( string name, bool startActive, float startTime, float effectTime, E_TimerType timerType, E_RestType restType )
	{
		Name = name;
		Active = startActive;

		Time = 0.0f;
		StartTime = startTime;
		EffectTime = effectTime;

		TimerType = timerType;
		TimerDirection = ( startActive ? E_TimerDirection.Increase : E_TimerDirection.None );
		RestType = restType;

		Effects = new List< TimelineEffect >();
	}

	// Copy
	public Timeline Copy()
	{
		Timeline o = new Timeline();

		o.Name = Name;
		o.Active = Active;

		o.Time = Time;
		o.StartTime = StartTime;
		o.EffectTime = EffectTime;

		o.TimerType = TimerType;
		o.TimerDirection = TimerDirection;
		o.RestType = RestType;

		o.Effects = new List< TimelineEffect >( Effects.Count );

		for ( int i = 0; i < Effects.Count; ++i )
			o.Effects.Add( Effects[ i ].Copy() );

		return o;
	}

	// AddEffect
	public void AddEffect( TimelineEffect effect )
	{
		Effects.Add( effect );
	}

	// Bind
	public void Bind( WidgetBase widget )
	{
		for ( int i = 0; i < Effects.Count; ++i )
			Effects[ i ].Bind( widget );
	}

	// SetActive
	public void SetActive( bool active )
	{
		Active = active;

		// make sure we're going the correct way
		if ( !Active )
		{
			if ( RestType == E_RestType.Start )
				TimerDirection = E_TimerDirection.Decrease;
			else
			if ( RestType == E_RestType.End )
				TimerDirection = E_TimerDirection.Increase;
		}
		else
		{
			if ( ( RestType == E_RestType.Start ) && ( TimerType != E_TimerType.Bounce ) )
				TimerDirection = E_TimerDirection.Increase;
			else
			if ( TimerDirection == E_TimerDirection.None )
				TimerDirection = E_TimerDirection.Increase;
		}
	}

	// Reset
	public void Reset( float time, bool active )
	{
		if ( time != -1.0f )
			Time = time;

		TimerDirection = E_TimerDirection.Increase; // reset effect

		SetActive( active );
	}

	// Update
	public void Update( float frameTime )
	{
		if ( !Active && ( TimerDirection != E_TimerDirection.None ) )
		{
			// check for resting
			if ( RestType == E_RestType.None )
				return;

			if ( ( RestType == E_RestType.Start ) && ( Time == 0.0f ) )
				return;

			if ( ( RestType == E_RestType.End ) && ( Math.Abs( Time - ( StartTime + EffectTime ) ) < 0.001f ) )
				return;

			// else we've already set the correct TimeDirection in Active()
		}

		// update effects
		float mult = 0.0f;

		switch ( TimerDirection )
		{
			case E_TimerDirection.Increase:		mult =  1.0f;		break;
			case E_TimerDirection.Decrease:		mult = -1.0f;		break;
		}

		Time = MathHelper.Clamp( Time + ( frameTime * mult ), 0.0f, ( StartTime + EffectTime ) );

		float time01 = 0.0f;

		if ( Time > StartTime )
			time01 = ( Time - StartTime ) / EffectTime;

		for ( int i = 0; i < Effects.Count; ++i )
			Effects[ i ].Update( time01 );

		// check for wrap/bounce
		if ( Active )
		{
			if ( Time <= StartTime )
			{
				if ( ( TimerType == E_TimerType.Bounce ) && ( TimerDirection == E_TimerDirection.Decrease ) )
				{
					Time = StartTime;
					TimerDirection = E_TimerDirection.Increase;
				}
			}
			else
			if ( Math.Abs( Time - ( StartTime + EffectTime ) ) < 0.001f )
			{
				if ( TimerType == E_TimerType.Stop )
					Active = false; // finished
				else
				if ( TimerType == E_TimerType.Wrap )
					Time = StartTime;
				else
				if ( TimerType == E_TimerType.Bounce )
					TimerDirection = E_TimerDirection.Decrease;
			}
		}
	}

	//
	public  string						Name;
	private bool						Active;

	private float						Time;
	private float						StartTime;
	private float						EffectTime;

	private E_TimerType					TimerType;
	private E_TimerDirection			TimerDirection;
	private E_RestType					RestType;

	private List< TimelineEffect >		Effects;
	//
};
	
}; // namespace UI
