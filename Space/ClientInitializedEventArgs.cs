using System;
using Space.Control;

namespace Space
{
    internal sealed class ClientInitializedEventArgs : EventArgs
    {
        public GameClient Client { get; private set; }

        public ClientInitializedEventArgs(GameClient client)
        {
            Client = client;
        }
    }
}
