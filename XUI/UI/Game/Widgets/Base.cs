//-----------------------------------------------
// XUI - Base.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

// TODO - InheritS/R/T flags
// TODO - check for parent loops in Init
// TODO - MouseLeft/Right flags
// TODO - Mouse picking for 3d passes

namespace XUI.UI
{

// E_WidgetFlag
public enum E_WidgetFlag
{
	UseMatrix			= 0x01,
	InheritAlpha		= 0x02,
	InheritIntensity	= 0x04,
	DirtyMatrix			= 0x08,
	DirtyColor			= 0x10,
#if WINDOWS
	ProcessMouse		= 0x20,
	MouseOver			= 0x40,
#endif
};

//
#if WINDOWS
public delegate void Mouse_OnEnter( WidgetBase sender );
public delegate void Mouse_OnExit( WidgetBase sender );
public delegate void Mouse_OnOver( WidgetBase sender );
#endif
//

// class WidgetBase
public class WidgetBase
{
	// WidgetBase
	public WidgetBase()
	{
		IsActive = true;
		IsSelected = true;

		Name = null;
		
		RenderPass = 0;
		Layer = 0;

		Position = new Vector3( 0.0f, 0.0f, 0.0f );
		Size = new Vector3( 0.0f, 0.0f, 0.0f );
		Scale = new Vector2( 1.0f, 1.0f );
		Rotation = new Vector3( 0.0f, 0.0f, 0.0f );

		Align = E_Align.TopLeft;

		ParentWidget = null;
		ParentAttach = E_Align.None;
		
		Children = new List< WidgetBase >();
		
		Alpha = 1.0f;
		Intensity = 1.0f;
		ColorBaseName = null;
		ColorBase = Color.Magenta;

		RenderStateName = null;
		RenderState = new RenderState( (int)E_Effect.MultiTexture1, E_BlendState.AlphaBlend );

		Textures = new List< SpriteTexture >();

		Timelines = new List< Timeline >();
		
		AlphaFinal = Alpha;
		IntensityFinal = Intensity;
		ColorFinal = ColorBase;

		Flags = (int)E_WidgetFlag.InheritAlpha | (int)E_WidgetFlag.InheritIntensity | (int)E_WidgetFlag.DirtyColor;
		TransformMatrix = Matrix.Identity;

		UpY = 1.0f;

	#if WINDOWS
		Mouse_DefaultBB = true;
	#endif
	}

	// Copy
	public WidgetBase Copy()
	{
		WidgetBase o = (WidgetBase)Activator.CreateInstance( GetType() );

		CopyTo( o );

		return o;
	}

	// CopyTo
	protected virtual void CopyTo( WidgetBase o )
	{
		o.IsActive = IsActive;
		o.IsSelected = IsSelected;

		o.Name = Name;

		o.RenderPass = RenderPass;
		o.Layer = Layer;

		o.Position = Position;
		o.Size = Size;
		o.Scale = Scale;
		o.Rotation = Rotation;

		o.Align = Align;

		// ParentWidget - doesn't copy over
		o.ParentAttach = ParentAttach;

		// Children - don't copy over

		o.Alpha = Alpha;
		o.Intensity = Intensity;
		o.ColorBaseName = ColorBaseName;
		o.ColorBase = ColorBase;

		o.RenderStateName = RenderStateName;
		o.RenderState = RenderState;

		o.Textures.Capacity = Textures.Count;

		for ( int i = 0; i < Textures.Count; ++i )
			o.Textures.Add( Textures[ i ] );

		o.Timelines.Capacity = Timelines.Count;

		for ( int i = 0; i < Timelines.Count; ++i )
			o.Timelines.Add( Timelines[ i ].Copy() );

		o.AlphaFinal = AlphaFinal;
		o.IntensityFinal = IntensityFinal;
		o.ColorFinal = ColorFinal;

		o.Flags = Flags;
		o.TransformMatrix = TransformMatrix;

		o.UpY = UpY;

	#if WINDOWS
		o.Mouse_DefaultBB = Mouse_DefaultBB;
		o.Mouse_PosBB = Mouse_PosBB;
		o.Mouse_SizeBB = Mouse_SizeBB;

		// Mouse_OnEnter - doesn't copy over
		// Mouse_OnExit  - doesn't copy over
		// Mouse_OnOver  - doesn't copy over
	#endif
	}

