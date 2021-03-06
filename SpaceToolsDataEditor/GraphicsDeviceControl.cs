#region File Description
//-----------------------------------------------------------------------------
// GraphicsDeviceControl.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements

using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Graphics;
using WinFormsContentLoading;

#endregion

namespace Space.Tools.DataEditor
{
    // System.Drawing and the XNA Framework both define Color and Rectangle
    // types. To avoid conflicts, we specify exactly which ones to use.
    
    /// <summary>
    /// Custom control uses the XNA Framework GraphicsDevice to render onto
    /// a Windows Form. Derived classes can override the Initialize and Draw
    /// methods to add their own drawing code.
    /// </summary>
    public class GraphicsDeviceControl : Control
    {
        #region Properties

        /// <summary>
        /// Gets the graphics device service.
        /// </summary>
        public IGraphicsDeviceService GraphicsDeviceManager
        {
            get { return _graphicsDeviceService; }
        }

        /// <summary>
        /// Gets an IServiceProvider containing our IGraphicsDeviceService.
        /// This can be used with components such as the ContentManager,
        /// which use this service to look up the GraphicsDevice.
        /// </summary>
        public ServiceContainer Services
        {
            get { return _services; }
        }

        #endregion

        #region Fields

        readonly ServiceContainer _services = new ServiceContainer();

        // However many GraphicsDeviceControl instances you have, they all share
        // the same underlying GraphicsDevice, managed by this helper service.
        private GraphicsDeviceService _graphicsDeviceService;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the control.
        /// </summary>
        protected override void OnCreateControl()
        {
            // Don't initialize the graphics device if we are running in the designer.
            if (!DesignMode)
            {
                _graphicsDeviceService = GraphicsDeviceService.AddRef(Handle,
                                                                     ClientSize.Width,
                                                                     ClientSize.Height);

                // Register the service, so components like ContentManager can find it.
                _services.AddService<IGraphicsDeviceService>(_graphicsDeviceService);

                // Give derived classes a chance to initialize themselves.
                Initialize();
            }

            base.OnCreateControl();
        }

        /// <summary>
        /// Disposes the control.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (_graphicsDeviceService != null)
            {
                _graphicsDeviceService.Release(disposing);
                _graphicsDeviceService = null;
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Paint

        /// <summary>
        /// Redraws the control in response to a WinForms paint message.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            string beginDrawError = BeginDraw();

            if (string.IsNullOrEmpty(beginDrawError))
            {
                // Draw the control using the GraphicsDevice.
                Draw();
                EndDraw();
            }
            else
            {
                // If BeginDraw failed, show an error message using System.Drawing.
                PaintUsingSystemDrawing(e.Graphics, beginDrawError);
            }
        }

        /// <summary>
        /// Attempts to begin drawing the control. Returns an error message string
        /// if this was not possible, which can happen if the graphics device is
        /// lost, or if we are running inside the Form designer.
        /// </summary>
        string BeginDraw()
        {
            // If we have no graphics device, we must be running in the designer.
            if (_graphicsDeviceService == null)
            {
                return Text + "\n\n" + GetType();
            }

            // Make sure the graphics device is big enough, and is not lost.
            string deviceResetError = HandleDeviceReset();

            if (!string.IsNullOrEmpty(deviceResetError))
            {
                return deviceResetError;
            }

            var viewport = new Viewport
            {
                X = 0,
                Y = 0,
                Width = GraphicsDeviceManager.GraphicsDevice.PresentationParameters.BackBufferWidth,
                Height = GraphicsDeviceManager.GraphicsDevice.PresentationParameters.BackBufferHeight,
                MinDepth = 0,
                MaxDepth = 1
            };

            GraphicsDeviceManager.GraphicsDevice.Viewport = viewport;

            return null;
        }

        /// <summary>
        /// Ends drawing the control. This is called after derived classes
        /// have finished their Draw method, and is responsible for presenting
        /// the finished image onto the screen, using the appropriate WinForms
        /// control handle to make sure it shows up in the right place.
        /// </summary>
        void EndDraw()
        {
            try
            {
                var sourceRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, ClientSize.Width, ClientSize.Height);

                GraphicsDeviceManager.GraphicsDevice.Present(sourceRectangle, null, Handle);
            }
            catch
            {
                // Present might throw if the device became lost while we were
                // drawing. The lost device will be handled by the next BeginDraw,
                // so we just swallow the exception.
            }
        }

        /// <summary>
        /// Helper used by BeginDraw. This checks the graphics device status,
        /// making sure it is big enough for drawing the current control, and
        /// that the device is not lost. Returns an error string if the device
        /// could not be reset.
        /// </summary>
        string HandleDeviceReset()
        {
            bool deviceNeedsReset;

            switch (GraphicsDeviceManager.GraphicsDevice.GraphicsDeviceStatus)
            {
                case GraphicsDeviceStatus.Lost:
                    // If the graphics device is lost, we cannot use it at all.
                    return "Graphics device lost";

                case GraphicsDeviceStatus.NotReset:
                    // If device is in the not-reset state, we should try to reset it.
                    deviceNeedsReset = true;
                    break;

                default:
                    // If the device state is ok, check whether it is big enough.
                    var pp = GraphicsDeviceManager.GraphicsDevice.PresentationParameters;

                    deviceNeedsReset = (ClientSize.Width != pp.BackBufferWidth) ||
                                       (ClientSize.Height != pp.BackBufferHeight);
                    break;
            }

            // Do we need to reset the device?
            if (deviceNeedsReset)
            {
                try
                {
                    _graphicsDeviceService.ResetDevice(ClientSize.Width,
                                                      ClientSize.Height);
                }
                catch (Exception e)
                {
                    return "Graphics device reset failed\n\n" + e;
                }
            }

            return null;
        }

        /// <summary>
        /// If we do not have a valid graphics device (for instance if the device
        /// is lost, or if we are running inside the Form designer), we must use
        /// regular System.Drawing method to display a status message.
        /// </summary>
        protected virtual void PaintUsingSystemDrawing(System.Drawing.Graphics graphics, string text)
        {
            graphics.Clear(Color.DarkSlateGray);

            using (Brush brush = new SolidBrush(Color.Red))
            {
                using (var format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;

                    graphics.DrawString(text, Font, brush, ClientRectangle, format);
                }
            }
        }

        /// <summary>
        /// Ignores WinForms paint-background messages. The default implementation
        /// would clear the control to the current background color, causing
        /// flickering when our OnPaint implementation then immediately draws some
        /// other color over the top using the XNA Framework GraphicsDevice.
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Derived classes override this to initialize their drawing code.
        /// </summary>
        protected virtual void Initialize()
        {
        }

        /// <summary>
        /// Derived classes override this to draw themselves using the GraphicsDevice.
        /// </summary>
        protected virtual void Draw()
        {    
        }

        #endregion
    }
}
