using System.Collections.Generic;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Microsoft.Xna.Framework.Graphics;
using Engine.ComponentSystem.Common.Messages;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Util;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    ///     This System is used to easily display text. A System that wants to be displayed must Implement the
    ///     IInformation interface.
    /// </summary>
    [Packetizable(false)]
    public sealed class InformationDisplaySystem : AbstractSystem, IDrawingSystem
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Fields

        /// <summary>Whether this System shall be called for rendering or not</summary>
        public bool Enabled { get; set; }

        /// <summary>The sprite batch to render textures into.</summary>
        private SpriteBatch SpriteBatch;

        /// <summary>The Font to render with</summary>
        private SpriteFont Font;

        /// <summary>A List of all IInformations to display. Currently not working</summary>
        private List<IInformation> Informations = new List<IInformation>(); //not used maybe some different aproach?

        #endregion

        #region Logic

        /// <summary>Adds an IInformation to the list</summary>
        /// <param name="info"></param>
        public void addInformation(IInformation info)
        {
            if (Informations.Contains(info))
            {
                //throw exception?
                return;
            }
            Informations.Add(info);
        }

        /// <summary>Removes an IInformation from the List of displayed Informations</summary>
        /// <param name="info"></param>
        public void removeInformation(IInformation info)
        {
            if (Informations.Contains(info))
            {
                Informations.Remove(info);
                return;
            }
            //Throw exception?
        }
        
        [MessageCallback]
        public void OnGraphicsDeviceCreated(GraphicsDeviceCreated message)
        {
            LoadContent(((ContentSystem) Manager.GetSystem(ContentSystem.TypeId)).Content, message.Graphics);
        }

        [MessageCallback]
        public void OnGraphicsDeviceDisposing(GraphicsDeviceDisposing message)
        {
            UnloadContent();
        }

        [MessageCallback]
        public void OnGraphicsDeviceReset(GraphicsDeviceReset message)
        {
            UnloadContent();
            LoadContent(((ContentSystem) Manager.GetSystem(ContentSystem.TypeId)).Content, message.Graphics);
        }

        /// <summary>Called when the graphics device has been (re)created, and assets should be loaded.</summary>
        /// <param name="content">The content manager.</param>
        /// <param name="graphics">The graphics device service.</param>
        private void LoadContent(ContentManager content, IGraphicsDeviceService graphics)
        {
            SpriteBatch = new SpriteBatch(graphics.GraphicsDevice);
            Font = content.Load<SpriteFont>("Fonts/visitor");
        }

        /// <summary>Called when the graphics device is being disposed, and any assets manually allocated should be disposed.</summary>
        private void UnloadContent()
        {
            if (SpriteBatch != null)
            {
                SpriteBatch.Dispose();
                SpriteBatch = null;
            }
        }

        /// <summary>Draw all Informations</summary>
        /// <param name="frame"></param>
        /// <param name="elapsedMilliseconds"></param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            var newList = new List<IInformation>();
            var localEntity = ((LocalPlayerSystem) Manager.GetSystem(LocalPlayerSystem.TypeId)).LocalPlayerAvatar;
            if (localEntity > 0)
            {
                var shipInfo = (ShipInfo) Manager.GetComponent(localEntity, ShipInfo.TypeId);
                if (shipInfo != null)
                {
                    newList.Add(shipInfo);
                }
            }

            SpriteBatch.Begin();
            int rowNumber = 0;
            foreach (var info in newList)
            {
                if (!info.shallDraw()) //check if we shall draw this information
                {
                    continue;
                }

                var output = info.getDisplayText(); //get text
                foreach (var text in output)
                {
                    var position = new Vector2(100, 20 + 20 * rowNumber++);
                    Vector2 FontOrigin = Font.MeasureString(text) / 2;
                    SpriteBatch.DrawString(Font, text, position, info.getDisplayColor());
                }
            }
            SpriteBatch.End();
        }

        #endregion
    }
}