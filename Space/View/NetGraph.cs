using System;
using System.Collections.Generic;
using Engine.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.View
{
    internal static class NetGraph
    {
        private static readonly Dictionary<SpriteBatch, Texture2D> PixelTextures =
            new Dictionary<SpriteBatch, Texture2D>();

        public static void Draw(IProtocolInfo info, Vector2 offset, SpriteFont font, SpriteBatch spriteBatch)
        {
            if (!PixelTextures.ContainsKey(spriteBatch))
            {
                var pixelTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
                pixelTexture.SetData(new[] {Color.White});
                PixelTextures[spriteBatch] = pixelTexture;
            }

            // Settings.
            const int graphWidth = 180, graphHeight = 40;

            // Precompute stuff.
            int minIncoming = int.MaxValue,
                maxIncoming = 0,
                avgIncoming = 0,
                minOutgoing = int.MaxValue,
                maxOutgoing = 0,
                avgOutgoing = 0,
                minTotal = int.MaxValue,
                maxTotal = 0,
                avgTotal = 0;

            var values = new Tuple<int, Color>[Math.Max(info.IncomingTraffic.Count, info.OutgoingTraffic.Count) - 1][];
            for (var i = 0; i < values.Length; ++i)
            {
                values[i] = new[]
                            {
                                Tuple.Create(0, Color.White),
                                Tuple.Create(0, Color.White),
                                Tuple.Create(0, Color.White),
                                Tuple.Create(0, Color.White),
                                Tuple.Create(0, Color.White)
                            };
            }

            {
                // Skip first entry, as that one's subject to change.
                var i = 1;
                var incoming = info.IncomingTraffic.First.Next;
                var outgoing = info.OutgoingTraffic.First.Next;
                while (i < info.IncomingTraffic.Count &&
                       incoming != null && outgoing != null)
                {
                    var subTotal = 0;
                    {
                        var val = incoming.Value[TrafficTypes.Any];
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

                        values[i - 1][0] = Tuple.Create(incoming.Value[TrafficTypes.Invalid], Color.Firebrick);
                        values[i - 1][1] = Tuple.Create(incoming.Value[TrafficTypes.Protocol], Color.DarkBlue);
                        values[i - 1][2] = Tuple.Create(incoming.Value[TrafficTypes.Data], Color.Blue);
                    }
                    {
                        int val = outgoing.Value[TrafficTypes.Any];
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

                        values[i - 1][3] = Tuple.Create(outgoing.Value[TrafficTypes.Protocol], Color.Green);
                        values[i - 1][4] = Tuple.Create(outgoing.Value[TrafficTypes.Data], Color.LimeGreen);
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

            avgIncoming /= info.HistoryLength - 1;
            avgOutgoing /= info.HistoryLength - 1;
            avgTotal /= info.HistoryLength - 1;

            var netInfo = String.Format("in: {0}|{1}|{2}|{3:f2}kB/s\n" +
                                        "    aps: {12:f2}|apc: {13:f2}\n" +
                                        "out: {4}|{5}|{6}|{7:f2}kB/s\n" +
                                        "     aps: {14:f2}|apc: {15:f2}\n" +
                                        "sum: {8}|{9}|{10}|{11:f2}kB/s",
                                        minIncoming, maxIncoming, avgIncoming, avgIncoming / 1024f,
                                        minOutgoing, maxOutgoing, avgOutgoing, avgOutgoing / 1024f,
                                        minTotal, maxTotal, avgTotal, avgTotal / 1024f,
                                        info.IncomingPacketSizes.Mean(), info.IncomingPacketCompression.Mean(),
                                        info.OutgoingPacketSizes.Mean(), info.OutgoingPacketCompression.Mean());
            var netInfoMeasure = font.MeasureString(netInfo);
            var netInfoPosition = offset;
            var graphPosition = new Vector2(offset.X, offset.Y + netInfoMeasure.Y + 5);

            var graphNormX = graphWidth / (float)Math.Max(info.IncomingTraffic.Count, info.OutgoingTraffic.Count);
            var graphNormY = graphHeight / (float)Math.Max(maxTotal, 1);

            // Draw it.
            spriteBatch.Begin();
            spriteBatch.DrawString(font, netInfo, netInfoPosition, Color.White);

            // Draw the bars.
            var barIdx = 0;
            foreach (var bar in values)
            {
                var barX = (int)(graphPosition.X + barIdx * graphNormX);
                var bottom = graphPosition.Y + graphHeight;
                foreach (var segment in bar)
                {
                    if (segment.Item1 <= 0)
                    {
                        continue;
                    }

                    var top = (int)(bottom - segment.Item1 * graphNormY);
                    var line = new Rectangle(barX, top, (int)graphNormX, (int)(bottom - top));
                    spriteBatch.Draw(PixelTextures[spriteBatch], line, segment.Item2);
                    bottom = top;
                }
                ++barIdx;
            }

            spriteBatch.End();
        }
    }
}
