using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.Serialization;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// Represents a player's inventory, with a list of items in it.
    /// </summary>
    public sealed class Inventory : AbstractComponent, IList<Entity>
    {
        #region Properties

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <returns>
        /// The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        ///   </returns>
        public int Count
        {
            get
            {
                if (_isFixed)
                {
                    // Get the number of slots that are actually occupied.
                    int count = 0;
                    for (int i = 0; i < _items.Count; i++)
                    {
                        if (_items[i] > 0)
                        {
                            ++count;
                        }
                    }
                    return count;
                }
                else
                {
                    // Dynamic length, no gaps to use actual count.
                    return _items.Count;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        /// </summary>
        /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.
        ///   </returns>
        public bool IsReadOnly { get { return _isFixed; } }

        /// <summary>
        /// A fixed capacity for this inventory. If set, there may be gaps in
        /// the list. Disable by setting it to zero.
        /// </summary>
        /// <remarks>Important: unlike the <c>List</c>s capacity, e.g., this
        /// capacity is fixed after it is set (unless it is set to zero).</remarks>
        public int Capacity
        {
            get
            {
                return _items.Count;
            }
            set
            {
                if (_items.Count > value)
                {
                    // Remove items that are out of bounds after fixing.
                    for (int i = _items.Count - 1; i >= value; --i)
                    {
                        var itemUid = _items[i];
                        if (itemUid > 0)
                        {
                            // This will, via messaging, also remove the item
                            // from the list, so the following capacity change
                            // is safe.
                            Entity.Manager.RemoveEntity(itemUid);
                        }
                        // If the list was fixed length, remove manually.
                        if (_isFixed)
                        {
                            _items.RemoveAt(i);
                        }
                    }
                }

                // Adjust capacity.
                _items.Capacity = value;

                // Fill up with zeros.
                for (int i = _items.Capacity - _items.Count; i > 0; --i)
                {
                    _items.Add(0);
                }

                // Remember our type.
                _isFixed = value > 0;
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// A list of items currently in this inventory.
        /// </summary>
        private List<int> _items = new List<int>();

        /// <summary>
        /// Whether we have a fixed length list.
        /// </summary>
        private bool _isFixed;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new inventory with a fixed capacity.
        /// </summary>
        /// <param name="fixedCapacity">The capacity of the inventory.</param>
        public Inventory(int fixedCapacity)
        {
            this.Capacity = fixedCapacity;
        }

        /// <summary>
        /// Creates a new inventory with a dynamic size.
        /// </summary>
        public Inventory()
        {
        }

        #endregion

        #region List interface

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <returns>
        /// The element at the specified index.
        ///   </returns>
        ///   
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.
        ///   </exception>
        ///   
        /// <exception cref="T:System.NotSupportedException">
        /// The property is set and the <see cref="T:System.Collections.Generic.IList`1"/> is read-only.
        ///   </exception>
        public Entity this[int index]
        {
            get
            {
                // Check for null entries (entity manager throws for unknown
                // values, as it should).
                if (_isFixed && _items[index] <= 0)
                {
                    return null;
                }
                return Entity.Manager.GetEntity(_items[index]);
            }
            set
            {
                _items[index] = value.UID;
            }
        }

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        /// <exception cref="T:System.NotSupportedException">
        /// The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        ///   </exception>
        /// <exception cref="T:System.InvalidOperationException">
        /// The fixed length inventory is already full.
        /// </exception>
        public void Add(Entity item)
        {
            // If the item is stackable, see if we already have a stack we can
            // add it on top of.
            var stackable = item.GetComponent<Stackable>();
            if (stackable != null)
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    if (!_isFixed || _items[i] > 0)
                    {
                        var otherStackable = this[i].GetComponent<Stackable>();
                        if (otherStackable != null &&
                            otherStackable.GroupId == stackable.GroupId &&
                            otherStackable.Count < otherStackable.MaxCount)
                        {
                            // Found a non-full stack of matching type, add as many
                            // as possible.
                            int toAdd = System.Math.Min(otherStackable.MaxCount - otherStackable.Count, stackable.Count);
                            otherStackable.Count += toAdd;
                            stackable.Count -= toAdd;

                            // We done yet?
                            if (stackable.Count == 0)
                            {
                                return;
                            } // ... else we continue in search of the next stack.
                        }
                    }
                }
            }

            // At this point, just add it normally. We get here via two routes:
            // * item is not stackable, which is the trivial case.
            // * item is stackable but could not be completely distributed to
            //   existing stacks, so we need to add what remains as a new one.
            if (_isFixed)
            {
                // Find the first free slot.
                for (int i = 0; i < _items.Count; ++i)
                {
                    if (_items[i] <= 0)
                    {
                        _items[i] = item.UID;
                        return;
                    }
                }

                // No free slot found!
                throw new InvalidOperationException("Inventory full.");
            }
            else
            {
                // Just append.
                _items.Add(item.UID);
            }
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">
        /// The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        ///   </exception>
        public void Clear()
        {
            // Remove all items and destroy them.
            for (int i = _items.Count - 1; i > 0; --i)
            {
                int itemUid = _items[i];
                if (itemUid > 0)
                {
                    // Will remove / clear the slot via messaging.
                    Entity.Manager.RemoveEntity(itemUid);
                }
            }
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        /// <returns>
        /// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
        /// </returns>
        public bool Contains(Entity item)
        {
            return _items.Contains(item.UID);
        }

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">Index of the array.</param>
        public void CopyTo(Entity[] array, int arrayIndex)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                array[arrayIndex + i] = Entity.Manager.GetEntity(_items[i]);
            }
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
        /// <returns>
        /// The index of <paramref name="item"/> if found in the list; otherwise, -1.
        /// </returns>
        public int IndexOf(Entity item)
        {
            return _items.IndexOf(item.UID);
        }

        /// <summary>
        /// Inserts an item to the <see cref="T:System.Collections.Generic.IList`1"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.
        ///   </exception>
        ///   
        /// <exception cref="T:System.NotSupportedException">
        /// The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.
        ///   </exception>
        /// <exception cref="System.NotSupportedException">If the inventory is of fixed length.</exception>
        public void Insert(int index, Entity item)
        {
            if (_isFixed)
            {
                this[index] = item;
            }
            else
            {
                _items.Insert(index, item.UID);
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        /// <returns>
        /// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">
        /// The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        ///   </exception>
        public bool Remove(Entity item)
        {
            if (_isFixed)
            {
                // Find where the item sits, then null the entry.
                int index = IndexOf(item);
                if (index >= 0)
                {
                    _items[index] = 0;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return _items.Remove(item.UID);
            }
        }

        /// <summary>
        /// Removes the <see cref="T:System.Collections.Generic.IList`1"/> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.
        ///   </exception>
        ///   
        /// <exception cref="T:System.NotSupportedException">
        /// The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.
        ///   </exception>
        public void RemoveAt(int index)
        {
            if (_isFixed)
            {
                _items[index] = 0;
            }
            else
            {
                _items.RemoveAt(index);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<Entity> GetEnumerator()
        {
            if (_isFixed)
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i] > 0)
                    {
                        yield return Entity.Manager.GetEntity(_items[i]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    yield return Entity.Manager.GetEntity(_items[i]);
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Swap two items in the list.
        /// </summary>
        /// <param name="firstIndex">The first index involved.</param>
        /// <param name="secondIndex">The second index involved.</param>
        public void Swap(int firstIndex, int secondIndex)
        {
            int tmp = _items[firstIndex];
            _items[firstIndex] = _items[secondIndex];
            _items[secondIndex] = tmp;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Check for removed entities.
        /// </summary>
        public override void HandleMessage<T>(ref T message)
        {
            if (message is EntityRemoved)
            {
                // If an entity was removed from the game and it was in this
                // inventory, remove it here, too.
                var removed = (EntityRemoved)(ValueType)message;
                Remove(removed.Entity);
            }
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet);

            packet.Write(_items.Count);
            for (int i = 0; i < _items.Count; i++)
            {
                packet.Write(_items[i]);
            }

            packet.Write(_isFixed);

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _items.Clear();
            int numItems = packet.ReadInt32();
            for (int i = 0; i < numItems; i++)
            {
                _items.Add(packet.ReadInt32());
            }

            _isFixed = packet.ReadBoolean();
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (Inventory)base.DeepCopy(into);

            if (copy == into)
            {
                copy._items.Clear();
                copy._items.AddRange(_items);
                copy._isFixed = _isFixed;
            }
            else
            {
                copy._items = new List<int>(_items);
            }

            return copy;
        }

        #endregion
    }
}
