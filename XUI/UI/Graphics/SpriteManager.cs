//-----------------------------------------------
// XUI - SpriteManager.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XUI.UI
{

// struct VertexSprite
public struct VertexSprite : IVertexType
{
	VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }

	//
	public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration (
		new VertexElement(  0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0 ),
		new VertexElement( 12, VertexElementFormat.Color, VertexElementUsage.Color, 0 ),
		new VertexElement( 16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0 ),
		new VertexElement( 24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1 ),
		new VertexElement( 32, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 2 )
	);

	public Vector3		Position;
	public uint			Color;
	public Vector2		UV0;
	public Vector2		UV1;
	public Vector2		UV2;
	//
};

// struct AutoTransform
public struct AutoTransform : IDisposable
{
	// AutoTransform
	public AutoTransform( WidgetBase widget )
	{
		Widget = widget;

		OldIndex = _UI.Sprite.AutoTransformIndex;
		OldOffset = _UI.Sprite.AutoTransformOffset;

		int newIndex = 0;
		Vector3 newOffset = new Vector3();

		if ( Widget.IsFlagSet( E_WidgetFlag.UseMatrix ) )
		{
			newIndex = _UI.Sprite.StorePostMatrix( ref Widget.TransformMatrix );
			newOffset = -Widget.Position;
		}

		_UI.Sprite.SetAutoTransform( newIndex, ref newOffset );
	}

	// Dispose
	public void Dispose()
	{
		_UI.Sprite.SetAutoTransform( OldIndex, ref OldOffset );
	}

	//
	private WidgetBase		Widget;
	private int				OldIndex;
	private Vector3			OldOffset;
	//
};

// class SpriteManager
public class SpriteManager
{
#if !RELEASE
	static Diagnostics.BoolD d_ShowWireFrame = new Diagnostics.BoolD( "UI.ShowWireFrame", false );
#endif

