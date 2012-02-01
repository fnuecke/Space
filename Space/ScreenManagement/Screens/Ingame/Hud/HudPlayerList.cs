using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.Control;
using Space.ScreenManagement.Screens.Elements.Hud.HudComponents;
using Space.ScreenManagement.Screens.Helper;
using Space.ScreenManagement.Screens.Ingame.Interfaces;

namespace Space.ScreenManagement.Screens.Elements.Hud
{
    /// <summary>
    /// A HUD element that displayes a list all current players and their health.
    /// </summary>
    class HudPlayerList : AbstractHudElement
    {

        #region Constants

        /// <summary>
        /// The standard value of the padding of the label to the outer border.
        /// </summary>
        private const int StandardPadding = 2;

        /// <summary>
        /// The standard value of the bottom border to the next element.
        /// </summary>
        private const int StandardBorderBottom = 2;

        #endregion

        #region Fields

        /// <summary>
        /// Helper class for drawing game specific forms.
        /// </summary>
        private SpaceForms _spaceForms;

        /// <summary>
        /// A label to display the name of a player.
        /// </summary>
        private HudSingleLabel _name;

        /// <summary>
        /// A label to display the current health of a player.
        /// </summary>
        private HudSingleLabel _health;

        /// <summary>
        /// The padding of the label to the outer border.
        /// </summary>
        private int _padding;

        /// <summary>
        /// The bottom border to the next element.
        /// </summary>
        private int _borderBottom;

        /// <summary>
        /// A list that should always hold the names of the current players.
        /// </summary>
        String[] _listNames;

        /// <summary>
        /// A list that should always hold the health of the current players.
        /// </summary>
        float[] _listHealth;

        #endregion

        #region Getter & Setter

        public override void SetPosition(Point position)
        {
            base.SetPosition(position);

            _name.SetPosition(new Point(GetPosition().X + _padding, GetPosition().Y + _padding));
            _health.SetPosition(new Point(_name.GetPosition().X + _name.GetWidth() + 2 * _padding + 2, _name.GetPosition().Y));
        }

        public override int GetHeight()
        {
            int height = 0;
            height += ((_name.GetHeight() + 2 * _padding + _borderBottom) * _listNames.Length);
            return height;
        }

        public override int GetWidth()
        {
            int width = 0;
            width += _name.GetWidth() + 2 * _padding;
            width += _borderBottom;
            width += _health.GetWidth() + 2 * _padding;
            return width;
        }

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
            _listNames = new String[0];
            _listHealth = new float[0];

            _name.LoadContent(spriteBatch, content);
            _health.LoadContent(spriteBatch, content);

            // set some standard settings for the elements
            _padding = StandardPadding;
            _borderBottom = StandardBorderBottom;

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

        #region Update & Drawing

        /// <summary>
        /// Updates the data of the HUD elements
        /// </summary>
        public void Update()
        {

            // Updates the data of player names and their health
            int numberOfPlayers = _client.Controller.Session.NumPlayers;
            _listNames = new String[numberOfPlayers];
            _listHealth = new float[numberOfPlayers];
            for (int i = 0; i < numberOfPlayers; i++)
            {
                _listNames[i] = _client.Controller.Session.GetPlayer(i).Name;
                _listHealth[i] = _client.GetPlayerShipInfo(i).Health / _client.GetPlayerShipInfo(i).MaxHealth;
            }

        }

        /// <summary>
        /// Render the HUD box with the current values.
        /// </summary>
        public override void Draw()
        {
            // save the original positions to be able to restore them later
            var originalNamePosition = _name.GetPosition();
            var originalHealthPosition = _health.GetPosition();
            var originalColorNorth = _health.ColorNorth;
            var originalColorSouth = _health.ColorSouth;

            var forY = GetPosition().Y;
            for (int i = 0; i < _listNames.Length; i++)
            {
                // set the name for the current loop
                _name.Text = _listNames[i];
                _health.Text = ((int) (_listHealth[i] * 100)) + "%";

                // color the health label yellow or red, if the health is low
                Color thisColorNorth = originalColorNorth;
                Color thisColorSouth = originalColorSouth;
                if (_listHealth[i] < 0.5f && _listHealth[i] >= 0.3f)
                {
                    thisColorNorth = Color.Lerp(originalColorNorth, HudColors.OrangeGradientLight, (1 - (_listHealth[i] - 0.3f) / 0.2f));
                    thisColorSouth = Color.Lerp(originalColorSouth, HudColors.OrangeGradientDark, (1 - (_listHealth[i] - 0.3f) / 0.2f));
                }
                else if (_listHealth[i] < 0.3f && _listHealth[i] >= 0.1f)
                {
                    thisColorNorth = Color.Lerp(HudColors.OrangeGradientLight, HudColors.RedGradientLight, 1 - (_listHealth[i] - 0.1f) / 0.2f);
                    thisColorSouth = Color.Lerp(HudColors.OrangeGradientDark, HudColors.RedGradientDark, 1 - (_listHealth[i] - 0.1f) / 0.2f);
                }
                else if (_listHealth[i] < 0.1f)
                {
                    thisColorNorth = HudColors.RedGradientLight;
                    thisColorSouth = HudColors.RedGradientDark;
                }

                _health.ColorNorth = thisColorNorth;
                _health.ColorSouth = thisColorSouth;

                _spriteBatch.Begin();
                _spaceForms.DrawRectangleWithoutEdges(
                    GetPosition().X,
                    forY,
                    _name.GetWidth() + 2 * _padding,
                    _name.GetHeight() + _padding * 2,
                    4, _padding, HudColors.Lines);
                _spaceForms.DrawRectangleWithoutEdges(
                    GetPosition().X + _name.GetWidth() + 2 * _padding + 2,
                    forY,
                    _health.GetWidth() + 2 * _padding,
                    _health.GetHeight() + _padding * 2,
                    4, _padding, HudColors.Lines);
                _spriteBatch.End();

                _name.Draw();
                _health.Draw();

                forY += _name.GetHeight() + 2 * _padding + _borderBottom;
                _name.SetPosition(new Point(_name.GetPosition().X, _name.GetPosition().Y + _name.GetHeight() + 2 * _padding + _borderBottom));
                _health.SetPosition(new Point(_health.GetPosition().X, _health.GetPosition().Y + _health.GetHeight() + 2 * _padding + _borderBottom));
            }

            // restore the original positions
            _name.SetPosition(originalNamePosition);
            _health.SetPosition(originalHealthPosition);
            _health.ColorNorth = originalColorNorth;
            _health.ColorSouth = originalColorSouth;
        }

        #endregion
    }
}