	// CopyAndAdd
	public WidgetBase CopyAndAdd( Screen screen )
	{
		return CopyAndAdd_Aux( screen, this );
	}

	private WidgetBase CopyAndAdd_Aux( Screen screen, WidgetBase widget )
	{
		WidgetBase o = widget.Copy();
		screen.Add( o );

		for ( int i = 0; i < widget.Children.Count; ++i )
		{
			WidgetBase oo = CopyAndAdd_Aux( screen, widget.Children[ i ] );
			oo.Parent( o );
		}

		return o;
	}

	// FindChild
	public WidgetBase FindChild( string name )
	{
		return FindChild_Aux( name, this );
	}

	private WidgetBase FindChild_Aux( string name, WidgetBase widget )
	{
		for ( int i = 0; i < widget.Children.Count; ++i )
		{
			WidgetBase o = widget.Children[ i ];

			if ( ( o.Name != null ) && ( o.Name.Equals( name ) ) )
				return o;

			WidgetBase oo = FindChild_Aux( name, o );

			if ( oo != null )
				return oo;
		}

		return null;
	}

	// AddTexture
	public void AddTexture( string name, ref Vector2 puv, ref Vector2 suv )
	{
		Textures.Add( new SpriteTexture( name, ref puv, ref suv ) );
	}

	public void AddTexture( string name, float pu, float pv, float su, float sv )
	{
		Textures.Add( new SpriteTexture( name, pu, pv, su, sv ) );
	}

	public void AddTexture( string name )
	{
		Textures.Add( _UI.Store_Texture.Get( name ) );
	}

	// ChangeTexture
	public void ChangeTexture( int slot, string name, ref Vector2 puv, ref Vector2 suv )
	{
		Textures[ slot ] = new SpriteTexture( name, ref puv, ref suv );
	}

	public void ChangeTexture( int slot, string name, float pu, float pv, float su, float sv )
	{
		Textures[ slot ] = new SpriteTexture( name, pu, pv, su, sv );
	}

	public void ChangeTexture( int slot, string name )
	{
		Textures[ slot ] = _UI.Store_Texture.Get( name );
	}

	public void ChangeTexture( int slot, ref SpriteTexture texture )
	{
		Textures[ slot ] = texture;
	}

	// GetTexture
	public SpriteTexture GetTexture( int slot )
	{
		return Textures[ slot ];
	}

	// ChangeColor
	public void ChangeColor( SpriteColors colors )
	{
		ColorBase = colors;
		FlagSet( E_WidgetFlag.DirtyColor );
	}

	public void ChangeColor( ref SpriteColors colors )
	{
		ColorBase = colors;
		FlagSet( E_WidgetFlag.DirtyColor );
	}

	public void ChangeColor( string colorName )
	{
		ColorBase = _UI.Store_Color.Get( colorName );
		FlagSet( E_WidgetFlag.DirtyColor );
	}

	// Init
	public void Init()
	{
		if ( ColorBaseName != null )
			ColorBase = _UI.Store_Color.Get( ColorBaseName );

		if ( RenderStateName != null )
			RenderState = _UI.Store_RenderState.Get( RenderStateName );

		for ( int i = 0; i < Timelines.Count; ++i )
			Timelines[ i ].Bind( this );

		UpY = _UI.Sprite.IsRenderPass3D( RenderPass ) ? -1.0f : 1.0f;

	#if WINDOWS
		if ( Mouse_DefaultBB )
		{
			Mouse_SizeBB.X = Size.X;
			Mouse_SizeBB.Y = Size.Y;
		}

		Vector2 offset = _UI.Sprite.GetVertexOffsetAligned( RenderPass, Align );

		Mouse_PosBB.X -= ( Mouse_SizeBB.X * offset.X );
		Mouse_PosBB.Y -= ( Mouse_SizeBB.Y * offset.Y );
	#endif

		OnInit();
	}