	// SpriteManager
	public SpriteManager()
	{
	#if !RELEASE
		RasterizerState_WireFrame = new RasterizerState();
		RasterizerState_WireFrame.FillMode = FillMode.WireFrame;
	#endif

		GraphicsDevice = _UI.Game.GraphicsDevice;

		PresentationParameters pp = GraphicsDevice.PresentationParameters;
		BackBufferSize = new Vector2( pp.BackBufferWidth, pp.BackBufferHeight );

		SpriteCountMax = _UI.Settings.Sprite_Count;
		Sprites = new Sprite[ SpriteCountMax ];
		SpriteCount = 0;
		CurrentSprite = 0;

		HasBatched = new bool[ _UI.Settings.Sprite_RenderPassCount ];

		TopLayer = _UI.Settings.Sprite_LayerCount - 1;

		RenderPassLayerSprites = new List< List< int >[] >( _UI.Settings.Sprite_RenderPassCount );
		RenderPass3dMask = _UI.Settings.Sprite_RenderPass3dMask;

		for ( int i = 0; i < _UI.Settings.Sprite_RenderPassCount; ++i )
		{
			List< int >[] layerSprites = new List< int >[ _UI.Settings.Sprite_LayerCount ];

			for ( int j = 0; j < layerSprites.Length; ++j )
				layerSprites[ j ] = new List< int >();

			RenderPassLayerSprites.Add( layerSprites );
		}

		PreMatrixPool = new Matrix[ _UI.Settings.Sprite_PreMatrixCount ];
		PreMatrixCount = 0;

		PostMatrixPool = new Matrix[ _UI.Settings.Sprite_PostMatrixCount ];
		PostMatrixCount = 0;

		Matrix identity = Matrix.Identity;

		// reserve identity
		StorePreMatrix( ref identity );
		StorePostMatrix( ref identity );

		RenderStatePool = new object[ _UI.Settings.Sprite_RenderStateCount ];
		RenderStateCount = 0;

		// default renderstates
		StoreRenderState( DepthStencilState.None );
		StoreRenderState( RasterizerState.CullNone );

		RenderStateStack_Blend = new Stack< int >( _UI.Settings.Sprite_RenderStateStackCount );
		RenderStateStack_DepthStencil = new Stack< int >( _UI.Settings.Sprite_RenderStateStackCount );
		RenderStateStack_Rasterizer = new Stack< int >( _UI.Settings.Sprite_RenderStateStackCount );

		VertexCorners = new Vector2[ VertexCount ]
		{
			new Vector2( 0.0f, 0.0f ),
			new Vector2( 1.0f, 0.0f ),
			new Vector2( 1.0f, 1.0f ),
			new Vector2( 0.0f, 1.0f ),
		};

		VertexCornersFlipped = new Vector2[ VertexCount ]
		{
			new Vector2( 0.0f, 1.0f ),
			new Vector2( 1.0f, 1.0f ),
			new Vector2( 1.0f, 0.0f ),
			new Vector2( 0.0f, 0.0f ),
		};

		VertexOffsetAligned = new Vector2[ (int)E_Align.Count ]
		{
			new Vector2( 0.0f, 0.0f ), // None
			new Vector2( 0.0f, 0.0f ), // TopLeft
			new Vector2( 0.5f, 0.0f ), // TopCentre
			new Vector2( 1.0f, 0.0f ), // TopRight
			new Vector2( 0.0f, 0.5f ), // MiddleLeft
			new Vector2( 0.5f, 0.5f ), // MiddleCentre
			new Vector2( 1.0f, 0.5f ), // MiddleRight
			new Vector2( 0.0f, 1.0f ), // BottomLeft
			new Vector2( 0.5f, 1.0f ), // BottomCentre
			new Vector2( 1.0f, 1.0f ), // BottomRight
		};

		VertexOffsetAlignedFlipped = new Vector2[ (int)E_Align.Count ]
		{
			new Vector2( 0.0f, 1.0f ), // None
			new Vector2( 0.0f, 1.0f ), // TopLeft
			new Vector2( 0.5f, 1.0f ), // TopCentre
			new Vector2( 1.0f, 1.0f ), // TopRight
			new Vector2( 0.0f, 0.5f ), // MiddleLeft
			new Vector2( 0.5f, 0.5f ), // MiddleCentre
			new Vector2( 1.0f, 0.5f ), // MiddleRight
			new Vector2( 0.0f, 0.0f ), // BottomLeft
			new Vector2( 0.5f, 0.0f ), // BottomCentre
			new Vector2( 1.0f, 0.0f ), // BottomRight
		};

		VertexCornersAligned = new Vector3[ (int)E_Align.Count ][];
		
		for ( int i = 0; i < VertexCornersAligned.Length; ++i )
			VertexCornersAligned[ i ] = new Vector3[ VertexCount ];

		VertexCornersAlignedFlipped = new Vector3[ (int)E_Align.Count ][];

		for ( int i = 0; i < VertexCornersAlignedFlipped.Length; ++i )
			VertexCornersAlignedFlipped[ i ] = new Vector3[ VertexCount ];

		for ( int i = 0; i < (int)E_Align.Count; ++i )
		{
			for ( int j = 0; j < VertexCount; ++j )
			{
				VertexCornersAligned[ i ][ j ] = new Vector3( VertexCorners[ j ] - VertexOffsetAligned[ i ], 0.0f );
				VertexCornersAlignedFlipped[ i ][ j ] = new Vector3( VertexCornersFlipped[ j ] - VertexOffsetAlignedFlipped[ i ], 0.0f );
			}
		}

		TransformMatrix2D = Matrix.CreateTranslation( -0.5f, -0.5f, 0.0f ) * Matrix.CreateOrthographicOffCenter( 0.0f, pp.BackBufferWidth, pp.BackBufferHeight, 0.0f, -1500.0f, 1500.0f );
		TransformMatrix = TransformMatrix2D; // default

		RenderPassTransformMatrix = new Matrix[ _UI.Settings.Sprite_RenderPassCount ];

		for ( int i = 0; i < RenderPassTransformMatrix.Length; ++i )
			RenderPassTransformMatrix[ i ] = Matrix.Identity;

		AutoTransformIndex = 0;
		AutoTransformOffset = new Vector3();

		VertexData = new VertexSprite[ VertexCount * SpriteCountMax ];
		VertexPosition = 0;
		
		DynamicVB = new DynamicVertexBuffer( GraphicsDevice, VertexSprite.VertexDeclaration, SpriteCountMax * VertexCount, BufferUsage.WriteOnly );

	#if WINDOWS
		DynamicVB.ContentLost += ( ( sender, e ) => { VertexPosition = 0; } );
	#endif

		short primitiveCount = 0;
		short[] dataIB = new short[ SpriteCountMax * IndexCount ];

		for ( int i = 0; i < dataIB.Length; )
		{
			short offset = (short)( primitiveCount * VertexCount );

			dataIB[ i++ ] = (short)( 0 + offset );
			dataIB[ i++ ] = (short)( 1 + offset );
			dataIB[ i++ ] = (short)( 2 + offset );
			dataIB[ i++ ] = (short)( 2 + offset );
			dataIB[ i++ ] = (short)( 3 + offset );
			dataIB[ i++ ] = (short)( 0 + offset );

			++primitiveCount;
		}

		DynamicIB = new DynamicIndexBuffer( GraphicsDevice, IndexElementSize.SixteenBits, SpriteCountMax * IndexCount, BufferUsage.WriteOnly );
		DynamicIB.SetData( dataIB );

	#if WINDOWS
		DataIB = dataIB;
		DynamicIB.ContentLost += ( ( sender, e ) => { DynamicIB.SetData( DataIB ); } );
	#endif
	}

