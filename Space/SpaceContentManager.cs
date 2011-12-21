using System;
using Microsoft.Xna.Framework.Content;

namespace Space
{
    class SpaceContentManager : ContentManager
    {
        private String _language;
        public SpaceContentManager(IServiceProvider service, String language)
            : base(service)
        {
            this._language = language;
        }

        public override T Load<T>(string assetName)
        {
            return base.Load<T>(assetName);
        }
    }
}
