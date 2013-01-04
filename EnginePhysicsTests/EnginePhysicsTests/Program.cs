using Engine.Serialization;

namespace Engine.Physics.Tests
{
#if WINDOWS || XBOX
    internal static class Program
    {
        private class A : IPacketizable
        {
            private string _type = "a";

            protected int _value;

            public A(int value)
            {
                _value = value;
            }

            public int Value
            {
                get { return _value; }
            }

            public virtual float Test { get; set; }

            public Packet Packetize(Packet packet)
            {
                return packet;
            }

            public void Depacketize(Packet packet)
            {
            }
        }

        private class B : A
        {
            internal string _b = "b";

            public B() : base(8)
            {
            }

            public string Type
            {
                get { return _b; }
            }

            public override float Test { get; set; }

        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main(string[] args)
        {
            var a = new A(4);
            a.Test = 7357;
            var b = new B();
            b.Test = 87357;

            var p = new Packet();
            Packetizable.Write(p, a);

            p.Reset();
            A ao = new A(1);
            p.ReadInto(ao);

            p.Reset();
            Packetizable.Write(p, b);

            p.Reset();
            B bo;
            p.Read(out bo);

            p.Reset();
            Packetizable.WriteWithTypeInfo(p, b);

            p.Reset();
            IPacketizable co = p.ReadWithTypeInfo<A>();

            using (var game = new TestRunner())
            {
                game.Run();
            }
        }
    }
#endif
}