	// StorePreMatrix
	public int StorePreMatrix( ref Matrix matrix )
	{
		if ( PreMatrixCount == _UI.Settings.Sprite_PreMatrixCount )
			return 0;

		int count = PreMatrixCount++;
		PreMatrixPool[ count ] = matrix;
		return count;
	}

	// StorePostMatrix
	public int StorePostMatrix( ref Matrix matrix )
	{
		if ( PostMatrixCount == _UI.Settings.Sprite_PostMatrixCount )
			return 0;

		int count = PostMatrixCount++;

		if ( AutoTransformIndex != 0 )
			PostMatrixPool[ count ] = PostMatrixPool[ AutoTransformIndex ] * matrix;
		else
			PostMatrixPool[ count ] = matrix;

		return count;
	}

	// StoreRenderState
	private int StoreRenderState( object renderState )
	{
		if ( RenderStateCount == _UI.Settings.Sprite_RenderStateCount )
			return -1;

		int count = RenderStateCount++;
		RenderStatePool[ count ] = renderState;
		return count;
	}

	// AddSprite
	public unsafe void AddSprite( int renderPass, int layer, BlendState blendState, bool pushState )
	{
		if ( SpriteCount == SpriteCountMax )
			return;

		fixed ( Sprite* pSprite = &Sprites[ SpriteCount ] )
		{
			pSprite->Type = E_SpriteType.BlendState;

			pSprite->PreTransformIndex = pushState ? 1 : 0;
			pSprite->PostTransformIndex = pushState ? StoreRenderState( blendState ) : 0;

			pSprite->BatchCount = 1;
		}

		RenderPassLayerSprites[ renderPass ][ layer ].Add( SpriteCount );
		CurrentSprite = SpriteCount++;
	}

	public unsafe void AddSprite( int renderPass, int layer, DepthStencilState depthStencilState, int stencilRef, bool pushState )
	{
		if ( SpriteCount == SpriteCountMax )
			return;

		fixed ( Sprite* pSprite = &Sprites[ SpriteCount ] )
		{
			pSprite->Type = E_SpriteType.DepthStencilState;

			pSprite->Texture0.TextureIndex = stencilRef;

			pSprite->PreTransformIndex = pushState ? 1 : 0;
			pSprite->PostTransformIndex = pushState ? StoreRenderState( depthStencilState ) : 0;

			pSprite->BatchCount = 1;
		}

		RenderPassLayerSprites[ renderPass ][ layer ].Add( SpriteCount );
		CurrentSprite = SpriteCount++;
	}

	public unsafe void AddSprite( int renderPass, int layer, RasterizerState rasterizerState, ref Vector2 scissorPosition, ref Vector2 scissorSize, bool pushState )
	{
		if ( SpriteCount == SpriteCountMax )
			return;

		fixed ( Sprite* pSprite = &Sprites[ SpriteCount ] )
		{
			pSprite->Type = E_SpriteType.RasterizerState;

			pSprite->Position.X = scissorPosition.X;
			pSprite->Position.Y = scissorPosition.Y;
			pSprite->Size.X = scissorSize.X;
			pSprite->Size.Y = scissorSize.Y;

			pSprite->PreTransformIndex = pushState ? 1 : 0;
			pSprite->PostTransformIndex = pushState ? StoreRenderState( rasterizerState ) : 0;

			pSprite->BatchCount = 1;
		}

		RenderPassLayerSprites[ renderPass ][ layer ].Add( SpriteCount );
		CurrentSprite = SpriteCount++;
	}

