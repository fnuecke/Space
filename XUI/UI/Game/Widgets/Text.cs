//-----------------------------------------------
// XUI - Text.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace XUI.UI
{

// class WidgetText
public class WidgetText : WidgetBase
{
	// WidgetText
	public WidgetText()
		: base()
	{
		FontEffects = new List< FontEffect >();
	}

	// CopyTo
	protected override void CopyTo( WidgetBase o )
	{
		base.CopyTo( o );

		WidgetText oo = (WidgetText)o;

		oo._String = _String;

		if ( StringUI != null )
			oo.StringUI = new StringUI( StringUI );

		oo.FontStyleName = FontStyleName;

		if ( FontStyle != null )
			oo.FontStyle = FontStyle.Copy();

		oo.FontEffects.Capacity = FontEffects.Count;

		for ( int i = 0; i < FontEffects.Count; ++i )
			oo.FontEffects.Add( FontEffects[ i ].Copy() );

		oo.RenderSizeY = RenderSizeY;
		oo.RenderSizeX = RenderSizeX;

		oo.Cache = Cache;

		if ( SpriteListCache != null )
		{
			oo.SpriteListCache = new FontSpriteList();
			SpriteListCache.CopyTo( oo.SpriteListCache );
		}

		oo.CacheValid = CacheValid;

		oo.FrameTime = FrameTime;
	}

	// OnInit
	protected override void OnInit()
	{
		RenderState.BlendState = E_BlendState.NonPremultiplied; // handled in the FontStyle

		RenderSizeX = Size.X;
		RenderSizeY = Size.Y;

		if ( FontStyleName != null )
			FontStyle = _UI.Store_FontStyle.Get( FontStyleName );

		for ( int i = 0; i < FontEffects.Count; ++i )
			FontEffects[ i ].Bind( this );

		if ( Cache && ( SpriteListCache == null ) )
			SpriteListCache = new FontSpriteList();
	}

	// AddFontEffect
	public void AddFontEffect( FontEffect fontEffect )
	{
		if ( fontEffect == null )
			return;

		FontEffects.Add( fontEffect );
	}

	public void AddFontEffect( string fontEffectName )
	{
		FontEffect fontEffect = _UI.Store_FontEffect.Get( fontEffectName );

		if ( fontEffect != null )
			AddFontEffect( fontEffect.Copy() );
	}

	// OnUpdate
	protected override void OnUpdate( float frameTime )
	{
		FrameTime = frameTime;
	}

	// OnRender
	protected override void OnRender()
	{
		if ( ( Size.Y <= 0.0f ) || ( !ColorFinal.HasAlpha() && ( ( RenderState.BlendState == E_BlendState.AlphaBlend ) || ( RenderState.BlendState == E_BlendState.NonPremultiplied ) ) ) )
			return;

		if ( FontStyle == null )
			return;

		if ( ( _String == null ) && ( StringUI == null ) )
			return;

		using ( new AutoTransform( this ) )
		{

		if ( FontEffects.Count > 0 )
			_UI.Font.DoFontEffectsNext( FontEffects );

		if ( Cache && CacheValid )
			FontStyle.Render( SpriteListCache, RenderSizeY );
		else
		{
			Vector2 stringSize = new Vector2();

			if ( StringUI != null )
				stringSize = _UI.Font.Draw( StringUI, FontStyle, RenderPass, Layer, ref Position, RenderSizeY, Align, ref ColorFinal, RenderSizeX );
			else
				stringSize = _UI.Font.Draw( _String, FontStyle, RenderPass, Layer, ref Position, RenderSizeY, Align, ref ColorFinal, RenderSizeX );

			// slag lag - are we bothered? We'll see ...
			Size.X = stringSize.X;
			Size.Y = stringSize.Y;

			if ( Cache )
			{
				CacheValid = true;
				_UI.Font.SpriteList.CopyTo( SpriteListCache );
			}
		}

		for ( int i = 0; i < FontEffects.Count; ++i )
			FontEffects[ i ].Update( FrameTime ); // meh - but need to update here to make sure the letter count is correct

		} // auto transform
	}

	// OnSelected
	protected override void OnSelected( bool value )
	{
		if ( value )
			for ( int i = 0; i < FontEffects.Count; ++i )
				FontEffects[ i ].ResetWait();
	}

	// OnChangeString
	public void OnChangeString()
	{
		if ( Cache )
			CacheValid = false;
	}

	//
	public  string					String			{ get { return _String; } set { _String = value; OnChangeString(); } }
	private string					_String;

	public  StringUI				StringUI;		// need to call OnChangeString() manually with this ... humph
	
	public  string					FontStyleName;
	public  FontStyle				FontStyle;

	private List< FontEffect >		FontEffects;

	public  bool					Cache;
	private FontSpriteList			SpriteListCache;
	private bool					CacheValid;

	public  float					RenderSizeY		{ get; private set; }
	private float					RenderSizeX;

	private float					FrameTime;
	//
};

}; // namespace UI
