using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework.Graphics;
using Engine.ComponentSystem.Common.Messages;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Util;
using Space.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// This System is used to easily display text. A System that wants to be displayed must Implement the IInformation interface.
    /// </summary>
    public class InformationDisplaySystem : AbstractSystem, IDrawingSystem, IMessagingSystem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion
        #region Fields

        /// <summary>
        /// Wether this System shall be called for rendering or not
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// The sprite batch to render textures into.
        /// </summary>
        protected SpriteBatch SpriteBatch;

        /// <summary>
        /// The Font to render with
        /// </summary>
        protected SpriteFont Font;

        /// <summary>
        /// A List of all IInformations to display. Currently not working
        /// </summary>
        protected List<IInformation> Informations = new List<IInformation>();//not used maybe some different aproach?
        #endregion


        #region Logic

        /// <summary>
        /// Adds an IInformation to the list
        /// </summary>
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

        /// <summary>
        /// Removes an IInformation from the List of displayed Informations
        /// </summary>
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

        /// <summary>
        /// Handle a message of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            {
                var cm = message as GraphicsDeviceCreated?;
                if (cm != null)
                {
                    LoadContent(cm.Value.Content, cm.Value.Graphics);
                }
            }
            {
                var cm = message as GraphicsDeviceDisposing?;
                if (cm != null)
                {
                    UnloadContent();
                }
            }
            {
                var cm = message as GraphicsDeviceReset?;
                if (cm != null)
                {
                    UnloadContent();
                    LoadContent(cm.Value.Content, cm.Value.Graphics);
                }
            }
        }

        /// <summary>
        /// Called when the graphics device has been (re)created, and assets
        /// should be loaded.
        /// </summary>
        /// <param name="content">The content manager.</param>
        /// <param name="graphics">The graphics device service.</param>
        protected virtual void LoadContent(ContentManager content, IGraphicsDeviceService graphics)
        {
            SpriteBatch = new SpriteBatch(graphics.GraphicsDevice);
            Font = content.Load<SpriteFont>("Fonts/visitor");

        }

        /// <summary>
        /// Called when the graphics device is being disposed, and
        /// any assets manually allocated should be disposed.
        /// </summary>
        protected virtual void UnloadContent()
        {
            if (SpriteBatch != null)
            {
                SpriteBatch.Dispose();
                SpriteBatch = null;

            }
        }
        /// <summary>
        /// Draw all Informations 
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="elapsedMilliseconds"></param>
        public void Draw(long frame, float elapsedMilliseconds)
        {

            var newList = new List<IInformation>();
            var localEntity = ((LocalPlayerSystem)Manager.GetSystem(LocalPlayerSystem.TypeId)).LocalPlayerAvatar;
            if (localEntity > 0)
            { 
                var shipInfo = (ShipInfo)Manager.GetComponent(localEntity, ShipInfo.TypeId);
                if (shipInfo != null)
                {
                    newList.Add(shipInfo);
                }
            }


            SpriteBatch.Begin();
            int rowNumber = 0;
            foreach (var info in newList)
            {
                if (!info.shallDraw())//check if we shall draw this information
                {
                    continue;
                }

                var output = info.getDisplayText();//get text
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