	public unsafe void AddSprite( int renderPass, int layer, ref Vector3 pos, float sX, float sY, E_Align align, ref SpriteColors colors, ref RenderState renderState )
	{
		if ( SpriteCount == SpriteCountMax )
			return;

		fixed ( Sprite* pSprite = &Sprites[ SpriteCount ] )
		{
			pSprite->Type = E_SpriteType.Sprite;

			pSprite->Position.X = pos.X + AutoTransformOffset.X;
			pSprite->Position.Y = pos.Y + AutoTransformOffset.Y;
			pSprite->Position.Z = pos.Z + AutoTransformOffset.Z;

			pSprite->Size.X = sX;
			pSprite->Size.Y = sY;
			pSprite->Align = align;
			pSprite->RenderState = renderState;

			pSprite->PreTransformIndex = 0;
			pSprite->PostTransformIndex = AutoTransformIndex;

			pSprite->Colors = colors;

			if ( ( renderState.BlendState == E_BlendState.AlphaBlend ) && !pSprite->Colors.PreMultipliedAlpha )
				pSprite->Colors.ToPremultiplied();

			pSprite->Texture0.TextureIndex = -1;
			pSprite->Texture1.TextureIndex = -1;
			pSprite->Texture2.TextureIndex = -1;

			pSprite->BatchCount = 1;
		}

		RenderPassLayerSprites[ renderPass ][ layer ].Add( SpriteCount );
		CurrentSprite = SpriteCount++;
	}

	public void AddSprite( int renderPass, int layer, float pX, float pY, float pZ, float sX, float sY, E_Align align, ref SpriteColors colors, ref RenderState renderState )
	{
		Vector3 p = new Vector3( pX, pY, pZ );
		AddSprite( renderPass, layer, ref p, sX, sY, align, ref colors, ref renderState );
	}

	// AddTexture
	public unsafe void AddTexture( int slot, int textureIndex, ref Vector2 puv, ref Vector2 suv )
	{
		fixed ( Sprite* pSprite = &Sprites[ CurrentSprite ] )
		{
			SpriteTexture* pSpriteTexture = &pSprite->Texture0 + slot;

			pSpriteTexture->TextureIndex = textureIndex;
			pSpriteTexture->PUV.X = puv.X;
			pSpriteTexture->PUV.Y = puv.Y;
			pSpriteTexture->SUV.X = suv.X;
			pSpriteTexture->SUV.Y = suv.Y;
		}
	}

	public void AddTexture( int slot, int textureIndex, float pu, float pv, float su, float sv )
	{
		Vector2 puv = new Vector2( pu, pv );
		Vector2 suv = new Vector2( su, sv );

		AddTexture( slot, textureIndex, ref puv, ref suv );
	}

	public void AddTexture( int slot, int textureIndex )
	{
		Vector2 puv = new Vector2( 0.0f, 0.0f );
		Vector2 suv = new Vector2( 1.0f, 1.0f );

		AddTexture( slot, textureIndex, ref puv, ref suv );
	}

	public void AddTexture( int slot, ref SpriteTexture texture )
	{
		AddTexture( slot, texture.TextureIndex, ref texture.PUV, ref texture.SUV );
	}

	public void AddTexture( int slot, SpriteTexture texture )
	{
		AddTexture( slot, texture.TextureIndex, ref texture.PUV, ref texture.SUV );
	}

	// AddPreMatrix
	public unsafe void AddPreMatrix( int index )
	{
		fixed ( Sprite* pSprite = &Sprites[ CurrentSprite ] )
			pSprite->PreTransformIndex = index;
	}

	// AddPostMatrix
	public unsafe void AddPostMatrix( int index )
	{
		fixed ( Sprite* pSprite = &Sprites[ CurrentSprite ] )
			pSprite->PostTransformIndex = index;
	}

	// BeginUpdate
	public void BeginUpdate()
	{
		SpriteCount = 0;

		for ( int i = 0; i < RenderPassLayerSprites.Count; ++i )
			for ( int j = 0; j < RenderPassLayerSprites[ i ].Length; ++j )
				RenderPassLayerSprites[ i ][ j ].Clear();

		for ( int i = 0; i < HasBatched.Length; ++i )
			HasBatched[ i ] = false;

		PreMatrixCount = 1; // identity is reserved
		PostMatrixCount = 1; // identity is reserved

		RenderStateCount = 2; // default DepthStencilState and RasterizerState are reserved
	}

