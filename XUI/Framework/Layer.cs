//-----------------------------------------------
// XUI - Layer.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using Microsoft.Xna.Framework.Content;

namespace XUI
{
    // class Layer
    public abstract class Layer
    {
        // Layer
        public Layer(int type)
        {
            Type = type;
            DoUpdate = true;
            DoRender = true;
        }

        // Startup
        public virtual void Startup(ContentManager content)
        {
            //
        }

        // Shutdown
        public virtual void Shutdown()
        {
            //
        }

        // Update
        public void Update(float frameTime)
        {
            if (!DoUpdate)
                return;

            OnUpdate(frameTime);
        }

        // OnUpdate
        protected virtual void OnUpdate(float frameTime)
        {
            //
        }

        // Render
        public void Render(float frameTime)
        {
            if (!DoRender)
                return;

            OnRender(frameTime);
        }

        // OnRender
        protected virtual void OnRender(float frameTime)
        {
            //
        }

        //
        public int Type { get; private set; }
        public bool DoUpdate;
        public bool DoRender;
        //
    };
}