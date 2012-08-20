using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.Serialization;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    ///   Represents a player's inventory, with a list of items in it.
    /// </summary>
    public sealed class Inventory : Component, IList<int>
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Properties

        /// <summary>
        ///   Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" /> .
        /// </summary>
        /// <returns> The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" /> . </returns>
        public int Count
        {
            get
            {
                if (!_isFixed)
                {
                    // Dynamic length, no gaps to use actual count.
                    return _items.Count;
                }
                else
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
            }
        }

        /// <summary>
        ///   Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        /// <returns> true if the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, false. </returns>
        public bool IsReadOnly
        {
            get { return _isFixed; }
        }

        /// <summary>
        ///   A fixed capacity for this inventory. If set, there may be gaps in the list. Disable by setting it to zero.
        /// </summary>
        /// <remarks>
        ///   Important: unlike the <c>List</c> s capacity, e.g., this capacity is fixed after it is set (unless it is set to zero).
        /// </remarks>
        public int Capacity
        {
            get { return _items.Count; }
            set
            {
                if (_items.Count > value)
                {
                    // Remove items that are out of bounds after fixing.
                    for (var i = _items.Count - 1; i >= value; --i)
                    {
                        var itemUid = _items[i];
                        if (itemUid > 0)
                        {
                            // This will, via messaging, also remove the item
                            // from the list, so the following capacity change
                            // is safe.
                            Manager.RemoveEntity(itemUid);
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

                // Fill up with empty slots.
                while (_items.Capacity > _items.Count)
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
        ///   A list of items currently in this inventory.
        /// </summary>
        private readonly List<int> _items = new List<int>();

        /// <summary>
        ///   Whether we have a fixed length list.
        /// </summary>
        private bool _isFixed;

        #endregion

        #region Initialization

        /// <summary>
        ///   Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other"> The component to copy the values from. </param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherInventory = (Inventory) other;
            _items.AddRange(otherInventory._items);
            _isFixed = otherInventory._isFixed;

            return this;
        }

        /// <summary>
        ///   Initialize with a fixed capacity.
        /// </summary>
        /// <param name="fixedCapacity"> The capacity of the inventory. </param>
        public Inventory Initialize(int fixedCapacity)
        {
            Capacity = fixedCapacity;

            return this;
        }

        /// <summary>
        ///   Reset the component to its initial state, so that it may be reused without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            _items.Clear();
            _isFixed = false;
        }

        #endregion

        #region List interface

        /// <summary>
        ///   Gets or sets the element at the specified index.
        /// </summary>
        /// <returns> The element at the specified index. </returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///   <paramref name="index" />
        ///   is not a valid index in the
        ///   <see cref="T:System.Collections.Generic.IList`1" />
        ///   .</exception>
        /// <exception cref="T:System.NotSupportedException">The property is set and the
        ///   <see cref="T:System.Collections.Generic.IList`1" />
        ///   is read-only.</exception>
        public int this[int index]
        {
            get
            {
                // Check for null entries (entity manager throws for unknown
                // values, as it should).
                if (_isFixed && _items[index] <= 0)
                {
                    return 0;
                }
                return _items[index];
            }
            set { _items[index] = value; }
        }

        /// <summary>
        ///   Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" /> .
        /// </summary>
        /// <param name="item"> The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" /> . </param>
        /// <exception cref="T:System.NotSupportedException">The
        ///   <see cref="T:System.Collections.Generic.ICollection`1" />
        ///   is read-only.</exception>
        /// <exception cref="T:System.InvalidOperationException">The fixed length inventory is already full.</exception>
        public void Add(int item)
        {
            // Check if the given id is valid.
            if (item <= 0)
            {
                throw new ArgumentNullException("item", "Item must not be null.");
            }

            // Check if its really an item.
            var itemType = ((Item)Manager.GetComponent(item, Item.TypeId));
            if (itemType == null)
            {
                throw new ArgumentException("Entity does not have an Item component.", "item");
            }

            // If the item is stackable, see if we already have a stack we can
            // add it on top of.
            var stackable = ((Stackable)Manager.GetComponent(item, Stackable.TypeId));
            if (stackable != null)
            {
                for (var i = 0; i < _items.Count; i++)
                {
                    var itemEntry = _items[i];
                    if (itemEntry <= 0)
                    {
                        continue;
                    }

                    var otherItemType = ((Item)Manager.GetComponent(itemEntry, Item.TypeId));
                    var otherStackable = ((Stackable)Manager.GetComponent(itemEntry, Stackable.TypeId));
                    if (otherStackable != null &&
                        otherItemType.Name.Equals(itemType.Name) &&
                        otherStackable.Count < otherStackable.MaxCount)
                    {
                        // Found a non-full stack of matching type, add as many
                        // as possible.
                        var toAdd = System.Math.Min(otherStackable.MaxCount - otherStackable.Count, stackable.Count);
                        otherStackable.Count += toAdd;
                        stackable.Count -= toAdd;

                        // We done yet?
                        if (stackable.Count == 0)
                        {
                            // Yes and the stack was used up, delete it.
                            Manager.RemoveEntity(item);
                            return;
                        } // ... else we continue in search of the next stack.
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
                for (var i = 0; i < _items.Count; ++i)
                {
                    if (_items[i] > 0)
                    {
                        continue;
                    }

                    // Found one, store it.
                    _items[i] = item;
                    return;
                }

                // No free slot found!
                throw new InvalidOperationException("Inventory full.");
            }
            else
            {
                // Just append.
                _items.Add(item);
            }
        }

        /// <summary>
        ///   Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" /> .
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The
        ///   <see cref="T:System.Collections.Generic.ICollection`1" />
        ///   is read-only.</exception>
        public void Clear()
        {
            _items.Clear();
        }

        /// <summary>
        ///   Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
        /// </summary>
        /// <param name="item"> The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" /> . </param>
        /// <returns> true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" /> ; otherwise, false. </returns>
        public bool Contains(int item)
        {
            return _items.Contains(item);
        }

        /// <summary>
        ///   Copies to.
        /// </summary>
        /// <param name="array"> The array. </param>
        /// <param name="arrayIndex"> Index of the array. </param>
        public void CopyTo(int[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        /// <summary>
        ///   Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1" /> .
        /// </summary>
        /// <param name="item"> The object to locate in the <see cref="T:System.Collections.Generic.IList`1" /> . </param>
        /// <returns> The index of <paramref name="item" /> if found in the list; otherwise, -1. </returns>
        public int IndexOf(int item)
        {
            return _items.IndexOf(item);
        }

        /// <summary>
        ///   Inserts an item to the <see cref="T:System.Collections.Generic.IList`1" /> at the specified index.
        /// </summary>
        /// <param name="index"> The zero-based index at which <paramref name="item" /> should be inserted. </param>
        /// <param name="item"> The object to insert into the <see cref="T:System.Collections.Generic.IList`1" /> . </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///   <paramref name="index" />
        ///   is not a valid index in the
        ///   <see cref="T:System.Collections.Generic.IList`1" />
        ///   .</exception>
        /// <exception cref="T:System.NotSupportedException">The
        ///   <see cref="T:System.Collections.Generic.IList`1" />
        ///   is read-only.</exception>
        /// <exception cref="System.NotSupportedException">If the inventory is of fixed length.</exception>
        public void Insert(int index, int item)
        {
            if (_isFixed)
            {
                this[index] = item;
            }
            else
            {
                _items.Insert(index, item);
            }
        }

        /// <summary>
        ///   Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" /> .
        /// </summary>
        /// <param name="item"> The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" /> . </param>
        /// <returns> true if <paramref name="item" /> was successfully removed from the <see
        ///    cref="T:System.Collections.Generic.ICollection`1" /> ; otherwise, false. This method also returns false if <paramref
        ///    name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" /> . </returns>
        /// <exception cref="T:System.NotSupportedException">The
        ///   <see cref="T:System.Collections.Generic.ICollection`1" />
        ///   is read-only.</exception>
        public bool Remove(int item)
        {
            // Avoid null.
            if (item <= 0)
            {
                throw new ArgumentNullException("item", "Item must not be null.");
            }

            if (_isFixed)
            {
                // Find where the item sits, then null the entry.
                var index = IndexOf(item);
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
                return _items.Remove(item);
            }
        }

        /// <summary>
        ///   Removes the <see cref="T:System.Collections.Generic.IList`1" /> item at the specified index.
        /// </summary>
        /// <param name="index"> The zero-based index of the item to remove. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///   <paramref name="index" />
        ///   is not a valid index in the
        ///   <see cref="T:System.Collections.Generic.IList`1" />
        ///   .</exception>
        /// <exception cref="T:System.NotSupportedException">The
        ///   <see cref="T:System.Collections.Generic.IList`1" />
        ///   is read-only.</exception>
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
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns> A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection. </returns>
        public IEnumerator<int> GetEnumerator()
        {
            if (_isFixed)
            {
                for (var i = 0; i < _items.Count; i++)
                {
                    if (_items[i] > 0)
                    {
                        yield return _items[i];
                    }
                }
            }
            else
            {
                for (var i = 0; i < _items.Count; i++)
                {
                    yield return _items[i];
                }
            }
        }

        /// <summary>
        ///   Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns> An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection. </returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///   Swap two items in the list.
        /// </summary>
        /// <param name="firstIndex"> The first index involved. </param>
        /// <param name="secondIndex"> The second index involved. </param>
        public void Swap(int firstIndex, int secondIndex)
        {
            var tmp = _items[firstIndex];
            _items[firstIndex] = _items[secondIndex];
            _items[secondIndex] = tmp;
        }

        #endregion

        #region Serialization

        /// <summary>
        ///   Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet"> The packet to write the data to. </param>
        /// <returns> The packet after writing. </returns>
        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet);

            // Write total capacity.
            packet.Write(_items.Count);

            // Write number of actual items.
            var count = 0;
            for (var i = 0; i < _items.Count; i++)
            {
                if (_items[i] > 0)
                {
                    ++count;
                }
            }
            packet.Write(count);

            // Write actual item ids with their positions.
            for (var i = 0; i < _items.Count; i++)
            {
                var itemEntry = _items[i];
                if (itemEntry <= 0)
                {
                    continue;
                }

                packet.Write(i);
                packet.Write(itemEntry);
            }

            packet.Write(_isFixed);

            return packet;
        }

        /// <summary>
        ///   Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet"> The packet to read from. </param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _items.Clear();
            var capacity = packet.ReadInt32();
            _items.Capacity = capacity;
            for (var i = 0; i < capacity; i++)
            {
                _items.Add(0);
            }

            var numItems = packet.ReadInt32();
            for (var i = 0; i < numItems; i++)
            {
                var index = packet.ReadInt32();
                _items[index] = packet.ReadInt32();
            }

            _isFixed = packet.ReadBoolean();
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", Count=" + Count + ", Capacity=" + Capacity;
        }

        #endregion
    }
}