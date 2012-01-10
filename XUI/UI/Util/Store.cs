//-----------------------------------------------
// XUI - Store.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System.Collections.Generic;

namespace XUI.UI
{

// class Store
public class Store< T >
{
	// Store
	public Store()
		: this( default( T ) )
	{
		//
	}

	public Store( T defaultValue )
	{
		StoreT = new Dictionary< string, T >();
		DefaultT = defaultValue;
	}

	// Add
	public void Add( string name, T o )
	{
		StoreT.Add( name, o );
	}

	// Get
	public T Get( string name )
	{
		T o;

		if ( StoreT.TryGetValue( name, out o ) )
			return o;

		return DefaultT;
	}

	//
	private Dictionary< string, T >		StoreT;
	private T							DefaultT;
	//
};

}; // namespace UI
