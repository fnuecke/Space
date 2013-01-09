using System;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Util
{
    /// <summary>
    ///     Interface for Display Information. A Component or System that wants its data to be displayed must implement
    ///     this Information. Used by InformationDisplaySystem
    /// </summary>
    public interface IInformation
    {
        /// <summary>Returns the text to be displayed in the System</summary>
        /// <returns></returns>
        String[] getDisplayText();

        /// <summary>Returns the Color in which the text shall be displayed</summary>
        /// <returns></returns>
        Color getDisplayColor();

        /// <summary>Returns whether this Information shall be displayed</summary>
        /// <returns></returns>
        bool shallDraw();
    }
}