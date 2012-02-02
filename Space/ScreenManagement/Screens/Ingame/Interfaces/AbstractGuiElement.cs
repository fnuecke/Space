using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Space.Control;
using Nuclex.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Space.ScreenManagement.Screens.Ingame.Interfaces
{

    /// <summary>
    /// An abstract class to guarantee that all GUI elements have the
    /// necessary methods and data.
    /// </summary>
    public abstract class AbstractGuiElement
    {

        #region Fields

        /// <summary>
        /// The local client, used to fetch player's position and radar range.
        /// </summary>
        protected readonly GameClient _client;

        /// <summary>
        /// Holds all GUI elements that are child elements of this element.
        /// </summary>
        private List<AbstractGuiElement> _childElements;

        /// <summary>
        /// Holds the x- and y-position of the GUI element.
        /// </summary>
        private Vector2 _position;

        /// <summary>
        /// The width of the element.
        /// </summary>
        private float _width;

        /// <summary>
        /// The height of the element.
        /// </summary>
        private float _height;

        #endregion

        #region Properties

        /// <summary>
        /// Status whether the GUI element is enabled or not.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Status whether the GUI element is displayed or not.
        /// </summary>
        public bool Visible { get; set; }

        #endregion

        #region Initialisation

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">The local client.</param>
        public AbstractGuiElement(GameClient client)
        {
            _client = client;
            _childElements = new List<AbstractGuiElement>();
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public virtual void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            // set some standard settings for the elements
            _position = new Vector2(0, 0);
            _width = 0;
            _height = 0;
            Enabled = false;
            Visible = false;
        }

        #endregion

        #region Getter / Setter

        /// <summary>
        /// Get the x- and y-position of the current element.
        /// </summary>
        /// <returns>The x- and y-position of the current element</returns>
        public virtual Vector2 GetPosition()
        {
            return _position;
        }

        /// <summary>
        /// Set the x- and y-position of the current element.
        /// </summary>
        /// <param name="position">The x- and y-position of the current element</returns>
        public virtual void SetPosition(Vector2 position)
        {
            _position = position;
        }

        /// <summary>
        /// Set the x- and y-position of the current element.
        /// </summary>
        /// <param name="x">The x-position of the current element</returns>
        /// <param name="y">The y-position of the current element</returns>
        public void SetPosition(float x, float y)
        {
            SetPosition(new Vector2(x, y));
        }

        /// <summary>
        /// Get the width of the current element.
        /// </summary>
        /// <returns>The width of the current element</returns>
        public virtual float GetWidth()
        {
            return _width;
        }

        /// <summary>
        /// Set the width of the current element.
        /// </summary>
        /// <param name="width">The width of the current element</returns>
        public virtual void SetWidth(float width)
        {
            _width = width;
        }

        /// <summary>
        /// Get the height of the current element.
        /// </summary>
        /// <returns>The height of the current element</returns>
        public virtual float GetHeight()
        {
            return _height;
        }

        /// <summary>
        /// Set the height of the current element.
        /// </summary>
        /// <param name="height">The height of the current element</returns>
        public virtual void SetHeight(float height)
        {
            _height = height;
        }

        #endregion

        #region Update & Draw

        /// <summary>
        /// Updates the data of the elements.
        /// </summary>
        public virtual void Update()
        {
        }

        /// <summary>
        /// Draws the element.
        /// </summary>
        public abstract void Draw();

        #endregion

        #region Listener Basics (not overrideable)

        /// <summary>
        /// Handles the key pressed event.
        /// Returns false if the GUI element does not allow input handling.
        /// 
        /// Cannot be overwritten. For handling specific user inputs please 
        /// overwrite the method DoHandleKeyPressed.
        /// </summary>
        /// <returns>True if the element is allowed to handle inputs, False else.</returns>
        public bool HandleKeyPressed(Keys key)
        {
            // return false if this element is not allowed to handling inputs
            if (!IsInputHandlingAllowed())
            {
                return false;
            }

            DoHandleKeyPressed(key);

            // iterate all child elements and return true afterwards
            foreach (AbstractGuiElement e in _childElements)
            {
                e.HandleKeyPressed(key);
            }
            return true;
        }

        /// <summary>
        /// Handles the key released event.
        /// Returns false if the GUI element does not allow input handling.
        /// 
        /// Cannot be overwritten. For handling specific user inputs please 
        /// overwrite the method DoHandleKeyReleased.
        /// </summary>
        /// <returns>True if the element is allowed to handle inputs, False else.</returns>
        public bool HandleKeyReleased(Keys key)
        {
            // return false if this element is not allowed to handling inputs
            if (!IsInputHandlingAllowed())
            {
                return false;
            }

            DoHandleKeyReleased(key);

            // iterate all child elements and return true afterwards
            foreach (AbstractGuiElement e in _childElements)
            {
                e.HandleKeyReleased(key);
            }
            return true;
        }

        /// <summary>
        /// Handles the mouse pressed event.
        /// Returns false if the GUI element does not allow input handling.
        /// 
        /// Cannot be overwritten. For handling specific user inputs please 
        /// overwrite the method DoHandleMousePressed.
        /// </summary>
        /// <returns>True if the element is allowed to handle inputs, False else.</returns>
        public bool HandleMousePressed(MouseButtons buttons)
        {
            // return false if this element is not allowed to handling inputs
            if (!IsInputHandlingAllowed())
            {
                return false;
            }

            DoHandleMousePressed(buttons);

            // iterate all child elements and return true afterwards
            foreach (AbstractGuiElement e in _childElements)
            {
                e.HandleMousePressed(buttons);
            }
            return true;
        }

        /// <summary>
        /// Handles the mouse released event.
        /// Returns false if the GUI element does not allow input handling.
        /// 
        /// Cannot be overwritten. For handling specific user inputs please 
        /// overwrite the method DoHandleMouseReleased.
        /// </summary>
        /// <returns>True if the element is allowed to handle inputs, False else.</returns>
        public bool HandleMouseReleased(MouseButtons buttons)
        {
            // return false if this element is not allowed to handling inputs
            if (!IsInputHandlingAllowed())
            {
                return false;
            }

            DoHandleMouseReleased(buttons);

            // iterate all child elements and return true afterwards
            foreach (AbstractGuiElement e in _childElements)
            {
                e.HandleMouseReleased(buttons);
            }
            return true;
        }

        /// <summary>
        /// Handles the mouse moved event.
        /// Returns false if the GUI element does not allow input handling.
        /// 
        /// Cannot be overwritten. For handling specific user inputs please 
        /// overwrite the method DoHandleMouseMoved.
        /// </summary>
        /// <returns>True if the element is allowed to handle inputs, False else.</returns>
        public bool HandleMouseMoved(float x, float y)
        {
            // return false if this element is not allowed to handling inputs
            if (!IsInputHandlingAllowed())
            {
                return false;
            }

            DoHandleMouseMoved(x, y);

            // iterate all child elements and return true afterwards
            foreach (AbstractGuiElement e in _childElements)
            {
                e.HandleMouseMoved(x, y);
            }
            return true;
        }

        /// <summary>
        /// Handles the gamepad key pressed event.
        /// Returns false if the GUI element does not allow input handling.
        /// 
        /// Cannot be overwritten. For handling specific user inputs please 
        /// overwrite the method DoHandleGamePadPressed.
        /// </summary>
        /// <returns>True if the element is allowed to handle inputs, False else.</returns>
        public bool HandleGamePadPressed(Buttons buttons)
        {
            // return false if this element is not allowed to handling inputs
            if (!IsInputHandlingAllowed())
            {
                return false;
            }

            DoHandleGamePadPressed(buttons);

            // iterate all child elements and return true afterwards
            foreach (AbstractGuiElement e in _childElements)
            {
                e.HandleGamePadPressed(buttons);
            }
            return true;
        }

        /// <summary>
        /// Handles the gamepad key released event.
        /// Returns false if the GUI element does not allow input handling.
        /// 
        /// Cannot be overwritten. For handling specific user inputs please 
        /// overwrite the method DoHandleGamePadReleased.
        /// </summary>
        /// <returns>True if the element is allowed to handle inputs, False else.</returns>
        public bool HandleGamePadReleased(Buttons buttons)
        {
            // return false if this element is not allowed to handling inputs
            if (!IsInputHandlingAllowed())
            {
                return false;
            }

            DoHandleGamePadReleased(buttons);

            // iterate all child elements and return true afterwards
            foreach (AbstractGuiElement e in _childElements)
            {
                e.HandleGamePadReleased(buttons);
            }
            return true;
        }

        #endregion

        #region Listener Actions (overrideable)

        /// <summary>
        /// Holds the action that is done when the input handler is active.
        /// Is doing nothing in default, have to be overridden to add action.
        /// </summary>
        /// <param name="key"></param>
        public virtual void DoHandleKeyPressed(Keys key)
        {
        }

        /// <summary>
        /// Holds the action that is done when the input handler is active.
        /// Is doing nothing in default, have to be overridden to add action.
        /// </summary>
        /// <param name="key"></param>
        public virtual void DoHandleKeyReleased(Keys key)
        {
        }

        /// <summary>
        /// Holds the action that is done when the input handler is active.
        /// Is doing nothing in default, have to be overridden to add action.
        /// </summary>
        /// <param name="key"></param>
        public virtual void DoHandleMousePressed(MouseButtons buttons)
        {
        }

        /// <summary>
        /// Holds the action that is done when the input handler is active.
        /// Is doing nothing in default, have to be overridden to add action.
        /// </summary>
        /// <param name="key"></param>
        public virtual void DoHandleMouseReleased(MouseButtons buttons)
        {
        }

        /// <summary>
        /// Holds the action that is done when the input handler is active.
        /// Is doing nothing in default, have to be overridden to add action.
        /// </summary>
        /// <param name="key"></param>
        public virtual void DoHandleMouseMoved(float x, float y)
        {
        }

        /// <summary>
        /// Holds the action that is done when the input handler is active.
        /// Is doing nothing in default, have to be overridden to add action.
        /// </summary>
        /// <param name="key"></param>
        public virtual void DoHandleGamePadPressed(Buttons buttons)
        {
        }

        /// <summary>
        /// Holds the action that is done when the input handler is active.
        /// Is doing nothing in default, have to be overridden to add action.
        /// </summary>
        /// <param name="key"></param>
        public virtual void DoHandleGamePadReleased(Buttons buttons)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks the status if an user input (keyboard, mouse or gamepad) is
        /// currently allowed for the GUI element.
        /// </summary>
        /// <returns>The status if an user input is allowed for the GUI element.</returns>
        private bool IsInputHandlingAllowed()
        {
            return Enabled;
        }

        #endregion

    }
}
