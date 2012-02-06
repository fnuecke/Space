using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Engine.ComponentSystem.RPG.Components;

namespace Space.ScreenManagement.Screens.Ingame.GuiElementManager
{

    /// <summary>
    /// A class that should help managing selected items and to handle inputs
    /// from the user even if the icons are located in different GUI elements.
    /// 
    /// Example:
    /// If you want to move an item from the inventory into the hotkey bar
    /// this class always holds the status which item is selected and all the
    /// necessary data.
    /// </summary>
    class ItemSelectionManager
    {

        #region Properties

        /// <summary>
        /// Holds the object of the class that holds the current selected item.
        /// </summary>
        public IItem SelectedClass { get; private set; }

        /// <summary>
        /// Holds the ID of the current selected item.
        /// </summary>
        public int SelectedId { get; private set; }

        /// <summary>
        /// Holds the status if an item is currently selected
        /// </summary>
        public bool ItemIsSelected { get; private set; }

        /// <summary>
        /// Holds the status if drag 'n drop mode is currently active.
        /// </summary>
        public bool DragNDropMode { get; set; }

        /// <summary>
        /// Holds the icon object of the displayed icon.
        /// </summary>
        public Texture2D SelectedIcon { get; private set; }

        #endregion

        #region Initialisation

        /// <summary>
        /// Constructor
        /// </summary>
        public ItemSelectionManager()
        {
            RemoveSelection();
            DragNDropMode = false;
        }

        #endregion

        #region Getter / Setter

        /// <summary>
        /// Remove the current selection and set all properties to the default value.
        /// </summary>
        public void RemoveSelection()
        {
            SelectedClass = null;
            SelectedId = -1;
            SelectedIcon = null;
            ItemIsSelected = false;
        }

        /// <summary>
        /// Set a new selection and set all properties.
        /// </summary>
        /// <param name="SelectedClass">The object of the class that holds the selected item.</param>
        /// <param name="SelectedId">The ID of the selected item.</param>
        /// <param name="SelectedItem">The icon object of the displayed icon.</param>
        public void SetSelection(IItem SelectedClass, int SelectedId, Texture2D SelectedItem)
        {
            this.SelectedClass = SelectedClass;
            this.SelectedId = SelectedId;
            this.SelectedIcon = SelectedItem;
            this.ItemIsSelected = true;
        }

        #endregion

    }

}
