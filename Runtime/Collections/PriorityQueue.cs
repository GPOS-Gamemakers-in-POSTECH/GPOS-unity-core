using System;
using System.Collections.Generic;

namespace GPOS.Core.Collections
{
    /// <summary>
    /// Binary-heap priority queue. Defaults to a min-heap (smallest element dequeued first);
    /// pass a custom <see cref="IComparer{T}"/> to change the ordering.
    /// Provided because <c>System.Collections.Generic.PriorityQueue</c> is unavailable on the
    /// .NET Standard 2.1 runtime Unity uses.
    /// </summary>
    public class PriorityQueue<T>
    {
        private readonly List<T> _heap;
        private readonly IComparer<T> _comparer;

        public PriorityQueue(IComparer<T> comparer = null)
        {
            _heap = new List<T>();
            _comparer = comparer ?? Comparer<T>.Default;
        }

        public int Count => _heap.Count;

        /// <summary>Adds an item, ordering it against the rest of the queue.</summary>
        public void Enqueue(T item)
        {
            _heap.Add(item);
            SiftUp(_heap.Count - 1);
        }

        /// <summary>Removes and returns the highest-priority item.</summary>
        public T Dequeue()
        {
            if (_heap.Count == 0)
                throw new InvalidOperationException("[PriorityQueue] Queue is empty.");

            T root = _heap[0];
            int last = _heap.Count - 1;
            _heap[0] = _heap[last];
            _heap.RemoveAt(last);

            if (_heap.Count > 0)
                SiftDown(0);

            return root;
        }

        /// <summary>Removes and returns the highest-priority item without throwing when empty.</summary>
        public bool TryDequeue(out T item)
        {
            if (_heap.Count == 0)
            {
                item = default;
                return false;
            }

            item = Dequeue();
            return true;
        }

        /// <summary>Returns the highest-priority item without removing it.</summary>
        public T Peek()
        {
            if (_heap.Count == 0)
                throw new InvalidOperationException("[PriorityQueue] Queue is empty.");

            return _heap[0];
        }

        public bool TryPeek(out T item)
        {
            if (_heap.Count == 0)
            {
                item = default;
                return false;
            }

            item = _heap[0];
            return true;
        }

        public void Clear() => _heap.Clear();

        private void SiftUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_comparer.Compare(_heap[index], _heap[parent]) >= 0)
                    break;

                Swap(index, parent);
                index = parent;
            }
        }

        private void SiftDown(int index)
        {
            int count = _heap.Count;
            while (true)
            {
                int left = index * 2 + 1;
                int right = index * 2 + 2;
                int smallest = index;

                if (left < count && _comparer.Compare(_heap[left], _heap[smallest]) < 0)
                    smallest = left;
                if (right < count && _comparer.Compare(_heap[right], _heap[smallest]) < 0)
                    smallest = right;

                if (smallest == index)
                    break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            (_heap[a], _heap[b]) = (_heap[b], _heap[a]);
        }
    }
}
