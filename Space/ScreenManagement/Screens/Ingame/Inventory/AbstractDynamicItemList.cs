using Engine.ComponentSystem.RPG.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using Nuclex.Input;
using Space.ComponentSystem.Components;
using Space.Control;
using Space.Data;
using Space.ScreenManagement.Screens.Helper;
using Space.ScreenManagement.Screens.Ingame.GuiElementManager;
using Space.ScreenManagement.Screens.Ingame.Interfaces;
using Space.Simulation.Commands;

namespace Space.ScreenManagement.Screens.Ingame.Hud
{

    /// <summary>
    /// A class that displayed a specific number of item slots.
    /// 
    /// By setting new values for the properties the design can be changed.
    /// Several input handlers and mouse over effects makes it possible to
    /// move items to different the slots. By using the basic item selection
    /// manager its also possible to move them to other DynamicItemList objects.
    /// 
    /// By using the alignment property it is possible to set the alignment to
    /// left, center or right.
    /// </summary>
    abstract class AbstractDynamicItemList : AbstractGuiElement, IItem
    {

        #region Constants

        /// <summary>
        /// The alignment for this element.
        /// </summary>
        public enum Align
        {
            Left, Center, Right
        }
        
        #endregion
        
        #region Fields

        /// <summary>
        /// The basic item selection manager.
        /// </summary>
        private ItemSelectionManager _itemSelection;

        /// <summary>
        /// The basic texture manager.
        /// </summary>
        private TextureManager _textureManager;

        #endregion

        #region Properties

        /// <summary>
        /// The size (height and width) of the icons
        /// </summary>
        public int IconSize { get; set; }

        /// <summary>
        /// The margin around the icons. (= gap between the icons)
        /// </summary>
        public int Margin { get; set; }

        /// <summary>
        /// The number of icons that are displayed each row.
        /// </summary>
        public int ElementsEachRow { get; set; }

        /// <summary>
        /// The current Alignment of the element.
        /// </summary>
        public Align Alignment { get; set; }

        #endregion

        #region Initialisation

        /// <summary>
        /// Constructor
        /// </summary>
        public AbstractDynamicItemList(GameClient client, ItemSelectionManager itemSelection, TextureManager textureManager)
            : base(client)
        {
            _textureManager = textureManager;
            _itemSelection = itemSelection;

            // set some standard values
            IconSize = 35;
            Margin = 2;
            ElementsEachRow = 5;
            Alignment = Align.Left;
        }

        public override void LoadContent(IngameScreen ingame, ContentManager content)
        {
            base.LoadContent(ingame, content);
            base.Enabled = true;

            _scale = ingame.Scale;
        }

        #endregion

        #region Draw

