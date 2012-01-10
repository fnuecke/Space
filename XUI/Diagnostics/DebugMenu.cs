//-----------------------------------------------
// XUI - DebugMenu.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XUI
{
    // class DebugMenu
    public class DebugMenu : XUI.Diagnostics.Menu
    {
        // DebugMenu
        public DebugMenu(SpriteBatch spriteBatch, Texture2D texture, SpriteFont font)
            : base()
        {
            SpriteBatch = spriteBatch;
            Texture = texture;
            Font = font;

            TempString = new StringBuilder(32); // should be plenty

            H = Font.LineSpacing;
            ArrowH = H * 0.5f;
            Padding = 20.0f;

            PresentationParameters pp = _UI.Game.GraphicsDevice.PresentationParameters;

            float h = pp.BackBufferHeight;
            h -= (Padding * 2.0f);

            MaxRenderCount = (int)(h / H);
        }

        // OnUpdate
        protected override void OnUpdate(float frameTime)
        {
            Input input = _UI.GameInput.GetInput(0); // debug only pad 0

            if ((input.ButtonDown((int)E_UiButton.Back) && input.ButtonJustPressed((int)E_UiButton.Start)))
            {
                Active = !Active;

                if (Active)
                    UpdateBounds();
            }
            else
                if (Active)
                {
                    float delay = _UI.AutoRepeatDelay;
                    float repeat = _UI.AutoRepeatRepeat;

                    if (input.ButtonJustPressed((int)E_UiButton.A))
                        Next();
                    else
                        if (input.ButtonJustPressed((int)E_UiButton.B))
                            Back();
                        else
                            if (input.ButtonAutoRepeat((int)E_UiButton.Up, delay, repeat))
                                Up();
                            else
                                if (input.ButtonAutoRepeat((int)E_UiButton.Down, delay, repeat))
                                    Down();
                                else
                                    if (input.ButtonAutoRepeat((int)E_UiButton.Left, delay, repeat))
                                        Decrease();
                                    else
                                        if (input.ButtonAutoRepeat((int)E_UiButton.Right, delay, repeat))
                                            Increase();
                                        else
                                            if (input.ButtonJustPressed((int)E_UiButton.X))
                                                ResetToDefault();
                }
        }

        // OnNext
        protected override void OnNext()
        {
            UpdateBounds();
        }

        // OnBack
        protected override void OnBack()
        {
            UpdateBounds();
        }

        // UpdateBounds
        protected void UpdateBounds()
        {
            XUI.Diagnostics.MenuItem v = (Current.Parent != null) ? Current.Parent.Child1 : Current;

            Count = 0;
            SizeX1 = 0.0f;
            SizeX2 = 0.0f;
            SizeY = 0.0f;

            while (v != null)
            {
                SizeX1 = Math.Max(SizeX1, Font.MeasureString(v.Name).X);
                SizeY += H;

                ++Count;
                v = v.Next;
            }

            if (Count > MaxRenderCount)
                SizeY = (MaxRenderCount * H);

            SizeX2 = 200.0f + (ArrowH * 2.0f); // TODO - properly - mebs
        }

        // OnRender
        protected override void OnRender()
        {
            if ((Texture == null) || (Font == null))
                return;

            Color colour = Color.White;
            Color colourCurrent = Color.Orange;

            float x = Padding;
            float y = Padding;

            SpriteBatch.Begin();

            // backing
            Color c1 = Color.Black * 0.85f;
            Color c2 = Color.Black * 0.85f;

            float sx1 = (SizeX1 + Padding);
            float sx2 = (SizeX2 + Padding);

            SpriteBatch.Draw(Texture, new Rectangle((int)(x - (Padding * 0.5f)), (int)(y - (Padding * 0.5f)), (int)sx1, (int)(SizeY + Padding)), new Rectangle(1, 1, 30, 30), c1);
            SpriteBatch.Draw(Texture, new Rectangle((int)(x + sx1 - (Padding * 0.5f)), (int)(y - (Padding * 0.5f)), (int)sx2, (int)(SizeY + Padding)), new Rectangle(1, 1, 30, 30), c2);

            // options - start at top
            XUI.Diagnostics.MenuItem v = (Current.Parent != null) ? Current.Parent.Child1 : Current;

            // find out what index we're at
            // - could probs just update this on up/down instead
            int index = 0;

            for (XUI.Diagnostics.MenuItem vv = v; ((vv != null) && (vv != Current)); vv = vv.Next)
                ++index;

            // what we shall render
            int low = index - (MaxRenderCount / 2);

            if (low < 0)
                low = 0;

            int high = low + MaxRenderCount - 1;

            if (high >= Count)
            {
                high = Count - 1;
                low = Count - MaxRenderCount;
            }

            // now render down
            int sCount = 0;

            for (; v != null; ++sCount, v = v.Next)
            {
                if ((sCount < low) || (sCount > high))
                    continue;

                x = Padding;

                Color c = (v == Current) ? colourCurrent : colour;

                SpriteBatch.DrawString(Font, v.Name, new Vector2(x, y), c);

                Vector2 size = Font.MeasureString(v.AsString(TempString));
                SpriteBatch.DrawString(Font, TempString, new Vector2(x + SizeX1 + (SizeX2 / 2.0f) + Padding - (size.X / 2.0f), y), c);

                // arrows
                if (v == Current)
                {
                    int yt = (int)(y + (H / 2.0f) - (ArrowH / 2.0f));

                    if (CanDecrease())
                        SpriteBatch.Draw(Texture, new Rectangle((int)(x + SizeX1 + Padding), yt, (int)ArrowH, (int)ArrowH), new Rectangle(64, 0, -32, 32), c);
                    if (CanIncrease())
                        SpriteBatch.Draw(Texture, new Rectangle((int)(x + SizeX1 + SizeX2 - ArrowH + Padding), yt, (int)ArrowH, (int)ArrowH), new Rectangle(32, 0, 32, 32), c);
                }

                y += H;
            }

            SpriteBatch.End();
        }

        //
        private SpriteBatch SpriteBatch;
        private Texture2D Texture;
        private SpriteFont Font;

        private StringBuilder TempString;

        private int Count;
        private int MaxRenderCount;
        private float Padding;
        private float SizeX1;
        private float SizeX2;
        private float SizeY;
        private float H;
        private float ArrowH;
        //
    };
}