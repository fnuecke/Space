//-----------------------------------------------
// XUI - FontData.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System.Collections.Generic;

namespace XUI.UI
{

// class Font
public class Font
{
	// Font
	public Font( string path, string name )
	{
		Name = name;

		_UI.Texture.Add( 0, path + name, name );
		TextureIndex = _UI.Texture.Get( name );

		FontDataXML fontDataXML = FontDataXML.Load( _UI.Content.RootDirectory + "\\" + path + name + ".fnt" );
		FontData = new FontData( fontDataXML );
	}

	//
	public string		Name;
	public int			TextureIndex;
	public FontData		FontData;
	//
};

// struct FontDataChar
public struct FontDataChar
{
	// FontDataChar
	public FontDataChar( FontDataCharXML dataXML )
	{
		X = dataXML.X;
		Y = dataXML.Y;
		Width = dataXML.Width;
		Height = dataXML.Height;
		OffsetX = dataXML.XOffset;
		OffsetY = dataXML.YOffset;
		AdvanceX = dataXML.XAdvance;
	}

	//
	public int		X;
	public int		Y;
	public int		Width;
	public int		Height;
	public int		OffsetX;
	public int		OffsetY;
	public int		AdvanceX;
	//
};

// class FontData
public class FontData
{
	// FontData
	public FontData( FontDataXML dataXML )
	{
		FontHeight = dataXML.Info.Size;
		LineHeight = dataXML.Common.LineHeight;
		TextureWidth = dataXML.Common.ScaleW;
		TextureHeight = dataXML.Common.ScaleH;

		Characters = new FontDataChar[ dataXML.Chars.Count ];
		CharMap = new Dictionary< char, int >( Characters.Length );

		for ( int i = 0; i < dataXML.Chars.Count; ++i )
		{
			Characters[ i ] = new FontDataChar( dataXML.Chars[ i ] );
			CharMap.Add( (char)dataXML.Chars[ i ].ID, i );
		}

		NullCharacter = CharMap[ (char)0xffff ];

		KerningMap = new Dictionary< int, int >( dataXML.Kernings.Count );

		for ( int i = 0; i < dataXML.Kernings.Count; ++i )
			KerningMap.Add( GetKerningHash( (char)dataXML.Kernings[ i ].First, (char)dataXML.Kernings[ i ].Second ), dataXML.Kernings[ i ].Amount );
	}

	// GetKerningHash
	private int GetKerningHash( char first, char second )
	{
		return (int)( ( first << 16 ) | second );
	}

	// GetCharacterIndex
	public int GetCharacterIndex( char character )
	{
		int result;

		if ( CharMap.TryGetValue( character, out result ) )
			return result;

		return NullCharacter;
	}

	// GetKerningAmount
	public int GetKerningAmount( char first, char second )
	{
		int kerningHash = GetKerningHash( first, second );

		int result;

		if ( KerningMap.TryGetValue( kerningHash, out result ) )
			return result;

		return 0;
	}

	//
	public  int							FontHeight;
	public  int							LineHeight;
	public  int							TextureWidth;
	public  int							TextureHeight;

	public  FontDataChar[]				Characters;
	private int							NullCharacter;

	private Dictionary< char, int >		CharMap;
	private Dictionary< int, int >		KerningMap;
	//
};

}; // namespace UI
