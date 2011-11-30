using System;
using System.Collections.Generic;
using Engine.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.View
{
    static class NetGraph
    {
        private static Dictionary<SpriteBatch, Texture2D> pixelTextures = new Dictionary<SpriteBatch, Texture2D>();

        public static void Draw(ProtocolInfo info, Vector2 offset, SpriteFont font, SpriteBatch spriteBatch)
        {
            if (!pixelTextures.ContainsKey(spriteBatch))
            {
                var pixelTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
                pixelTexture.SetData(new[] { Color.White });
                pixelTextures[spriteBatch] = pixelTexture;
            }

            // Settings.
            const int graphWidth = 180, graphHeight = 40;

            // Precompute stuff.
            int minIncoming = int.MaxValue, maxIncoming = 0, avgIncoming = 0,
                minOutgoing = int.MaxValue, maxOutgoing = 0, avgOutgoing = 0,
                minTotal = int.MaxValue, maxTotal = 0, avgTotal = 0;

            var values = new Tuple<int, Color>[System.Math.Max(info.IncomingTraffic.Count, info.OutgoingTraffic.Count) - 1][];
            for (int i = 0; i < values.Length; ++i)
            {
                values[i] = new Tuple<int, Color>[]
                {
                    Tuple.Create(0, Color.White),
                    Tuple.Create(0, Color.White),
                    Tuple.Create(0, Color.White),
                    Tuple.Create(0, Color.White)
                };
            }

            {
                // Skip first entry, as that one's subject to change.
                int i = 1;
                var incoming = info.IncomingTraffic.First.Next;
                var outgoing = info.OutgoingTraffic.First.Next;
                while (i < info.IncomingTraffic.Count &&
                        incoming != null && outgoing != null)
                {
                    int subTotal = 0;
                    {
                        int val = incoming.Value[TrafficType.Any];
                        if (val < minIncoming)
                        {
                            minIncoming = val;
                        }
                        if (val > maxIncoming)
                        {
                            maxIncoming = val;
                        }
                        avgIncoming += val;
                        subTotal += val;

                        values[i - 1][0] = Tuple.Create(incoming.Value[TrafficType.Protocol], Color.Yellow);
                        values[i - 1][1] = Tuple.Create(incoming.Value[TrafficType.Data], Color.Red);
                    }
                    {
                        int val = outgoing.Value[TrafficType.Any];
                        if (val < minOutgoing)
                        {
                            minOutgoing = val;
                        }
                        if (val > maxOutgoing)
                        {
                            maxOutgoing = val;
                        }
                        avgOutgoing += val;
                        subTotal += val;

                        values[i - 1][2] = Tuple.Create(outgoing.Value[TrafficType.Protocol], Color.Blue);
                        values[i - 1][3] = Tuple.Create(outgoing.Value[TrafficType.Data], Color.Green);
                    }
                    if (subTotal < minTotal)
                    {
                        minTotal = subTotal;
                    }
                    if (subTotal > maxTotal)
                    {
                        maxTotal = subTotal;
                    }
                    avgTotal += subTotal;

                    ++i;
                    incoming = incoming.Next;
                    outgoing = outgoing.Next;
                }
            }

            avgIncoming /= info.IncomingTraffic.Count - 1;
            avgOutgoing /= info.OutgoingTraffic.Count - 1;
            avgTotal /= info.OutgoingTraffic.Count - 1;

            string netInfo = String.Format("in: {0}|{1}|{2} - {3:f}kB/s\n" +
                                           "out: {4}|{5}|{6} - {7:f}kB/s\n" +
                                           "sum: {8}|{9}|{10} - {11:f}kB/s",
                                           minIncoming, maxIncoming, avgIncoming, avgIncoming / 1024f,
                                           minOutgoing, maxOutgoing, avgOutgoing, avgOutgoing / 1024f,
                                           minTotal, maxTotal, avgTotal, avgTotal / 1024f);
            var netInfoMeasure = font.MeasureString(netInfo);
            var netInfoPosition = offset;
            var graphPosition = new Vector2(offset.X, offset.Y + netInfoMeasure.Y + 5);

            float graphNormX = graphWidth / (float)System.Math.Max(info.IncomingTraffic.Count, info.OutgoingTraffic.Count);
            float graphNormY = graphHeight / (float)System.Math.Max(maxTotal, 1);

            // Draw it.
            spriteBatch.Begin();
            spriteBatch.DrawString(font, netInfo, netInfoPosition, Color.White);

            // Draw the bars.
            int barIdx = 0;
            foreach (var bar in values)
            {
                int barX = (int)(graphPosition.X + barIdx * graphNormX);
                float bottom = graphPosition.Y + graphHeight;
                foreach (var segment in bar)
                {
                    if (segment.Item1 > 0)
                    {
                        int top = (int)(bottom - segment.Item1 * graphNormY);
                        var line = new Rectangle(barX, top, (int)graphNormX, (int)(bottom - top));
                        spriteBatch.Draw(pixelTextures[spriteBatch], line, segment.Item2);
                        bottom = top;
                    }
                }
                ++barIdx;
            }

            spriteBatch.End();
        }
    }
}