	// OnInit
	protected virtual void OnInit()
	{
		//
	}

	// PostInit
	public void PostInit()
	{
		OnPostInit();
	}

	// OnPostInit
	protected virtual void OnPostInit()
	{
		//
	}

	// ProcessInput
	public void ProcessInput( Input input )
	{
		if ( !IsActive || !IsSelected )
			return;

		OnProcessInput( input );
	}

	// OnProcessInput
	protected virtual void OnProcessInput( Input input )
	{
		//
	}

	// Update
	public void Update( float frameTime )
	{
		FlagSet( E_WidgetFlag.DirtyMatrix );

		if ( !IsActive )
			return;

		UpdateTimelines( frameTime );

		OnUpdate( frameTime );
	}

	// OnUpdate
	protected virtual void OnUpdate( float frameTime )
	{
		//
	}

	// Render
	public void Render()
	{
		if ( !IsActive )
			return;

		CalculateTransform();
		CalculateColor();

	#if WINDOWS
		if ( IsFlagSet( E_WidgetFlag.ProcessMouse ) && ( _UI.Screen.GetState() == E_ScreenState.Update ) )
			ProcessMouse();
	#endif

		OnRender();
	}

	// OnRender
	protected virtual void OnRender()
	{
		//
	}

	// CalculateTransform
	private void CalculateTransform()
	{
		if ( !IsFlagSet( E_WidgetFlag.DirtyMatrix ) )
			return;

		TransformMatrix = Matrix.Identity;

		if ( ( ParentWidget == null ) && ( Scale.X == 1.0f ) && ( Scale.Y == 1.0f ) && ( Rotation.X == 0.0f ) && ( Rotation.Y == 0.0f ) && ( Rotation.Z == 0.0f ) )
		{
			FlagClear( E_WidgetFlag.UseMatrix );
			TransformMatrix.Translation = Position;
		}
		else
		{
			FlagSet( E_WidgetFlag.UseMatrix );

			Matrix s = Matrix.CreateScale( Scale.X, Scale.Y, 1.0f );
			Matrix r = Matrix.CreateFromYawPitchRoll( Rotation.Y * 0.01745329f, Rotation.X * 0.01745329f, Rotation.Z * 0.01745329f );

			Matrix.Multiply( ref TransformMatrix, ref s, out TransformMatrix );
			Matrix.Multiply( ref TransformMatrix, ref r, out TransformMatrix );

			if ( ParentWidget != null )
			{
				ParentWidget.CalculateTransform();

				TransformMatrix.Translation = Position + new Vector3( new Vector2( ParentWidget.Size.X, ParentWidget.Size.Y ) * ( _UI.Sprite.GetVertexOffsetAligned( RenderPass, ParentAttach ) 
					- _UI.Sprite.GetVertexOffsetAligned( ParentWidget.RenderPass, ParentWidget.Align ) ), 0.0f );

				Matrix.Multiply( ref TransformMatrix, ref ParentWidget.TransformMatrix, out TransformMatrix );
			}
			else
				TransformMatrix.Translation = Position;
		}

		FlagClear( E_WidgetFlag.DirtyMatrix );
	}

