//-----------------------------------------------
// XUI - FontDataXML.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;

namespace XUI.UI
{

// class FontDataXML
[XmlRoot( "font" )]
public class FontDataXML
{
	// FontDataXML
	public FontDataXML()
	{
		//
	}

	// Load
	public static FontDataXML Load( string filename )
	{
		TextReader textReader = new StreamReader( filename );
		XmlSerializer deserializer = new XmlSerializer( typeof( FontDataXML ) );
		FontDataXML fontData = (FontDataXML)deserializer.Deserialize( textReader );
		textReader.Close();

		return fontData;
	}

	//
	[XmlElement( "info" )]									public FontDataInfoXML					Info			{ get; set; }
	[XmlElement( "common" )]								public FontDataCommonXML				Common			{ get; set; }
	[XmlArray( "pages" )][XmlArrayItem( "page" )]			public List< FontDataPageXML >			Pages			{ get; set; }
	[XmlArray( "chars" )][XmlArrayItem( "char" )]			public List< FontDataCharXML >			Chars			{ get; set; }
	[XmlArray( "kernings" )][XmlArrayItem( "kerning" )]		public List< FontDataKerningXML >		Kernings		{ get; set; }
	//
};

// class FontDataInfoXML
public class FontDataInfoXML
{
	//
	[XmlAttribute( "face" )]		public string		Face				{ get; set; }
	[XmlAttribute( "size" )]		public int			Size				{ get; set; }
	[XmlAttribute( "bold" )]		public int			Bold				{ get; set; }
	[XmlAttribute( "italic" )]		public int			Italic				{ get; set; }
	[XmlAttribute( "charset" )]		public string		CharSet				{ get; set; }
	[XmlAttribute( "unicode" )]		public int			Unicode				{ get; set; }
	[XmlAttribute( "stretchH" )]	public int			StretchHeight		{ get; set; }
	[XmlAttribute( "smooth" )]		public int			Smooth				{ get; set; }
	[XmlAttribute( "aa" )]			public int			SuperSampling		{ get; set; }
	[XmlAttribute( "outline" )]		public int			Outline				{ get; set; }
	[XmlAttribute( "padding" )]		public string		Padding				{ set { String[] padding = value.Split( ',' ); PaddingRectangle = new Rectangle( int.Parse( padding[ 0 ] ), int.Parse( padding[ 1 ] ), int.Parse( padding[ 2 ] ), int.Parse( padding[ 3 ] ) ); } }
	[XmlAttribute( "spacing" )]		public string		Spacing				{ set { String[] spacing = value.Split( ',' ); SpacingPoint = new Point( int.Parse( spacing[ 0 ] ), int.Parse( spacing[ 1 ] ) ); } }
									public Rectangle	PaddingRectangle	{ get; set; }
									public Point		SpacingPoint		{ get; set; }
	//
};

// class FontDataCommonXML
public class FontDataCommonXML
{
	//
	[XmlAttribute( "lineHeight" )]	public int		LineHeight			{ get; set; }
	[XmlAttribute( "base" )]		public int		Base				{ get; set; }
	[XmlAttribute( "scaleW" )]		public int		ScaleW				{ get; set; }
	[XmlAttribute( "scaleH" )]		public int		ScaleH				{ get; set; }
	[XmlAttribute( "pages" )]		public int		Pages				{ get; set; }
	[XmlAttribute( "packed" )]		public int		Packed				{ get; set; }
	[XmlAttribute( "alphaChnl" )]	public int		AlphaChannel		{ get; set; }
	[XmlAttribute( "redChnl" )]		public int		RedChannel			{ get; set; }
	[XmlAttribute( "greenChnl" )]	public int		GreenChannel		{ get; set; }
	[XmlAttribute( "blueChnl" )]	public int		BlueChannel			{ get; set; }
	//
};

// class FontDataPageXML
public class FontDataPageXML
{
	//
	[XmlAttribute( "id" )]			public int			ID			{ get; set; }
	[XmlAttribute( "file" )]		public string		File		{ get; set; }
	//
};

// class FontDataCharXML
public class FontDataCharXML
{
	//
	[XmlAttribute( "id" )]			public int		ID				{ get; set; }
	[XmlAttribute( "x" )]			public int		X				{ get; set; }
	[XmlAttribute( "y" )]			public int		Y				{ get; set; }
	[XmlAttribute( "width" )]		public int		Width			{ get; set; }
	[XmlAttribute( "height" )]		public int		Height			{ get; set; }
	[XmlAttribute( "xoffset" )]		public int		XOffset			{ get; set; }
	[XmlAttribute( "yoffset" )]		public int		YOffset			{ get; set; }
	[XmlAttribute( "xadvance" )]	public int		XAdvance		{ get; set; }
	[XmlAttribute( "page" )]		public int		Page			{ get; set; }
	[XmlAttribute( "chnl" )]		public int		Channel			{ get; set; }
	//
};

// class FontDataKerningXML
public class FontDataKerningXML
{
	//
	[XmlAttribute( "first" )]		public int		First		{ get; set; }
	[XmlAttribute( "second" )]		public int		Second		{ get; set; }
	[XmlAttribute( "amount" )]		public int		Amount		{ get; set; }
	//
};

}; // namespace UI
