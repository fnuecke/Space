﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Space.Control;
using Space.ScreenManagement.Screens.Ingame.Interfaces;
using Nuclex.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Space.ScreenManagement.Screens.Helper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Space.ScreenManagement.Screens.Ingame.GuiElementManager;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.ComponentSystem.RPG.Components;
using Space.Simulation.Commands;
using Space.Data;
using Space.ComponentSystem.Components;

namespace Space.ScreenManagement.Screens.Ingame.Hud
{

    /// <summary>
    /// A class that displayed a specific number of item slots.
    /// 
    /// By setting new values for the properties the design can be changed.
    /// Several input handlers and mouse over effects makes it possible to
    /// move items to different the slots. By using the basic item selection
    /// manager its also possible to move them to other DynamicItemList objects.
    /// </summary>
    class DynamicItemList : AbstractGuiElement, IItem
    {

        #region Constants

        /// <summary>
        /// Enumeration that holds the possible modes. Necessary to get the correct data.
        /// </summary>
        public enum Modes {
            Inventory
        }

        #endregion
        
        #region Fields

        /// <summary>
        /// The basic item selection manager.
        /// </summary>
        private ItemSelectionManager _itemSelection;

        /// <summary>
        /// The basic texture manager.
        /// </summary>
        private TextureManager _textureManager;

        #endregion

        #region Properties

        /// <summary>
        /// The size (height and width) of the icons
        /// </summary>
        public int IconSize { get; set; }

        /// <summary>
        /// The margin around the icons. (= gap between the icons)
        /// </summary>
        public int Margin { get; set; }

        /// <summary>
        /// The number of icons that are displayed each row.
        /// </summary>
        public int ElementsEachRow { get; set; }

        /// <summary>
        /// The number of icons that are displayed each row.
        /// </summary>
        public Modes Mode { get; set; }

        #endregion

        #region Initialisation

        /// <summary>
        /// Constructor
        /// </summary>
        public DynamicItemList(GameClient client, ItemSelectionManager itemSelection, TextureManager textureManager, Modes mode)
            : base(client)
        {
            _textureManager = textureManager;
            _itemSelection = itemSelection;
            Mode = mode;

            IconSize = 35;
            Margin = 2;
            ElementsEachRow = 5;
        }

        public override void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            base.LoadContent(spriteBatch, content);
            base.Enabled = true;
        }

        #endregion

        #region Draw

        public override void Draw()
        {
            _spriteBatch.Begin();

            for (int i = 0; i < DataCount(); i++)
            {
                // draw the background that is visible if no icon is displayed
                _basicForms.FillRectangle(WestX(i), NorthY(i), IconSize, IconSize, Color.White * 0.2f);

                // load the image of the icon that is saved in this slot
                string imagePath = null;
                var item = ItemAt(i);
                if (item != null)
                {
                    imagePath = item.Texture();
                }
                var image = _textureManager.Get(imagePath);

                // draw the current item if a) an item is available for this slot and b) the item is currently not selected
                if (imagePath != null && !(_itemSelection.SelectedId == i && _itemSelection.SelectedClass == this))
                {
                    _spriteBatch.Draw(image, new Rectangle(WestX(i), NorthY(i), IconSize, IconSize), Color.White);
                }
            }

            for (int i = 0; i < DataCount(); i++)
            {
                // draw the tooltip
                if (IsMousePositionOnIcon(i) && ItemAt(i) != null)
                {
                    _basicForms.FillRectangle(WestX(i) + IconSize + 10, NorthY(i), 200, 100, Color.Black * 0.5f);

                    SpriteFont font = _content.Load<SpriteFont>("Fonts/ConsoleFont");
                    var line = 1;
                    var item = ItemAt(i);
                    if (item != null)
                    {
                        _spriteBatch.DrawString(font, item.Name(), new Vector2(WestX(i) + IconSize + 20, NorthY(i) + line * 12), Color.White);
                        line++;
                        var attributes = item.Attributes();
                        foreach (var attribute in attributes)
                        {
                            _spriteBatch.DrawString(font, attribute.Modifier.Type+" "+attribute.Modifier.Value, new Vector2(WestX(i) + IconSize + 20, NorthY(i) + line * 12), Color.White);
                            line++;
                        }
                    }
                    
                }
            }


            _spriteBatch.End();
        }

        #endregion

        #region Listener

        public override bool DoHandleMousePressed(MouseButtons buttons)
        {
            // If no item is selected, select the item and enable the drag 'n drop mode...
            if (!_itemSelection.ItemIsSelected)
            {
                for (int i = 0; i < DataCount(); i++)
                {
                    if (IsMousePositionOnIcon(i))
                    {
                        string imagePath = null;
                        var item = ItemAt(i);
                        if (item != null)
                        {
                            imagePath = item.Texture();
                        } 
                        _itemSelection.DragNDropMode = true;

                        // ... set it selected.
                        if (imagePath != null)
                        {
                            _itemSelection.SetSelection(this, i, imagePath);
                        }
                    }
                }
            }
            // otherwise disable the Drag 'n Drop Mode
            else
            {
                _itemSelection.DragNDropMode = false;
            }
            return false;
        }