	// Render
	public void Render( int renderPass )
	{
		TransformMatrix = RenderPassTransformMatrix[ renderPass ];

		if ( !IsRenderPass3D( renderPass ) )
			Matrix.Multiply( ref TransformMatrix, ref TransformMatrix2D, out TransformMatrix );

		bool hasBatched = HasBatched[ renderPass ];

		List< int >[] layerSprites = RenderPassLayerSprites[ renderPass ];

		for ( int i = 0; i < layerSprites.Length; ++i )
		{
			List< int > sprites = layerSprites[ i ];

			if ( sprites.Count == 0 )
				continue;

			if ( !hasBatched )
				Batch( sprites );

			RenderLayer( renderPass, sprites );
		}

		HasBatched[ renderPass ] = true;
	}

	// Batch
	private unsafe void Batch( List< int > sprites )
	{
		fixed ( Sprite* pSpriteStart = &Sprites[ 0 ] )
		{
			Sprite* pSprite = ( pSpriteStart + sprites[ 0 ] );

			for ( int j = 1; j < sprites.Count; ++j )
			{
				bool doBatch = true;
				
				Sprite* pSpriteNext = ( pSpriteStart + sprites[ j ] );

				// type
				doBatch &= ( pSprite->Type == pSpriteNext->Type );

				// textures
				doBatch &= ( pSprite->Texture0.TextureIndex == pSpriteNext->Texture0.TextureIndex );
				doBatch &= ( pSprite->Texture1.TextureIndex == pSpriteNext->Texture1.TextureIndex );
				doBatch &= ( pSprite->Texture2.TextureIndex == pSpriteNext->Texture2.TextureIndex );

				// renderstate
				doBatch &= ( pSprite->RenderState.Effect == pSpriteNext->RenderState.Effect );
				doBatch &= ( pSprite->RenderState.BlendState == pSpriteNext->RenderState.BlendState );
				doBatch &= ( pSprite->RenderState.SamplerState0 == pSpriteNext->RenderState.SamplerState0 );
				doBatch &= ( pSprite->RenderState.SamplerState1 == pSpriteNext->RenderState.SamplerState1 );
				doBatch &= ( pSprite->RenderState.SamplerState2 == pSpriteNext->RenderState.SamplerState2 );
				doBatch &= ( pSprite->RenderState.EffectParamF == pSpriteNext->RenderState.EffectParamF );
				doBatch &= ( pSprite->RenderState.EffectParamI == pSpriteNext->RenderState.EffectParamI );
				
				if ( doBatch )
					pSprite->BatchCount++;
				else
					pSprite = pSpriteNext;
			}
		}
	}

