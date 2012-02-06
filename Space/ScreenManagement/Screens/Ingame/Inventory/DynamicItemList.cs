using System;
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

namespace Space.ScreenManagement.Screens.Ingame.Hud
{
    class DynamicItemList : AbstractGuiElement, IItem
    {
        InventoryManagerTest _manager;
        ItemSelectionManager _itemSelection;

        /// <summary>
        /// The size (height and width) of the icons
        /// </summary>
        public int IconSize { get; set; }

        /// <summary>
        /// The margin around the icons. (= gap between the icons)
        /// </summary>
        public int Margin { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public DynamicItemList(GameClient client, ItemSelectionManager itemSelection)
            : base(client)
        {
            _manager = new InventoryManagerTest(client);
            _itemSelection = itemSelection;

            IconSize = 50;
            Margin = 10;
        }

        public override void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            base.LoadContent(spriteBatch, content);
            base.Enabled = true;
        }

        public override void Draw()
        {
            _spriteBatch.Begin();

            for (int i = 0; i < 4; i++)
            {
                // draw the background that is visible if no icon is displayed
                _basicForms.FillRectangle((int)GetPosition().X + i * (IconSize + Margin), (int)GetPosition().Y, IconSize, IconSize, Color.White * 0.2f);

                // load the image of the icon that is saved in this slot
                var image = _manager.GetImage(i);

                // draw the current item if a) an item is available for this slot and b) the item is currently not selected
                if (image != null && !(_itemSelection.SelectedId == i && _itemSelection.SelectedClass == this))
                {
                    _spriteBatch.Draw(image, new Rectangle((int)GetPosition().X + i * (IconSize + Margin), (int)GetPosition().Y, IconSize, IconSize), Color.White);
                }
            }

            _spriteBatch.End();
        }

        public override bool DoHandleMousePressed(MouseButtons buttons)
        {
            // If no item is selected, select the item and enable the drag 'n drop mode...
            if (!_itemSelection.ItemIsSelected)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (IsMousePositionOnIcon(i))
                    {
                        var image = _manager.GetImage(i);
                        _itemSelection.DragNDropMode = true;

                        // ... set it selected.
                        if (image != null)
                        {
                            _itemSelection.SetSelection(this, i, image);
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
                for (int i = 0; i < 4; i++)
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
                            var image = _manager.GetImage(i);

                            // ... tell the manager to swap the items.
                            var previousId = _itemSelection.SelectedId;
                            _manager.SetImage(_itemSelection.SelectedIcon, i);
                            _manager.SetImage(image, previousId);
                            _itemSelection.RemoveSelection();
                            _itemSelection.DragNDropMode = false;
                            return true;
                        }
                    }
                }
            }

            for (int i = 0; i < 4; i++)
            {
                // if the mouse click is within the current item dimension
                if (IsMousePositionOnIcon(i))
                {
                    var image = _manager.GetImage(i);

                    // if an item is currently selected...
                    if (_itemSelection.ItemIsSelected)
                    {
                        // ... tell the manager to swap the items.
                        var previousId = _itemSelection.SelectedId;
                        _manager.SetImage(_itemSelection.SelectedIcon, i);
                        _manager.SetImage(image, previousId);
                        _itemSelection.RemoveSelection();
                    }

                    // if no item is selected...
                    else
                    {
                        // ... set it selected.
                        if (image != null)
                        {
                            _itemSelection.SetSelection(this, i, image);
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

        /// <summary>
        /// Returns the status if the mouse cursor is currently over the icon with a specific id.
        /// </summary>
        /// <param name="id">The id of the icon to check.</param>
        /// <returns>The status if the mouse cursor is currently over the icon with a specific id</returns>
        private bool IsMousePositionOnIcon(int id)
        {
            return Mouse.GetState().X >= GetPosition().X + id * (IconSize + Margin)
                && Mouse.GetState().X <= GetPosition().X + id * (IconSize + Margin) + IconSize
                && Mouse.GetState().Y >= GetPosition().Y
                && Mouse.GetState().Y <= GetPosition().Y + IconSize;
        }
    }
}
