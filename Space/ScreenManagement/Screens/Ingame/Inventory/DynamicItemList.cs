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
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.ComponentSystem.RPG.Components;
using Space.Simulation.Commands;
using Space.Data;

namespace Space.ScreenManagement.Screens.Ingame.Hud
{
    class DynamicItemList : AbstractGuiElement, IItem
    {
        //private InventoryManagerTest _manager;
        private ItemSelectionManager _itemSelection;
        private TextureManager _textureManager;

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
        /// Constructor
        /// </summary>
        public DynamicItemList(GameClient client, ItemSelectionManager itemSelection, TextureManager textureManager)
            : base(client)
        {
            _textureManager = textureManager;
            _itemSelection = itemSelection;

            IconSize = 35;
            Margin = 2;
            ElementsEachRow = 5;
        }

        public override void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            base.LoadContent(spriteBatch, content);
            base.Enabled = true;
        }

        public override void Draw()
        {
            _spriteBatch.Begin();

            var inventar = _client.GetInventory();
            
            for (int i = 0; i < inventar.Count(); i++)
            {
                // draw the background that is visible if no icon is displayed
                _basicForms.FillRectangle(WestX(i), NorthY(i), IconSize, IconSize, Color.White * 0.2f);

                // load the image of the icon that is saved in this slot
                string imagePath = null;
                var invItem = inventar[i];
                if (invItem != null)
                {
                    var item = invItem.GetComponent < Item<AttributeType>>();
                    if (item != null)
                    {
                        imagePath = item.Texture();
                    }
                }
                var image = _textureManager.Get(imagePath);

                // draw the current item if a) an item is available for this slot and b) the item is currently not selected
                if (imagePath != null && !(_itemSelection.SelectedId == i && _itemSelection.SelectedClass == this))
                {
                    _spriteBatch.Draw(image, new Rectangle(WestX(i), NorthY(i), IconSize, IconSize), Color.White);
                }
            }

            for (int i = 0; i < inventar.Count(); i++)
            {
                // draw the tooltip
                if (IsMousePositionOnIcon(i) && inventar[i] != null)
                {
                    _basicForms.FillRectangle(WestX(i) + IconSize + 10, NorthY(i), 200, 100, Color.Black * 0.5f);

                    SpriteFont font = _content.Load<SpriteFont>("Fonts/ConsoleFont");
                    var line = 1;
                    _spriteBatch.DrawString(font, "Lalala", new Vector2(WestX(i) + IconSize + 20, NorthY(i) + line * 12), Color.White);
                    line++;
                    _spriteBatch.DrawString(font, "Lalala", new Vector2(WestX(i) + IconSize + 20, NorthY(i) + line * 12), Color.White);
                    line++;
                }
            }


            _spriteBatch.End();
        }

        public override bool DoHandleMousePressed(MouseButtons buttons)
        {
            // If no item is selected, select the item and enable the drag 'n drop mode...
            if (!_itemSelection.ItemIsSelected)
            {
                var inventar = _client.GetInventory();

                for (int i = 0; i < inventar.Count(); i++)
                {
                    if (IsMousePositionOnIcon(i))
                    {
                        string imagePath = null;
                        var invItem = inventar[i];
                        if (invItem != null)
                        {
                            var item = invItem.GetComponent < Item<AttributeType>>();
                            if (item != null)
                            {
                                imagePath = item.Texture();
                            }
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
            var inventar = _client.GetInventory();

            if (_itemSelection.DragNDropMode)
            {
                for (int i = 0; i < inventar.Count(); i++)
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
                            var invItem = inventar[i];

                            if (invItem != null) {
                                var item = invItem.GetComponent<Item<AttributeType>>();
                                if (item != null)
                                {
                                    imagePath = item.Texture();
                                }
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

            for (int i = 0; i < inventar.Count(); i++)
            {
                // if the mouse click is within the current item dimension
                if (IsMousePositionOnIcon(i))
                {
                    string imagePath = null;
                    var invItem = inventar[i];
                    if (invItem != null)
                    {
                        var item = invItem.GetComponent < Item<AttributeType>>();
                        if (item != null)
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

    }
}