	// RenderLayer
	private unsafe void RenderLayer( int renderPass, List< int > sprites )
	{
		RenderStateStack_Blend.Clear();
		RenderStateStack_DepthStencil.Clear();
		RenderStateStack_Rasterizer.Clear();

		// set default render states
		GraphicsDevice.DepthStencilState = (DepthStencilState)RenderStatePool[ 0 ];
		GraphicsDevice.RasterizerState = (RasterizerState)RenderStatePool[ 1 ];

	#if !RELEASE
		if ( d_ShowWireFrame )
			GraphicsDevice.RasterizerState = RasterizerState_WireFrame;
	#endif

		int vertexStride = VertexSprite.VertexDeclaration.VertexStride;

		fixed ( Sprite* pSpriteStart = &Sprites[ 0 ] )
		fixed ( VertexSprite* pVertexDataStart = &VertexData[ 0 ] )
		{
			for ( int i = 0; i < sprites.Count; )
			{
				Sprite* pSprite = ( pSpriteStart + sprites[ i ] );

				if ( pSprite->Type == E_SpriteType.BlendState )
				{
					if ( pSprite->PreTransformIndex != 0 )
					{
						RenderStateStack_Blend.Push( i );
						GraphicsDevice.BlendState = (BlendState)RenderStatePool[ pSprite->PostTransformIndex ];
					}
					else
					{
						RenderStateStack_Blend.Pop();

						if ( RenderStateStack_Blend.Count != 0 )
						{
							Sprite* pSpriteRenderState = ( pSpriteStart + RenderStateStack_Blend.Peek() );
							GraphicsDevice.BlendState = (BlendState)RenderStatePool[ pSprite->PostTransformIndex ];
						}
						
						// else BlendState is already set per-batch
					}

					++i;
					continue;
				}
				else
				if ( pSprite->Type == E_SpriteType.DepthStencilState )
				{
					if ( pSprite->PreTransformIndex != 0 )
					{
						RenderStateStack_DepthStencil.Push( i );
						GraphicsDevice.DepthStencilState = (DepthStencilState)RenderStatePool[ pSprite->PostTransformIndex ];
						GraphicsDevice.ReferenceStencil = pSprite->Texture0.TextureIndex;
					}
					else
					{
						RenderStateStack_DepthStencil.Pop();

						if ( RenderStateStack_DepthStencil.Count == 0 )
							GraphicsDevice.DepthStencilState = (DepthStencilState)RenderStatePool[ 0 ]; // default
						else
						{
							Sprite* pSpriteRenderState = ( pSpriteStart + RenderStateStack_DepthStencil.Peek() );
							GraphicsDevice.DepthStencilState = (DepthStencilState)RenderStatePool[ pSpriteRenderState->PostTransformIndex ];
							GraphicsDevice.ReferenceStencil = pSpriteRenderState->Texture0.TextureIndex;
						}
					}

					++i;
					continue;
				}
				else
				if ( pSprite->Type == E_SpriteType.RasterizerState )
				{
					if ( pSprite->PreTransformIndex != 0 )
					{
						RenderStateStack_Rasterizer.Push( i );
						SetRasterizerState( pSprite );
					}
					else
					{
						RenderStateStack_Rasterizer.Pop();

						if ( RenderStateStack_Rasterizer.Count == 0 )
							GraphicsDevice.RasterizerState = (RasterizerState)RenderStatePool[ 1 ]; // default
						else
						{
							Sprite* pSpriteRenderState = ( pSpriteStart + RenderStateStack_Rasterizer.Peek() );
							SetRasterizerState( pSpriteRenderState );
						}
					}

					++i;
					continue;
				}

				// else we must be a Sprite type

				// textures and sampler states
				int textureCount = 0;

				if ( pSprite->Texture0.TextureIndex != -1 )
				{
					GraphicsDevice.Textures[ textureCount ] = _UI.Texture.Get( pSprite->Texture0.TextureIndex );
					GraphicsDevice.SamplerStates[ textureCount ] = GetSamplerState( pSprite->RenderState.SamplerState0 );
					++textureCount;
				}

				if ( pSprite->Texture1.TextureIndex != -1 )
				{
					GraphicsDevice.Textures[ textureCount ] = _UI.Texture.Get( pSprite->Texture1.TextureIndex );
					GraphicsDevice.SamplerStates[ textureCount ] = GetSamplerState( pSprite->RenderState.SamplerState1 );
					++textureCount;
				}

				if ( pSprite->Texture2.TextureIndex != -1 )
				{
					GraphicsDevice.Textures[ textureCount ] = _UI.Texture.Get( pSprite->Texture2.TextureIndex );
					GraphicsDevice.SamplerStates[ textureCount ] = GetSamplerState( pSprite->RenderState.SamplerState2 );
					++textureCount;
				}

				// blend state
				switch ( pSprite->RenderState.BlendState )
				{
					case E_BlendState.AlphaBlend:			GraphicsDevice.BlendState = BlendState.AlphaBlend;				break;
					case E_BlendState.Opaque:				GraphicsDevice.BlendState = BlendState.Opaque;					break;
					case E_BlendState.Additive:				GraphicsDevice.BlendState = BlendState.Additive;				break;
					case E_BlendState.NonPremultiplied:		GraphicsDevice.BlendState = BlendState.NonPremultiplied;		break;
					case E_BlendState.Custom:				/* do nothing - assumed you've set a custom blend state */		break;
				}

				// effect
				Effect effect = _UI.Effect.Get( pSprite->RenderState.Effect );

				_UI.Effect.GetParam_UiTransform( pSprite->RenderState.Effect ).SetValue( TransformMatrix ); // can't seem to globally set this - humph
				_UI.Effect.SetParams( ref pSprite->RenderState );

				effect.CurrentTechnique.Passes[ 0 ].Apply();

				// process batch
				SetDataOptions dataOption = SetDataOptions.NoOverwrite;

				if ( ( ( VertexPosition + pSprite->BatchCount ) * VertexCount ) > ( SpriteCountMax * VertexCount ) )
				{
					dataOption = SetDataOptions.Discard;
					VertexPosition = 0;
				}
				
				VertexSprite* pVertexData = pVertexDataStart;

				for ( int j = 0; j < pSprite->BatchCount; ++j )
				{
					Sprite* pSpriteNext = ( pSpriteStart + sprites[ i++ ] );

					Vector3[] vertexCornersAligned = IsRenderPass3D( renderPass ) ? VertexCornersAlignedFlipped[ (int)pSpriteNext->Align ] : VertexCornersAligned[ (int)pSpriteNext->Align ];

					Matrix m;
					Matrix.CreateScale( pSpriteNext->Size.X, pSpriteNext->Size.Y, 1.0f, out m );

					if ( pSpriteNext->PreTransformIndex != 0 )
						Matrix.Multiply( ref m, ref PreMatrixPool[ pSpriteNext->PreTransformIndex ], out m );

					m.Translation += pSpriteNext->Position;

					if ( pSpriteNext->PostTransformIndex != 0 )
						Matrix.Multiply( ref m, ref PostMatrixPool[ pSpriteNext->PostTransformIndex ], out m );

					fixed ( Vector2* pVertexTextureCornersStart = &VertexCorners[ 0 ] )
					{
						for ( int k = 0; k < VertexCount; ++k )
						{
							Vector3.Transform( ref vertexCornersAligned[ k ], ref m, out pVertexData->Position );

							Vector2* pVertexTextureCorners = ( pVertexTextureCornersStart + k );

							pVertexData->UV0.X = pSpriteNext->Texture0.PUV.X + ( pSpriteNext->Texture0.SUV.X * pVertexTextureCorners->X );
							pVertexData->UV0.Y = pSpriteNext->Texture0.PUV.Y + ( pSpriteNext->Texture0.SUV.Y * pVertexTextureCorners->Y );

							if ( textureCount > 1 )
							{
								pVertexData->UV1.X = pSpriteNext->Texture1.PUV.X + ( pSpriteNext->Texture1.SUV.X * pVertexTextureCorners->X );
								pVertexData->UV1.Y = pSpriteNext->Texture1.PUV.Y + ( pSpriteNext->Texture1.SUV.Y * pVertexTextureCorners->Y );

								if ( textureCount > 2 )
								{
									pVertexData->UV2.X = pSpriteNext->Texture2.PUV.X + ( pSpriteNext->Texture2.SUV.X * pVertexTextureCorners->X );
									pVertexData->UV2.Y = pSpriteNext->Texture2.PUV.Y + ( pSpriteNext->Texture2.SUV.Y * pVertexTextureCorners->Y );
								}
							}

							SpriteColor* pColor = &pSpriteNext->Colors.Color0 + k;
							pVertexData->Color = (uint)( ( pColor->A << 24 ) | ( pColor->B << 16 ) | ( pColor->G << 8 ) | pColor->R ); // ABGR

							++pVertexData;
						}
					}
				}

				DynamicVB.SetData( VertexPosition * VertexCount * vertexStride, VertexData, 0, pSprite->BatchCount * VertexCount, vertexStride, dataOption );

				GraphicsDevice.SetVertexBuffer( DynamicVB );
				GraphicsDevice.Indices = DynamicIB;

				GraphicsDevice.DrawIndexedPrimitives( PrimitiveType.TriangleList, 0, VertexPosition * VertexCount, pSprite->BatchCount * VertexCount, VertexPosition * IndexCount, pSprite->BatchCount * 2 );

				VertexPosition += pSprite->BatchCount;

				GraphicsDevice.SetVertexBuffer( null );
			}
		}
	}

