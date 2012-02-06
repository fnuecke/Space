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

        public DynamicItemList(GameClient client, ItemSelectionManager itemSelection)
            : base(client)
        {
            _manager = new InventoryManagerTest(client);
            _itemSelection = itemSelection;
        }

        public override void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            Console.WriteLine("DynamicItemList.LoadContent()");
            base.LoadContent(spriteBatch, content);
            base.Enabled = true;
        }

        public override void Draw()
        {
            _spriteBatch.Begin();
            for (int i = 0; i < 4; i++)
            {
                _basicForms.FillRectangle((int)GetPosition().X + i * 52, (int)GetPosition().Y, 50, 50, Color.White * 0.2f);
                var image = _manager.GetImage(i);
                if (image != null && !(_itemSelection.SelectedId == i && _itemSelection.SelectedClass == this))
                {
                    _spriteBatch.Draw(image, new Rectangle((int)GetPosition().X + i * 52, (int)GetPosition().Y, 50, 50), Color.White);
                }
            }
            _spriteBatch.End();
        }

        public override bool DoHandleMousePressed(MouseButtons buttons)
        {
            Console.WriteLine(GetPosition().X + " x " + GetPosition().Y);

            return false;
        }

        public override bool DoHandleMouseReleased(MouseButtons buttons)
        {
            for (int i = 0; i < 4; i++)
            {
                // if the mouse click is within the item dimension
                if (Mouse.GetState().X >= GetPosition().X + i * 52 && Mouse.GetState().X <= GetPosition().X + i * 52 + 50 && Mouse.GetState().Y >= GetPosition().Y && Mouse.GetState().Y <= GetPosition().Y + 50)
                {
                    var image = _manager.GetImage(i);

                    if (_itemSelection.ItemIsSelected)
                    {
                        var previousId = _itemSelection.SelectedId;
                        _manager.SetImage(_itemSelection.SelectedIcon, i);
                        _manager.SetImage(image, previousId);
                        _itemSelection.RemoveSelection();
                    }
                    else
                    {
                        if (image != null)
                        {
                            _itemSelection.SetSelection(this, i, image);
                        }
                    }
                }
            }

            return true;
        }
    }
}
