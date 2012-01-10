//-----------------------------------------------
// XUI - Settings.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

namespace XUI.UI
{

// class Settings
public class Settings
{
	//
	public int		Effect_Count = (int)E_Effect.Count;

	public int		Sprite_Count = 1024;
	public int		Sprite_RenderPassCount = 3;
	public int		Sprite_RenderPass3dMask = 2;
	public int		Sprite_LayerCount = 8;
	public int		Sprite_PreMatrixCount = 32;
	public int		Sprite_PostMatrixCount = 512;
	public int		Sprite_RenderStateCount = 16;
	public int		Sprite_RenderStateStackCount = 4;

	public int		Font_SpriteCount = 256;

	public int		Texture_BundleCount = 4;

	public int		Screen_MessageCount = 16;
	public float	Screen_SafeAreaSize = 0.9f;
	//
};

}; // namespace UI