	// GetSamplerState
	private SamplerState GetSamplerState( E_SamplerState samplerState )
	{
		switch ( samplerState )
		{
			case E_SamplerState.AnisotropicClamp:		return SamplerState.AnisotropicClamp;
			case E_SamplerState.AnisotropicWrap:		return SamplerState.AnisotropicWrap;
			case E_SamplerState.LinearClamp:			return SamplerState.LinearClamp;
			case E_SamplerState.LinearWrap:				return SamplerState.LinearWrap;
			case E_SamplerState.PointClamp:				return SamplerState.PointClamp;
			case E_SamplerState.PointWrap:				return SamplerState.PointWrap;
		}

		return null;
	}

	// SetRasterizerState
	private unsafe void SetRasterizerState( Sprite* pSprite )
	{
		GraphicsDevice.RasterizerState = (RasterizerState)RenderStatePool[ pSprite->PostTransformIndex ];

		if ( GraphicsDevice.RasterizerState.ScissorTestEnable )
		{
			Vector3 scissorTL = new Vector3( pSprite->Position.X, pSprite->Position.Y, 0.0f );
			Vector3 scissorBR = new Vector3( pSprite->Position.X + pSprite->Size.X, pSprite->Position.Y + pSprite->Size.Y, 0.0f );

			// device
			Vector3.Transform( ref scissorTL, ref TransformMatrix, out scissorTL );
			Vector3.Transform( ref scissorBR, ref TransformMatrix, out scissorBR );

			// screen - offset as half-pixel is in TransformMatrix2D
			scissorTL.X = 0.5f + ( (  scissorTL.X + 1.0f ) / 2.0f ) * BackBufferSize.X;
			scissorTL.Y = 0.5f + ( ( -scissorTL.Y + 1.0f ) / 2.0f ) * BackBufferSize.Y;
			scissorBR.X = 0.5f + ( (  scissorBR.X + 1.0f ) / 2.0f ) * BackBufferSize.X;
			scissorBR.Y = 0.5f + ( ( -scissorBR.Y + 1.0f ) / 2.0f ) * BackBufferSize.Y;

			GraphicsDevice.ScissorRectangle = new Rectangle( (int)scissorTL.X, (int)scissorTL.Y, (int)( scissorBR.X - scissorTL.X ), (int)( scissorBR.Y - scissorTL.Y ) );
		}
	}

