using Microsoft.Xna.Framework;
using XUI;
using XUI.UI;

namespace Space.XUI
{
    public class IngameMain : Screen
    {
        public IngameMain()
            : base()
        {
            LoadTextures(_UI.Texture.CreateBundle());

            // Create a graphic in the center of the screen that is the size of
            // the safe area.
            WidgetGraphic g = new WidgetGraphic();
            g.Position = new Vector3(_UI.SXM - 50, _UI.SYM, 0.0f);
            g.Size = new Vector3(20, 40, 0.0f);
            g.ColorBase = Color.Red;
            g.Align = E_Align.MiddleCentre;
            g.AddTexture("ship", 0.0f, 0.0f, 1.0f, 1.0f);

            // Add the graphic to the screen.
            Add(g);
        }

        /// <summary>
        /// Load all textures used anywhere in the GUI here.
        /// </summary>
        /// <param name="bundleIndex">The bundle index to store textures in.</param>
        private void LoadTextures(int bundleIndex)
        {
            // Load *all* textures used in the GUI here.
            // e.g.: _UI.Texture.Add(bundleIndex, "Textures\\UI_Box", "box");

            _UI.Texture.Add(bundleIndex, "Textures/ship", "ship");
        }
    }
}