        public override void Draw()
        {
            _spriteBatch.Begin();

            for (int i = 0; i < DataCount(); i++)
            {
                // draw the background that is visible if no icon is displayed
                _basicForms.FillRectangle(_scale.X(WestX(i)), _scale.Y(NorthY(i)), _scale.X(IconSize), _scale.Y(IconSize), Color.White * 0.2f);

                // load the image of the icon that is saved in this slot
                string imagePath = null;
                var item = ItemAt(i);
                if (item != null)
                {
                    imagePath = item.IconName;
                }
                var image = _textureManager.Get(imagePath);

                // draw the current item if a) an item is available for this slot and b) the item is currently not selected
                if (imagePath != null && !(_itemSelection.SelectedId == i && _itemSelection.SelectedClass == this))
                {
                    _spriteBatch.Draw(image, new Rectangle(_scale.X(WestX(i)), _scale.Y(NorthY(i)), _scale.X(IconSize), _scale.Y(IconSize)), Color.White);
                }
            }

            ItemDescription description = new ItemDescription();
            for (int i = 0; i < DataCount(); i++)
            {
                // draw the tooltip
                if (IsMousePositionOnIcon(i) && ItemAt(i) != null)
                {
                    _basicForms.FillRectangle(_scale.X(WestX(i)) + IconSize + 10, _scale.Y(NorthY(i)), 200, 100, Color.Black * 0.5f);

                    var line = 1;
                    var item = ItemAt(i);
                    if (item != null)
                    {
                        // TODO: item.Name is the id, convert it to a localized display string, ideally taking into account modifiers for prefixes suffixes (stupid armor of the nerd).
                        _fonts.DrawString(Fonts.Types.ConsoleFont, item.Name, new Vector2(_scale.X(WestX(i)) + IconSize + 20, _scale.Y(NorthY(i)) + line * 12), Color.White);
                        line++;
                        var stackable = item.Entity.GetComponent<Stackable>();
                        if (stackable != null)
                        {
                            _fonts.DrawString(Fonts.Types.ConsoleFont, stackable.Count + "/" + stackable.MaxCount, new Vector2(_scale.X(WestX(i)) + IconSize + 20, _scale.Y(NorthY(i)) + line * 12), Color.White);
                            line++;
                        }
                        if (item is SpaceItem)
                        {
                            ((SpaceItem)item).GetDescription(ref description);
                            var attributes = description.Attributes;
                            foreach (var attribute in attributes)
                            {
                                _fonts.DrawString(Fonts.Types.ConsoleFont, attribute.Type + " " + attribute.Value, new Vector2(_scale.X(WestX(i)) + IconSize + 20, _scale.Y(NorthY(i)) + line * 12), Color.White);
                                line++;
                            }
                        }
                    }
                    
                }
            }


            _spriteBatch.End();
        }

        #endregion

        #region Listener

        public override bool DoHandleMousePressed(MouseButtons buttons)
        {
            if (buttons == MouseButtons.Left)
            {
                // If no item is selected, select the item and enable the drag 'n drop mode...
                if (!_itemSelection.ItemIsSelected)
                {
                    for (int i = 0; i < DataCount(); i++)
                    {
                        if (IsMousePositionOnIcon(i))
                        {
                            string imagePath = null;
                            var item = ItemAt(i);
                            if (item != null)
                            {
                                imagePath = item.IconName;
                            }
                            _itemSelection.DragNDropMode = true;

                            // ... set it selected.
                            if (imagePath != null)
                            {
                                _itemSelection.SetSelection(this, i, imagePath);
                            }
                        }
                    }
                }
                // otherwise disable the Drag 'n Drop Mode
                else
                {
                    _itemSelection.DragNDropMode = false;
                }
            }
            return false;
        }