	// CalculateColor
	private void CalculateColor()
	{
		float oldAlphaFinal = AlphaFinal;
		float oldIntensityFinal = IntensityFinal;

		AlphaFinal = Alpha;
		IntensityFinal = Intensity;

		bool doAlpha = IsFlagSet( E_WidgetFlag.InheritAlpha );
		bool doIntensity = IsFlagSet( E_WidgetFlag.InheritIntensity );
		
		for ( WidgetBase p = ParentWidget; p != null; p = p.ParentWidget )
		{
			if ( doAlpha )
				AlphaFinal *= p.Alpha;

			if ( doIntensity )
			{
				if ( p.Intensity > 1.0f )
					IntensityFinal += ( p.Intensity - 1.0f );
				else
					IntensityFinal *= p.Intensity;
			}

			doAlpha &= p.IsFlagSet( E_WidgetFlag.InheritAlpha );
			doIntensity &= p.IsFlagSet( E_WidgetFlag.InheritIntensity );

			if ( !doAlpha && !doIntensity )
				break;
		}

		if ( ( AlphaFinal != oldAlphaFinal ) || ( IntensityFinal != oldIntensityFinal ) || IsFlagSet( E_WidgetFlag.DirtyColor ) )
		{
			ColorFinal.Set( ref ColorBase, AlphaFinal, IntensityFinal );

			if ( RenderState.BlendState == E_BlendState.AlphaBlend )
				ColorFinal.ToPremultiplied();

			FlagClear( E_WidgetFlag.DirtyColor );
		}
	}

#if WINDOWS
	// ProcessMouse
	private void ProcessMouse()
	{
		if ( _UI.Sprite.IsRenderPass3D( RenderPass ) )
			return;

		Vector3 mouseP = new Vector3( _UI.GameInput.Mouse.XY(), 0.0f );

		Matrix matrixI;
		Matrix.Invert( ref _UI.Sprite.RenderPassTransformMatrix[ RenderPass ], out matrixI );
		Vector3.Transform( ref mouseP, ref matrixI, out mouseP );

		Matrix matrixII;
		Matrix.Invert( ref TransformMatrix, out matrixII );
		Vector3.Transform( ref mouseP, ref matrixII, out mouseP );

		Vector2 offsetP = _UI.Sprite.GetVertexOffsetAligned( RenderPass, Align );

		bool isOver = ( ( mouseP.X >= Mouse_PosBB.X ) && ( mouseP.X <= ( Mouse_PosBB.X + Mouse_SizeBB.X ) ) && ( mouseP.Y >= Mouse_PosBB.Y ) && ( mouseP.Y <= ( Mouse_PosBB.Y + Mouse_SizeBB.Y ) ) );
		bool wasOver = IsFlagSet( E_WidgetFlag.MouseOver );

		if ( isOver )
		{
			if ( !wasOver )
			{
				FlagSet( E_WidgetFlag.MouseOver );

				OnMouseEnter();

				if ( Mouse_OnEnter != null )
					Mouse_OnEnter( this );
			}

			OnMouseOver();

			if ( Mouse_OnOver != null )
				Mouse_OnOver( this );
		}
		else
		{
			if ( wasOver )
			{
				FlagClear( E_WidgetFlag.MouseOver );

				OnMouseExit();

				if ( Mouse_OnExit != null )
					Mouse_OnExit( this );
			}
		}
	}

	// OnMouseEnter
	protected virtual void OnMouseEnter()
	{
		//
	}

	// OnMouseExit
	protected virtual void OnMouseExit()
	{
		//
	}

	// OnMouseOver
	protected virtual void OnMouseOver()
	{
		//
	}
#endif

	// AddTimeline
	public void AddTimeline( Timeline timeline )
	{
		Timelines.Add( timeline );
	}

	public void AddTimeline( string name )
	{
		Timeline timeline = _UI.Store_Timeline.Get( name );

		if ( timeline != null )
			Timelines.Add( timeline );
	}

	// UpdateTimelines
	private void UpdateTimelines( float frameTime )
	{
		for ( int i = 0; i < Timelines.Count; ++i )
			Timelines[ i ].Update( frameTime );
	}