	// GetVertexOffsetAligned
	public Vector2 GetVertexOffsetAligned( int renderPass, E_Align align )
	{
		if ( IsRenderPass3D( renderPass ) )
			return VertexOffsetAlignedFlipped[ (int)align ];

		return VertexOffsetAligned[ (int)align ];
	}

	// IsRenderPass3D
	public bool IsRenderPass3D( int renderPass )
	{
		return ( ( RenderPass3dMask & ( 1 << renderPass ) ) != 0 );
	}

	// SetAutoTransform
	public void SetAutoTransform( int index, ref Vector3 offset )
	{
		AutoTransformIndex = index;
		AutoTransformOffset = offset;
	}

	//
#if !RELEASE
	private RasterizerState			RasterizerState_WireFrame;
#endif

	private GraphicsDevice			GraphicsDevice;
	private Vector2					BackBufferSize;

	private int						SpriteCountMax;
	private Sprite[]				Sprites;
	private int						SpriteCount;
	private int						CurrentSprite;

	private bool[]					HasBatched;

	public  int						TopLayer		{ get; private set; }

	private List< List< int >[] >	RenderPassLayerSprites;
	private int						RenderPass3dMask;

	private Matrix[]				PreMatrixPool;
	private int						PreMatrixCount;

	private Matrix[]				PostMatrixPool;
	private int						PostMatrixCount;

	private object[]				RenderStatePool;
	private int						RenderStateCount;

	private Stack< int >			RenderStateStack_Blend;
	private Stack< int >			RenderStateStack_DepthStencil;
	private Stack< int >			RenderStateStack_Rasterizer;

	private Vector2[]				VertexCorners;
	private Vector2[]				VertexCornersFlipped;
	private Vector2[]				VertexOffsetAligned;
	private Vector2[]				VertexOffsetAlignedFlipped;
	private Vector3[][]				VertexCornersAligned;
	private Vector3[][]				VertexCornersAlignedFlipped;

	private  Matrix					TransformMatrix;
	private  Matrix					TransformMatrix2D;
	public   Matrix[]				RenderPassTransformMatrix		{ get; private set; }

	public  int						AutoTransformIndex				{ get; private set; }
	public  Vector3					AutoTransformOffset				{ get; private set; }

	private VertexSprite[]			VertexData;
	private int						VertexPosition;

	private const int				VertexCount = 4;
	private const int				IndexCount = 6;

	private DynamicVertexBuffer		DynamicVB;
	private DynamicIndexBuffer		DynamicIB;

#if WINDOWS
	private short[]					DataIB;
#endif
	//
};

}; // namespace UI
