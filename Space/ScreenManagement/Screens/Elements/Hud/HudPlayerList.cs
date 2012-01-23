using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Space.Control;
using Space.ScreenManagement.Screens.Interfaces;
using Space.ScreenManagement.Screens.Elements.Hud.HudComponents;
using Space.ScreenManagement.Screens.Helper;

namespace Space.ScreenManagement.Screens.Elements.Hud
{
    /// <summary>
    /// A sample file that can be used as a kind of template to create a new
    /// object for a new HUD element.
    /// 
    /// It is NOT intended to be used in the hud!
    /// </summary>
    class HudPlayerList : AHudElement
    {

        #region Fields

        /// <summary>
        /// Helper class for drawing game specific forms.
        /// </summary>
        private SpaceForms _spaceForms;

        private HudSingleLabel _name;
        private HudSingleLabel _health;

        private int _gapAround;
        private int _gap;

        #endregion

        #region Initialisation

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">The general client object.</param>
        public HudPlayerList(GameClient client)
            : base(client)
        {
            _name = new HudSingleLabel(client);
            _health = new HudSingleLabel(client);
        }

        public override void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            base.LoadContent(spriteBatch, content);

            _spaceForms = new SpaceForms(_spriteBatch);

            _name.LoadContent(spriteBatch, content);
            _health.LoadContent(spriteBatch, content);

            _gapAround = 2;
            _gap = 2;

            // set some standard settings for the elements
            _name.Text = "DaKaTotal";
            _name.SetSize(new Point(128, 16));
            _name.ColorNorth = HudColors.GreenDarkGradientLight;
            _name.ColorSouth = HudColors.GreenDarkGradientDark;
            _name.ColorText = HudColors.FontDark;
            _name.TextAlign = HudSingleLabel.Alignments.Center;

            _health.Text = "100%";
            _health.SetSize(new Point(52, 16));
            _health.ColorNorth = HudColors.BlueGradientLight;
            _health.ColorSouth = HudColors.BlueGradientDark;
            _health.ColorText = HudColors.FontLight;
            _health.TextAlign = HudSingleLabel.Alignments.Center;
        }

        #endregion

        #region Getter & Setter

        public override void SetPosition(Point position)
        {
            base.SetPosition(position);

            _name.SetPosition(new Point(GetPosition().X + _gapAround, GetPosition().Y + _gapAround));
            _health.SetPosition(new Point(_name.GetPosition().X + _name.GetWidth() + 2 * _gapAround + 2, _name.GetPosition().Y));
        }

        public override int GetHeight()
        {
            int height = 0;
            height += _name.GetHeight() + 2 * _gapAround + _gap;
            return height;
        }

        public override int GetWidth()
        {
            int width = 0;
            width += _name.GetWidth() + 2 * _gapAround;
            width += _gap;
            width += _health.GetWidth() + 2 * _gapAround;
            return width;
        }

        #endregion

        #region Draw

        /// <summary>
        /// Render the HUD box with the current values.
        /// </summary>
        public override void Draw()
        {
            // save the original positions to be able to restore them later
            var originalNamePosition = _name.GetPosition();
            var originalHealthPosition = _health.GetPosition();

            var info = _client.GetPlayerShipInfo();
            if (info == null)
            {
                return;
            }

            var names = new[] { "DaKaTotal", "Sangar", "lordjoda" };
            var health = new[] { (int) ((info.Health * 1.0 / info.MaxHealth) * 100), 95, 77 };

            var forY = GetPosition().Y;
            for (int i = 0; i < names.Length; i++)
            {
                // set the name for the current loop
                _name.Text = names[i];
                _health.Text = health[i] + "%";

                _spriteBatch.Begin();
                _spaceForms.DrawRectangleWithoutEdges(
                    GetPosition().X,
                    forY,
                    _name.GetWidth() + 2 * _gapAround,
                    _name.GetHeight() + _gapAround * 2,
                    4, _gapAround, HudColors.Lines);
                _spaceForms.DrawRectangleWithoutEdges(
                    GetPosition().X + _name.GetWidth() + 2 * _gapAround + 2,
                    forY,
                    _health.GetWidth() + 2 * _gapAround,
                    _health.GetHeight() + _gapAround * 2,
                    4, _gapAround, HudColors.Lines);
                _spriteBatch.End();

                _name.Draw();
                _health.Draw();

                forY += _name.GetHeight() + 2 * _gapAround + _gap;
                _name.SetPosition(new Point(_name.GetPosition().X, _name.GetPosition().Y + _name.GetHeight() + 2 * _gapAround + _gap));
                _health.SetPosition(new Point(_health.GetPosition().X, _health.GetPosition().Y + _health.GetHeight() + 2 * _gapAround + _gap));
            }

            // restore the original positions
            _name.SetPosition(originalNamePosition);
            _health.SetPosition(originalHealthPosition);
        }

        #endregion
    }
}
