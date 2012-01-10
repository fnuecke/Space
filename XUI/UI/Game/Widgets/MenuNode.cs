//-----------------------------------------------
// XUI - MenuNode.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

//#define MENU_RENDER_NODES


// TODO - locked flag

namespace XUI.UI
{

// class WidgetMenuNode
public class WidgetMenuNode : WidgetBase
{
	// WidgetMenuNode
	public WidgetMenuNode()
		: this( -1 )
	{
		//
	}

	public WidgetMenuNode( int value )
		: base()
	{
		Value = value;

	#if MENU_RENDER_NODES
		ColorBase = Color.White;
		ColorBase.A( 128 );

		AddTexture( "null", 0.0f, 0.0f, 1.0f, 1.0f );
	#endif
	}

	// CopyTo
	protected override void CopyTo( WidgetBase o )
	{
		base.CopyTo( o );

		WidgetMenuNode oo = (WidgetMenuNode)o;

		oo.Value = Value;
	}

#if MENU_RENDER_NODES
	// OnRender
	protected override void OnRender()
	{
		using ( new AutoTransform( this ) )
		{

		_UI.Sprite.AddSprite( RenderPass, Layer, ref Position, Size.X, Size.Y, Align, ref ColorFinal, ref RenderState );
		_UI.Sprite.AddTexture( 0, Textures[ 0 ] );

		} // auto transform
	}
#endif

	//
	public int		Value;
	//
};

}; // namespace UI
