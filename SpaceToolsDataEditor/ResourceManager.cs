using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Factories;

namespace Space.Tools.DataEditor
{
    class ResourceManager
    {
        public static void AddResource(SunSystemFactory sunSystem)
        {
            Search(sunSystem);

        }

        private static void Search(Object objects)
        {
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(objects))
            {
                if (property.Attributes.Contains(new ContentSerializerAttribute(){Optional = true}))
                {
                    var i = 0;

                }
                if (property.Attributes.Contains(new ContentSerializerAttribute() {Optional = true, SharedResource = true }))
                {
                    var i = 0;

                }
                if (property.Attributes.Contains(new SunSystemFactory.ChildrenHaveSharedResourcesAttribute()))
                {
                    Search(property.GetValue(objects));
                }
            }
        }
    }
}