        public override bool DoHandleMouseReleased(MouseButtons buttons)
        {
            if (buttons == MouseButtons.Left)
            {
                if (_itemSelection.DragNDropMode)
                {
                    for (int i = 0; i < DataCount(); i++)
                    {
                        if (IsMousePositionOnIcon(i))
                        {
                            // if it is the same slot set the variable sameItem to true to signal
                            // that the rest of the method should be run
                            if (i == _itemSelection.SelectedId)
                            {
                                _itemSelection.RemoveSelection();
                                _itemSelection.DragNDropMode = false;
                                break;
                            }
                            // if it is _not_ the same icon set the variable sameItem to false. The
                            // rest of the method should not be run.
                            else
                            {
                                string imagePath = null;
                                var item = ItemAt(i);
                                if (item != null)
                                {
                                    imagePath = item.IconName;
                                }

                                // ... tell the manager to swap the items.
                                var previousId = _itemSelection.SelectedId;
                                if (previousId != -1)
                                {
                                    _client.Controller.PushLocalCommand(new MoveItemCommand(i, previousId));
                                    _client.Save();
                                    _itemSelection.RemoveSelection();
                                    _itemSelection.DragNDropMode = false;
                                }
                                return true;
                            }
                        }
                    }
                }

                for (int i = 0; i < DataCount(); i++)
                {
                    // if the mouse click is within the current item dimension
                    if (IsMousePositionOnIcon(i))
                    {
                        string imagePath = null;
                        var item = ItemAt(i);
                        if (item != null)
                        {
                            imagePath = item.IconName;
                        }

                        // if an item is currently selected...
                        if (_itemSelection.ItemIsSelected)
                        {
                            // ... tell the manager to swap the items.
                            // but only if the slots are different ones...
                            if (i != _itemSelection.SelectedId)
                            {
                                _client.Controller.PushLocalCommand(new MoveItemCommand(i, _itemSelection.SelectedId));
                                _client.Save();
                            }
                            // ... if they are the same slots just remove the selection and do nothing
                            else
                            {
                                _itemSelection.RemoveSelection();
                                break;
                            }

                            // if the item was set into a slot which also holds an item
                            // then set the item from the slot as a selected one ...
                            if (ItemAt(i) != null)
                            {
                                _itemSelection.SetSelection(this, i, imagePath);
                            }
                            // ... but remove the selection if the slot was empty.
                            else
                            {
                                _itemSelection.RemoveSelection();
                            }
                        }

                        // if no item is selected...
                        else
                        {
                            // ... set it selected.
                            if (ItemAt(i) != null)
                            {
                                _itemSelection.SetSelection(this, i, imagePath);
                            }
                        }
                        break;
                    }

                }
            }
            return true;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns the status if the mouse cursor is currently over the icon with a specific id.
        /// </summary>
        /// <param name="id">The id of the icon to check.</param>
        /// <returns>The status if the mouse cursor is currently over the icon with a specific id</returns>
        private bool IsMousePositionOnIcon(int id)
        {
            return Mouse.GetState().X >= _scale.X(WestX(id))
                && Mouse.GetState().X <= _scale.X(EastX(id))
                && Mouse.GetState().Y >= _scale.Y(NorthY(id))
                && Mouse.GetState().Y <= _scale.Y(SouthY(id));
        }

        /// <summary>
        /// Calculates the western X position of any icon which is determine by the slot id.
        /// </summary>
        /// <param name="id">The id of the slot.</param>
        /// <returns>The western X position of the slot.</returns>
        private int WestX(int id)
        {
            var elementsLastRow = DataCount() % ElementsEachRow;
            var lastRowId = DataCount() / ElementsEachRow;
            var elementRow = id / ElementsEachRow;

            var indent = 0;

            if (elementRow == lastRowId)
            {
                if (Alignment == Align.Right)
                {
                    indent = (elementRow - elementsLastRow + 1) * (IconSize + Margin);
                }
                else if (Alignment == Align.Center)
                {
                    indent = ((elementRow - elementsLastRow + 1) * (IconSize + Margin)) / 2;
                }

            }

            return indent + (int)GetPosition().X + (id % ElementsEachRow) * (IconSize + Margin);
        }

        /// <summary>
        /// Calculates the northern Y position of any icon which is determine by the slot id.
        /// </summary>
        /// <param name="id">The id of the slot.</param>
        /// <returns>The northern Y position of the slot.</returns>
        private int NorthY(int id)
        {
            return (int)GetPosition().Y + (id / ElementsEachRow) * (IconSize + Margin);
        }

        /// <summary>
        /// Calculates the eastern X position of any icon which is determine by the slot id.
        /// </summary>
        /// <param name="id">The id of the slot.</param>
        /// <returns>The eastern X position of the slot.</returns>
        private int EastX(int id)
        {
            return WestX(id) + IconSize;
        }

        /// <summary>
        /// Calculates the southern Y position of any icon which is determine by the slot id.
        /// </summary>
        /// <param name="id">The id of the slot.</param>
        /// <returns>The southern Y position of the slot.</returns>
        private int SouthY(int id)
        {
            return NorthY(id) + IconSize;
        }

        /// <summary>
        /// Returns the number of slots that are available.
        /// </summary>
        /// <returns>The number of slots that are available.</returns>
        public abstract int DataCount();

        /// <summary>
        /// Returns the item at a specified position.
        /// </summary>
        /// <param name="id">The id of the slot of the item.</param>
        /// <returns>The item.</returns>
        public abstract Item ItemAt(int id);

        #endregion

    }
}
