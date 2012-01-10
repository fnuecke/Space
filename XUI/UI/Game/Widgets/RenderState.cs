//-----------------------------------------------
// XUI - RenderState.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XUI.UI
{

// class WidgetRenderState
public class WidgetRenderState : WidgetBase
{
	// WidgetRenderState
	public WidgetRenderState()
		: base()
	{
		PushState = true;
	}

	// CopyTo
	protected override void CopyTo( WidgetBase o )
	{
		base.CopyTo( o );

		WidgetRenderState oo = (WidgetRenderState)o;

		oo.State_BlendName = State_BlendName;
		oo.State_Blend = State_Blend;

		oo.State_DepthStencilName = State_DepthStencilName;
		oo.State_DepthStencil = State_DepthStencil;

		oo.State_RasterizerName = State_RasterizerName;
		oo.State_Rasterizer = State_Rasterizer;

		oo.StencilRef = StencilRef;

		oo.ScissorPosition = ScissorPosition;
		oo.ScissorSize = ScissorSize;
	}

	// OnInit
	protected override void OnInit()
	{
		Parent( null ); // not allowed

		if ( State_BlendName != null )
			State_Blend = _UI.Store_BlendState.Get( State_BlendName );

		if ( State_DepthStencilName != null )
			State_DepthStencil = _UI.Store_DepthStencilState.Get( State_DepthStencilName );

		if ( State_RasterizerName != null )
			State_Rasterizer = _UI.Store_RasterizerState.Get( State_RasterizerName );
	}

	// OnRender
	protected override void OnRender()
	{
		if ( State_Blend != null )
			_UI.Sprite.AddSprite( RenderPass, Layer, State_Blend, PushState );

		if ( State_DepthStencil != null )
			_UI.Sprite.AddSprite( RenderPass, Layer, State_DepthStencil, StencilRef.HasValue ? StencilRef.Value : State_DepthStencil.ReferenceStencil, PushState );

		if ( State_Rasterizer != null )
			_UI.Sprite.AddSprite( RenderPass, Layer, State_Rasterizer, ref ScissorPosition, ref ScissorSize, PushState );
	}

	//
	public string					State_BlendName;
	public BlendState				State_Blend;

	public string					State_DepthStencilName;
	public DepthStencilState		State_DepthStencil;

	public string					State_RasterizerName;
	public RasterizerState			State_Rasterizer;

	public int?						StencilRef;

	public Vector2					ScissorPosition;
	public Vector2					ScissorSize;

	public bool						PushState;
	//
};

}; // namespace UI
