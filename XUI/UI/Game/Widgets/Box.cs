//-----------------------------------------------
// XUI - Box.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using Microsoft.Xna.Framework;

namespace XUI.UI
{

// class WidgetBox
public class WidgetBox : WidgetBase
{
	// WidgetBox
	public WidgetBox()
		: base()
	{
		//
	}

	// CopyTo
	protected override void CopyTo( WidgetBase o )
	{
		base.CopyTo( o );

		WidgetBox oo = (WidgetBox)o;

		oo.CornerSize = CornerSize;
		oo.CornerPuv01 = CornerPuv01;
		oo.CornerSuv01 = CornerSuv01;
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

		// align
		Vector2 offset = _UI.Sprite.GetVertexOffsetAligned( 0, Align );
		Vector3 pos = Position;
		pos.X -= offset.X * Size.X;
		pos.Y -= offset.Y * Size.Y * UpY;

		// vertex positions
		float x0 = pos.X;
		float x1 = x0 + CornerSize.X;
		float x2 = x1 + Size.X - ( CornerSize.X * 2.0f );
		float x3 = x2 + CornerSize.X;

		float y0 = pos.Y;
		float y1 = pos.Y + ( CornerSize.Y * UpY );
		float y2 = y1 + ( Size.Y - ( CornerSize.Y * 2.0f ) ) * UpY;
		float y3 = y2 + ( CornerSize.Y * UpY );
		
		// sizes
		float sx1x0 = x1 - x0;
		float sx2x1 = x2 - x1;
		float sx3x2 = x3 - x2;

		float sy1y0 = ( y1 - y0 ) * UpY;
		float sy2y1 = ( y2 - y1 ) * UpY;
		float sy3y2 = ( y3 - y2 ) * UpY;

		// uvs
		SpriteTexture tex = Textures[ 0 ];
		
		float u0 = tex.PUV.X + ( CornerPuv01.X / tex.SUV.X );
		float u1 = u0 + ( CornerSuv01.X / tex.SUV.X );
		float u3 = tex.PUV.X + tex.SUV.X - ( CornerPuv01.X / tex.SUV.X );
		float u2 = u3 - ( CornerSuv01.X / tex.SUV.X );

		float v0 = tex.PUV.Y + ( CornerPuv01.Y / tex.SUV.Y );
		float v1 = u0 + ( CornerSuv01.Y / tex.SUV.Y );
		float v3 = tex.PUV.Y + tex.SUV.Y - ( CornerPuv01.Y / tex.SUV.Y );
		float v2 = u3 - ( CornerSuv01.Y / tex.SUV.Y );

		// uv sizes
		float su1u0 = u1 - u0;
		float su2u1 = u2 - u1;
		float su3u2 = u3 - u2;

		float sv1v0 = v1 - v0;
		float sv2v1 = v2 - v1;
		float sv3v2 = v3 - v2;

		// TL
		_UI.Sprite.AddSprite( RenderPass, Layer, x0, y0, pos.Z, sx1x0, sy1y0, E_Align.TopLeft, ref ColorFinal, ref RenderState );
		_UI.Sprite.AddTexture( 0, tex.TextureIndex, u0, v0, su1u0, sv1v0 );

		// TC
		_UI.Sprite.AddSprite( RenderPass, Layer, x1, y0, pos.Z, sx2x1, sy1y0, E_Align.TopLeft, ref ColorFinal, ref RenderState );
		_UI.Sprite.AddTexture( 0, tex.TextureIndex, u1, v0, su2u1, sv1v0 );

		// TR
		_UI.Sprite.AddSprite( RenderPass, Layer, x2, y0, pos.Z, sx3x2, sy1y0, E_Align.TopLeft, ref ColorFinal, ref RenderState );
		_UI.Sprite.AddTexture( 0, tex.TextureIndex, u2, v0, su3u2, sv1v0 );

		// ML
		_UI.Sprite.AddSprite( RenderPass, Layer, x0, y1, pos.Z, sx1x0, sy2y1, E_Align.TopLeft, ref ColorFinal, ref RenderState );
		_UI.Sprite.AddTexture( 0, tex.TextureIndex, u0, v1, su1u0, sv2v1 );

		// MC
		_UI.Sprite.AddSprite( RenderPass, Layer, x1, y1, pos.Z, sx2x1, sy2y1, E_Align.TopLeft, ref ColorFinal, ref RenderState );
		_UI.Sprite.AddTexture( 0, tex.TextureIndex, u1, v1, su2u1, sv2v1 );

		// MR
		_UI.Sprite.AddSprite( RenderPass, Layer, x2, y1, pos.Z, sx3x2, sy2y1, E_Align.TopLeft, ref ColorFinal, ref RenderState );
		_UI.Sprite.AddTexture( 0, tex.TextureIndex, u2, v1, su3u2, sv2v1 );

		// BL
		_UI.Sprite.AddSprite( RenderPass, Layer, x0, y2, pos.Z, sx1x0, sy3y2, E_Align.TopLeft, ref ColorFinal, ref RenderState );
		_UI.Sprite.AddTexture( 0, tex.TextureIndex, u0, v2, su1u0, sv3v2 );

		// BC
		_UI.Sprite.AddSprite( RenderPass, Layer, x1, y2, pos.Z, sx2x1, sy3y2, E_Align.TopLeft, ref ColorFinal, ref RenderState );
		_UI.Sprite.AddTexture( 0, tex.TextureIndex, u1, v2, su2u1, sv3v2 );

		// BR
		_UI.Sprite.AddSprite( RenderPass, Layer, x2, y2, pos.Z, sx3x2, sy3y2, E_Align.TopLeft, ref ColorFinal, ref RenderState );
		_UI.Sprite.AddTexture( 0, tex.TextureIndex, u2, v2, su3u2, sv3v2 );

		} // auto transform
	}

	//
	public Vector2		CornerSize;
	public Vector2		CornerPuv01;
	public Vector2		CornerSuv01;
	//
};

}; // namespace UI
