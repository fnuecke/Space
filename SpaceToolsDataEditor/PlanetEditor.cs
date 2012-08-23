using System;
using System.Drawing.Design;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using Space.ComponentSystem.Factories;

namespace Space.Tools.DataEditor
{
    /// <summary>
    /// Utility editor to allow choosing from known planets.
    /// </summary>
    public sealed class PlanetEditor : UITypeEditor
    {
        private readonly ListBox _list = new ListBox
        {
            SelectionMode = SelectionMode.One,
            BorderStyle = BorderStyle.None
        };

        public override UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }

        public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            var svc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
            if (svc != null)
            {
                _list.Items.Clear();
                FactoryManager.GetFactories()
                    .Where(factory => factory is PlanetFactory).ToList()
                    .ForEach(factory => _list.Items.Add(factory.Name));
                if (_list.Items.Count == 0)
                {
                    return value;
                }
                _list.Height = System.Math.Min(_list.Items.Count * _list.ItemHeight, _list.ItemHeight * 10);

                if (value != null)
                {
                    var item = _list.FindStringExact((string)value);
                    if (item >= 0)
                    {
                        _list.SelectedIndex = item;
                    }
                }
                EventHandler handler = (o, s) => svc.CloseDropDown();
                _list.SelectedIndexChanged += handler;
                svc.DropDownControl(_list);
                _list.SelectedIndexChanged -= handler;
                if (_list.SelectedItems.Count < 1)
                {
                    return value;
                }
                return _list.SelectedItems[0];
            }
            return value;
        }
    }
}