	// TimelineActive
	public void TimelineActive( string name, bool value, bool children )
	{
		for ( int i = 0; i < Timelines.Count; ++i )
		{
			Timeline timeline = Timelines[ i ];

			if ( timeline.Name.Equals( name ) )
				timeline.SetActive( value );
		}

		if ( children )
			for ( int i = 0; i < Children.Count; ++i )
				Children[ i ].TimelineActive( name, value, children );
	}

	// TimelineReset
	public void TimelineReset( string name, bool value, float time, bool children )
	{
		for ( int i = 0; i < Timelines.Count; ++i )
		{
			Timeline timeline = Timelines[ i ];

			if ( timeline.Name.Equals( name ) )
				timeline.Reset( time, value );
		}

		if ( children )
			for ( int i = 0; i < Children.Count; ++i )
				Children[ i ].TimelineReset( name, value, time, children );
	}

	// Active
	public void Active( bool value, bool children )
	{
		IsActive = value;

		OnActive( value );

		if ( children )
			for ( int i = 0; i < Children.Count; ++i )
				Children[ i ].Active( value, children );
	}

	public void Active( bool value )
	{
		Active( value, false );
	}

	public bool Active()
	{
		return IsActive;
	}

	// OnActive
	protected virtual void OnActive( bool value )
	{
		//
	}

	// Selected
	public void Selected( bool value, bool children, bool timelineMessage )
	{
		IsSelected = value;

		if ( timelineMessage )
			TimelineActive( "selected", value, false );

		OnSelected( value );

		if ( children )
			for ( int i = 0; i < Children.Count; ++i )
				Children[ i ].Selected( value, children, timelineMessage );
	}

	public void Selected( bool value )
	{
		Selected( value, false, false );
	}

	public bool Selected()
	{
		return IsSelected;
	}

	// OnSelected
	protected virtual void OnSelected( bool value )
	{
		//
	}

	// Parent
	public void Parent( WidgetBase widget )
	{
		if ( ParentWidget != null )
			ParentWidget.Children.Remove( this );
		
		ParentWidget = widget;
		
		if ( ParentWidget != null )
			ParentWidget.Children.Add( this );
	}

	// FlagSet
	public void FlagSet( E_WidgetFlag flag )
	{
		Flags |= (int)flag;
	}

	// FlagClear
	public void FlagClear( E_WidgetFlag flag )
	{
		Flags &= ~(int)flag;
	}

	// IsFlagSet
	public bool IsFlagSet( E_WidgetFlag flag )
	{
		return ( ( Flags & (int)flag ) != 0 );
	}

	//
	protected bool						IsActive;
	protected bool						IsSelected;

	public string						Name;

	public int							RenderPass;
	public int							Layer;

	public Vector3						Position;
	public Vector3						Size;
	public Vector2						Scale;
	public Vector3						Rotation;

	public E_Align						Align;

	protected WidgetBase				ParentWidget;
	public    E_Align					ParentAttach;

	protected List< WidgetBase >		Children;
	
	public float						Alpha;
	public float						Intensity;
	public string						ColorBaseName;
	public SpriteColors					ColorBase;

	public string						RenderStateName;
	public RenderState					RenderState;

	protected List< SpriteTexture >		Textures;

	protected List< Timeline >			Timelines;

	protected float						AlphaFinal;
	protected float						IntensityFinal;
	protected SpriteColors				ColorFinal;

	private int							Flags;
	public  Matrix						TransformMatrix;

	public  float						UpY;

#if WINDOWS
	public bool							Mouse_DefaultBB;
	public Vector2						Mouse_PosBB;
	public Vector2						Mouse_SizeBB;

	public event Mouse_OnEnter			Mouse_OnEnter;
	public event Mouse_OnExit			Mouse_OnExit;
	public event Mouse_OnOver			Mouse_OnOver;
#endif
	//
};

}; // namespace UI
