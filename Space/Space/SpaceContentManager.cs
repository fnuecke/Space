using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;

namespace Space
{
    class SpaceContentManager:ContentManager
    {
        String Language;
        public SpaceContentManager(IServiceProvider service,String language)
            :base(service)
        {
            Language = language;
        }
        override
        public  T Load<T>(string assetName)
        {
            
            return base.Load<T>(assetName);
        }
    }
}
