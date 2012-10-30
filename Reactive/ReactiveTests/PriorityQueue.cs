using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactiveTests
{
    class PriorityQueue<T>
    {
        const int defaultSize = 1024;

        Func<T, T, bool> isHigherPriority;
        T[] items;
        int size;

        public PriorityQueue(Func<T, T, bool> isHigherPriority)
        {
            this.isHigherPriority = isHigherPriority;
            this.items = new T[defaultSize];
            this.size = 0;
        }

        void Percolate(int index)
        {
            if (index >= size || index < 0)
                return;
            var parent = (index - 1) / 2;
            if (parent < 0 || parent == index)
                return;

            if (isHigherPriority(items[index], items[parent]))
            {
                var temp = items[index];
                items[index] = items[parent];
                items[parent] = temp;
                Percolate(parent);
            }
        }

        void Heapify()
        {
            Heapify(0);
        }

        void Heapify(int index)
        {
            if (index >= size || index < 0)
                return;

            var left = 2 * index + 1;
            var right = 2 * index + 2;
            var first = index;

            if (left < size && isHigherPriority(items[left], items[first]))
                first = left;
            if (right < size && isHigherPriority(items[right], items[first]))
                first = right;
            if (first != index)
            {
                var temp = items[index];
                items[index] = items[first];
                items[first] = temp;
                Heapify(first);
            }
        }

        public int Count { get { return size; } }

        public T Peek()
        {
            if (size == 0)
                throw new InvalidOperationException("Heap is empty.");

            return items[0];
        }

        public T Dequeue()
        {
            var result = Peek();
            items[0] = items[--size];
            Heapify();
            return result;
        }

        public void Enqueue(T item)
        {
            if (size >= items.Length)
            {
                var temp = items;
                items = new T[items.Length * 2];
                temp.CopyTo(items, 0);
            }

            var index = size++;
            items[index] = item;
            Percolate(index);
        }
    }
}
