using System;
using Space.Control;

namespace Space
{
    public sealed class ClientInitializedEventArgs : EventArgs
    {
        public GameClient Client { get; private set; }

        public ClientInitializedEventArgs(GameClient client)
        {
            Client = client;
        }
    }
}
