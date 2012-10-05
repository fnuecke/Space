using System;
using System.IO;
using System.Text;
using Engine.FarMath;
using Engine.Serialization;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Engine.Tests.FarPhysics
{
    [TestFixture]
    public class WorldSerializationTests
    {

        [Test]
        public void TestSerialization()
        {
            //var w = new World(Vector2.Zero);

            //var b = BodyFactory.CreateBody(w, new FarPosition(100, -20));
            //FixtureFactory.AttachCircle(10, 1, b);
            //b.LinearVelocity = new Vector2(-10, 0);
            //w.Step(1f/60f);

            //BodyFactory.CreateCircle(w, 20, 2, new FarPosition(30, 10));

            //var p = new Packet();
            //using (var stream = new MemoryStream())
            //{
            //    new WorldXmlSerializer().Serialize(w, stream);
            //    p.Write(stream.GetBuffer());
            //}

            //p.Reset();
            //using (var stream = new MemoryStream(p.ReadByteArray(), false))
            //{
            //    var w2 = new WorldXmlDeserializer().Deserialize(stream);

            //    var h = new Hasher();
            //    w.Hash(h);
            //    var h1 = h.Value;
            //    h.Reset();
            //    w2.Hash(h);
            //    var h2 = h.Value;

            //    Assert.AreEqual(h1, h2);
            //}
        }
    }
}
