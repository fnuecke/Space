//-----------------------------------------------
// XUI - SafeArea.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

namespace XUI.UI
{

// class WidgetSafeArea
public class WidgetSafeArea : WidgetBase
{
	// WidgetSafeArea
	public WidgetSafeArea()
		: base()
	{
		Position.X = _UI.SXL;
		Position.Y = _UI.SYT;

		Size.X = _UI.SSX;
		Size.Y = _UI.SSY;
	}
};

}; // namespace UI
