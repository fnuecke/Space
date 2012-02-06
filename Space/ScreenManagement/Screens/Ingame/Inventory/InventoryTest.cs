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
    class InventoryTest : AbstractGuiElement
    {
        InventoryManagerTest _manager;
        ItemSelectionManager _itemSelection;

        DynamicItemList _list;

        public InventoryTest(GameClient client, ItemSelectionManager itemSelection)
            : base(client)
        {
            _manager = new InventoryManagerTest(client);
            _itemSelection = itemSelection;

            _list = new DynamicItemList(client, itemSelection);
        }

        public override void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            base.LoadContent(spriteBatch, content);
            base.Enabled = true;

            _list.LoadContent(spriteBatch, content);
        }

        public override void Draw()
        {
            _spriteBatch.Begin();
            _basicForms.FillRectangle((int)GetPosition().X, (int)GetPosition().Y, (int)GetWidth(), (int)GetHeight(), Color.Black * 0.6f);
            _spriteBatch.End();
            _list.Draw();
        }

        public override void SetPosition(float x, float y)
        {
            Console.WriteLine(">>> SetPosition <<<");
            base.SetPosition(x, y);
            _list.SetPosition(x, y);
        }
        
        public override bool DoHandleMousePressed(MouseButtons buttons)
        {
            if (!(Mouse.GetState().X >= GetPosition().X && Mouse.GetState().X <= GetPosition().X + GetWidth() && Mouse.GetState().Y >= GetPosition().Y && Mouse.GetState().Y <= GetPosition().Y + GetHeight()))
            {
                return false;
            }
            _list.DoHandleMousePressed(buttons);
            return true;
        }

        public override bool DoHandleMouseReleased(MouseButtons buttons)
        {
            if (!(Mouse.GetState().X >= GetPosition().X && Mouse.GetState().X <= GetPosition().X + GetWidth() && Mouse.GetState().Y >= GetPosition().Y && Mouse.GetState().Y <= GetPosition().Y + GetHeight()))
            {
                return false;
            }
            _list.DoHandleMouseReleased(buttons);
            return true;
        }
    }
}
