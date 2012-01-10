//-----------------------------------------------
// XUI - Slider.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using Microsoft.Xna.Framework;

namespace XUI.UI
{

// class WidgetSlider
public class WidgetSlider : WidgetBase
{
	// WidgetSlider
	public WidgetSlider()
		: base()
	{
		MaxValue = 1;
		Step = 1;
	}

	// CopyTo
	protected override void CopyTo( WidgetBase o )
	{
		base.CopyTo( o );

		WidgetSlider oo = (WidgetSlider)o;

		oo.IndicatorSize = IndicatorSize;

		oo.Value = Value;
		oo.MinValue = MinValue;
		oo.MaxValue = MaxValue;
		oo.Step = Step;

		// TextWidget - doesn't copy over
	}

	// OnInit
	protected override void OnInit()
	{
		if ( TextWidget != null )
			TextWidget.StringUI = new StringUI( 16 );
	}

	// OnProcessInput
	protected override void OnProcessInput( Input input )
	{
		float delay = _UI.AutoRepeatDelay;
		float repeat = _UI.AutoRepeatRepeat;

		if ( input.ButtonAutoRepeat( (int)E_UiButton.Left, delay, repeat ) )
		{
			Value -= Step;

			if ( Value < MinValue )
				Value = MinValue;
		}
		else
		if ( input.ButtonAutoRepeat( (int)E_UiButton.Right, delay, repeat ) )
		{
			Value += Step;

			if ( Value > MaxValue )
				Value = MaxValue;
		}
	}

	// OnUpdate
	protected override void OnUpdate( float frameTime )
	{
		if ( TextWidget != null )
		{
			StringUI s = TextWidget.StringUI;

			s.Clear();
			s.Add( Value );
		}
	}

	// OnRender
	protected override void OnRender()
	{
		if ( Textures.Count < 2 )
			return;

		if ( !ColorFinal.HasAlpha() && ( ( RenderState.BlendState == E_BlendState.AlphaBlend ) || ( RenderState.BlendState == E_BlendState.NonPremultiplied ) ) )
			return;

		using ( new AutoTransform( this ) )
		{

		// align
		Vector2 offset = _UI.Sprite.GetVertexOffsetAligned( 0, Align );
		Vector3 pos = Position;
		pos.X -= offset.X * Size.X;
		pos.Y -= offset.Y * Size.Y * UpY;

		_UI.Sprite.AddSprite( RenderPass, Layer, ref pos, Size.X, Size.Y, E_Align.TopLeft, ref ColorFinal, ref RenderState );
		_UI.Sprite.AddTexture( 0, Textures[ 0 ] );

		float pos01 = (float)( Value - MinValue ) / ( MaxValue - MinValue );

		_UI.Sprite.AddSprite( RenderPass, Layer, pos.X + ( Size.X * pos01 ), pos.Y + ( Size.Y / 2.0f ) * UpY, pos.Z, IndicatorSize.X, IndicatorSize.Y, E_Align.MiddleCentre, ref ColorFinal, ref RenderState );
		_UI.Sprite.AddTexture( 0, Textures[ 1 ] );

		} // auto transform
	}

	//
	public Vector2			IndicatorSize;

	public int				Value;
	public int				MinValue;
	public int				MaxValue;
	public int				Step;

	public WidgetText		TextWidget;
	//
};

}; // namespace UI
