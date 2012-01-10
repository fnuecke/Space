//-----------------------------------------------
// XUI - Graphic.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

namespace XUI.UI
{

// class WidgetGraphic
public class WidgetGraphic : WidgetBase
{
	// WidgetGraphic
	public WidgetGraphic()
		: base()
	{
		//
	}

	// OnRender
	protected override void OnRender()
	{
		if ( Textures.Count == 0 )
			return;

		if ( !ColorFinal.HasAlpha() && ( ( RenderState.BlendState == E_BlendState.AlphaBlend ) || ( RenderState.BlendState == E_BlendState.NonPremultiplied ) ) )
			return;

		using ( new AutoTransform( this ) )
		{

		_UI.Sprite.AddSprite( RenderPass, Layer, ref Position, Size.X, Size.Y, Align, ref ColorFinal, ref RenderState );

		for ( int i = 0; i < Textures.Count; ++i )
		{
			if ( i == SpriteTexture.TextureCount )
				break;

			_UI.Sprite.AddTexture( i, Textures[ i ] );
		}

		} // auto transform
	}
};

}; // namespace UI
