using System;
using System.Collections;
using System.Collections.Generic;

namespace OfficeMice.MapGeneration.Pathfinding
{
    /// <summary>
    /// Binary heap implementation of priority queue for A* pathfinding.
    /// Provides O(log n) insertion, removal, and priority update operations.
    /// </summary>
    /// <typeparam name="T">Type that implements IComparable</typeparam>
    internal class PriorityQueue<T> : IEnumerable<T> where T : IComparable<T>
    {
        #region Private Fields
        
        private List<T> _heap;
        private Dictionary<T, int> _itemIndices;
        
        #endregion
        
        #region Properties
        
        public int Count => _heap.Count;
        
        #endregion
        
        #region Constructor
        
        public PriorityQueue()
        {
            _heap = new List<T>();
            _itemIndices = new Dictionary<T, int>();
        }
        
        public PriorityQueue(int capacity)
        {
            _heap = new List<T>(capacity);
            _itemIndices = new Dictionary<T, int>(capacity);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Adds an item to the priority queue.
        /// </summary>
        public void Enqueue(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            
            _heap.Add(item);
            int itemIndex = _heap.Count - 1;
            _itemIndices[item] = itemIndex;
            
            HeapifyUp(itemIndex);
        }
        
        /// <summary>
        /// Removes and returns the item with the highest priority (lowest value).
        /// </summary>
        public T Dequeue()
        {
            if (_heap.Count == 0)
                throw new InvalidOperationException("Priority queue is empty");
            
            T highestPriorityItem = _heap[0];
            
            // Move the last item to the root
            T lastItem = _heap[_heap.Count - 1];
            _heap[0] = lastItem;
            _itemIndices[lastItem] = 0;
            
            _heap.RemoveAt(_heap.Count - 1);
            _itemIndices.Remove(highestPriorityItem);
            
            if (_heap.Count > 0)
            {
                HeapifyDown(0);
            }
            
            return highestPriorityItem;
        }
        
        /// <summary>
        /// Returns the item with the highest priority without removing it.
        /// </summary>
        public T Peek()
        {
            if (_heap.Count == 0)
                throw new InvalidOperationException("Priority queue is empty");
            
            return _heap[0];
        }
        
        /// <summary>
        /// Updates the priority of an existing item in the queue.
        /// </summary>
        public void UpdatePriority(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            
            if (!_itemIndices.TryGetValue(item, out int index))
                throw new ArgumentException("Item not found in priority queue");
            
            // The item's priority has changed, so we need to restore the heap property
            // Try heapifying up first, then down if needed
            int originalIndex = index;
            HeapifyUp(index);
            
            // If the index didn't change during heapify up, try heapify down
            if (_itemIndices[item] == originalIndex)
            {
                HeapifyDown(index);
            }
        }
        
        /// <summary>
        /// Checks if the queue contains the specified item.
        /// </summary>
        public bool Contains(T item)
        {
            return _itemIndices.ContainsKey(item);
        }
        
        /// <summary>
        /// Removes all items from the priority queue.
        /// </summary>
        public void Clear()
        {
            _heap.Clear();
            _itemIndices.Clear();
        }
        
        /// <summary>
        /// Converts the priority queue to an array for debugging purposes.
        /// </summary>
        public T[] ToArray()
        {
            return _heap.ToArray();
        }
        
        #endregion
        
        #region Private Methods
        
        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                
                if (_heap[index].CompareTo(_heap[parentIndex]) >= 0)
                    break;
                
                Swap(index, parentIndex);
                index = parentIndex;
            }
        }
        
        private void HeapifyDown(int index)
        {
            while (true)
            {
                int leftChildIndex = 2 * index + 1;
                int rightChildIndex = 2 * index + 2;
                int smallestChildIndex = index;
                
                // Find the smallest child
                if (leftChildIndex < _heap.Count && 
                    _heap[leftChildIndex].CompareTo(_heap[smallestChildIndex]) < 0)
                {
                    smallestChildIndex = leftChildIndex;
                }
                
                if (rightChildIndex < _heap.Count && 
                    _heap[rightChildIndex].CompareTo(_heap[smallestChildIndex]) < 0)
                {
                    smallestChildIndex = rightChildIndex;
                }
                
                // If the smallest child is smaller than the current node, swap them
                if (smallestChildIndex != index)
                {
                    Swap(index, smallestChildIndex);
                    index = smallestChildIndex;
                }
                else
                {
                    break;
                }
            }
        }
        
        private void Swap(int index1, int index2)
        {
            T item1 = _heap[index1];
            T item2 = _heap[index2];
            
            _heap[index1] = item2;
            _heap[index2] = item1;
            
            _itemIndices[item1] = index2;
            _itemIndices[item2] = index1;
        }
        
        #endregion
        
        #region IEnumerable Implementation
        
        public IEnumerator<T> GetEnumerator()
        {
            // Return a copy to avoid modification during enumeration
            T[] copy = new T[_heap.Count];
            _heap.CopyTo(copy);
            return ((IEnumerable<T>)copy).GetEnumerator();
        }
        
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        #endregion
    }
}