        public override bool DoHandleMouseReleased(MouseButtons buttons)
        {
            if (_itemSelection.DragNDropMode)
            {
                for (int i = 0; i < DataCount(); i++)
                {
                    if (IsMousePositionOnIcon(i))
                    {
                        // if it is the same slot set the variable sameItem to true to signal
                        // that the rest of the method should be run
                        if (i == _itemSelection.SelectedId)
                        {
                            _itemSelection.RemoveSelection();
                            _itemSelection.DragNDropMode = false;
                            break;
                        }
                        // if it is _not_ the same icon set the variable sameItem to false. The
                        // rest of the method should not be run.
                        else
                        {
                            string imagePath = null;
                            var item = ItemAt(i);
                            if (item != null)
                            {
                                imagePath = item.Texture();
                            }
                            
                            // ... tell the manager to swap the items.
                            var previousId = _itemSelection.SelectedId;
                            if (previousId != -1) {
                                _client.Controller.PushLocalCommand(new MoveItemCommand(i,previousId));
                                _client.Save();
                                _itemSelection.RemoveSelection();
                                _itemSelection.DragNDropMode = false;
                            }
                            return true;
                        }
                    }
                }
            }

            for (int i = 0; i < DataCount(); i++)
            {
                // if the mouse click is within the current item dimension
                if (IsMousePositionOnIcon(i))
                {
                    string imagePath = null;
                    var item = ItemAt(i);
                    if (item != null)
                    {
                        imagePath = item.Texture();
                    }
                    
                    // if an item is currently selected...
                    if (_itemSelection.ItemIsSelected)
                    {
                        // ... tell the manager to swap the items.
                        var previousId = _itemSelection.SelectedId;
                        _client.Controller.PushLocalCommand(new MoveItemCommand(i, previousId));
                        _client.Save();
                        _itemSelection.RemoveSelection();
                    }

                    // if no item is selected...
                    else
                    {
                        // ... set it selected.
                        if (imagePath != null)
                        {
                            _itemSelection.SetSelection(this, i, imagePath);
                        }
                        else
                        {
                            _itemSelection.RemoveSelection();
                        }
                    }
                    break;
                }

            }

            return true;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns the status if the mouse cursor is currently over the icon with a specific id.
        /// </summary>
        /// <param name="id">The id of the icon to check.</param>
        /// <returns>The status if the mouse cursor is currently over the icon with a specific id</returns>
        private bool IsMousePositionOnIcon(int id)
        {
            return Mouse.GetState().X >= WestX(id)
                && Mouse.GetState().X <= EastX(id)
                && Mouse.GetState().Y >= NorthY(id)
                && Mouse.GetState().Y <= SouthY(id);
        }

        /// <summary>
        /// Calculates the western X position of any icon which is determine by the slot id.
        /// </summary>
        /// <param name="id">The id of the slot.</param>
        /// <returns>The western X position of the slot.</returns>
        private int WestX(int id)
        {
            return (int)GetPosition().X + (id % ElementsEachRow) * (IconSize + Margin);
        }

        /// <summary>
        /// Calculates the northern Y position of any icon which is determine by the slot id.
        /// </summary>
        /// <param name="id">The id of the slot.</param>
        /// <returns>The northern Y position of the slot.</returns>
        private int NorthY(int id)
        {
            return (int)GetPosition().Y + (id / ElementsEachRow) * (IconSize + Margin);
        }

        /// <summary>
        /// Calculates the eastern X position of any icon which is determine by the slot id.
        /// </summary>
        /// <param name="id">The id of the slot.</param>
        /// <returns>The eastern X position of the slot.</returns>
        private int EastX(int id)
        {
            return WestX(id) + IconSize;
        }

        /// <summary>
        /// Calculates the southern Y position of any icon which is determine by the slot id.
        /// </summary>
        /// <param name="id">The id of the slot.</param>
        /// <returns>The southern Y position of the slot.</returns>
        private int SouthY(int id)
        {
            return NorthY(id) + IconSize;
        }

        /// <summary>
        /// Returns the number of slots that are available.
        /// </summary>
        /// <returns>The number of slots that are available.</returns>
        private int DataCount()
        {
            switch (Mode)
            {
                case Modes.Inventory:
                    return _client.GetInventory().Count();
            }
            return 0;
        }

        /// <summary>
        /// Returns the item at a specified position.
        /// </summary>
        /// <param name="id">The id of the slot of the item.</param>
        /// <returns>The item.</returns>
        private Item<AttributeType> ItemAt(int id)
        {
            switch (Mode)
            {
                case Modes.Inventory:
                    if (_client.GetInventory()[id] == null) {
                        return null;
                    }
                    return _client.GetInventory()[id].GetComponent<Item<AttributeType>>();
                default:
                    return null;
            }
        }

        #endregion

    }
}
