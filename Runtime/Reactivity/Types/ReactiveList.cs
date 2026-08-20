using System;
using System.Collections;
using System.Collections.Generic;

namespace DM.Reactivity
{
    /// <summary>
    ///     Список с событиями об изменениях.
    ///     ВАЖНО: <see cref="Clear" /> поднимает только <see cref="OnClear" /> и НЕ поднимает
    ///     <see cref="OnRemoveItem" /> на каждый элемент. Подписчик, который отслеживает состав списка,
    ///     обязан подписаться и на <see cref="OnClear" />, иначе после Clear его состояние разъедется.
    /// </summary>
    public class ReactiveList<T> : IList<T>, IReactiveListReadOnly<T>
    {
        private readonly List<T> _list;

        public ReactiveList()
            : this(0)
        {
        }

        public ReactiveList(int capacity)
        {
            _list = new List<T>(capacity);
        }

        public int Count => _list.Count;
        public bool IsReadOnly => false;

        public event Action<GenericPairEventArgs<int, T>> OnAddItem;
        public event Action<GenericEventArg<IEnumerable<T>>> OnClear;
        public event Action<GenericPairEventArgs<int, T>> OnElementChange;
        public event Action<GenericPairEventArgs<int, T>> OnRemoveItem;
        public event Action<ReactiveListSortingArgs<T>> OnSort;

        public T this[int index]
        {
            get => _list[index];
            set
            {
                _list[index] = value;
                OnElementChange?.Invoke(new GenericPairEventArgs<int, T>(index, value));
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            _list.Add(item);
            OnAddItem?.Invoke(new GenericPairEventArgs<int, T>(_list.Count - 1, item));
        }

        public void Insert(int index, T item)
        {
            _list.Insert(index, item);
            OnAddItem?.Invoke(new GenericPairEventArgs<int, T>(index, item));
        }

        public bool Remove(T item)
        {
            // Один проход вместо Contains + IndexOf + Remove.
            var index = _list.IndexOf(item);
            if (index < 0) return false;

            _list.RemoveAt(index);
            OnRemoveItem?.Invoke(new GenericPairEventArgs<int, T>(index, item));
            return true;
        }

        public void RemoveAt(int index)
        {
            var removedItem = _list[index];
            _list.RemoveAt(index);
            OnRemoveItem?.Invoke(new GenericPairEventArgs<int, T>(index, removedItem));
        }

        public void RemoveRange(int index, int count)
        {
            if (OnRemoveItem == null)
            {
                _list.RemoveRange(index, count);
                return;
            }

            var removed = _list.GetRange(index, count);
            _list.RemoveRange(index, count);

            for (var i = 0; i < removed.Count; i++)
                OnRemoveItem.Invoke(new GenericPairEventArgs<int, T>(index + i, removed[i]));
        }

        public void Clear()
        {
            if (OnClear == null)
            {
                _list.Clear();
                return;
            }

            var clearedElements = new List<T>(_list);
            _list.Clear();
            OnClear.Invoke(new GenericEventArg<IEnumerable<T>>(clearedElements));
        }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var item in collection) Add(item);
        }

        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public void Sort()
        {
            SortInternal(list => list.Sort());
        }

        public void Sort(IComparer<T> comparer)
        {
            SortInternal(list => list.Sort(comparer));
        }

        public void Sort(Comparison<T> comparison)
        {
            SortInternal(list => list.Sort(comparison));
        }

        /// <summary>Снимает всех подписчиков. Вызывается владельцем списка при уничтожении.</summary>
        public void ClearSubscribers()
        {
            OnAddItem = null;
            OnClear = null;
            OnElementChange = null;
            OnRemoveItem = null;
            OnSort = null;
        }

        private void SortInternal(Action<List<T>> sort)
        {
            // Без подписчиков не платим ни за снимок, ни за диффинг.
            if (OnSort == null)
            {
                sort(_list);
                return;
            }

            var beforeSort = new T[_list.Count];
            _list.CopyTo(beforeSort);

            sort(_list);

            // Раньше здесь был IndexOf внутри цикла — O(n^2) на каждую сортировку.
            // Индекс после сортировки строится один раз за O(n).
            var newIndices = new Dictionary<T, int>(_list.Count);
            for (var i = 0; i < _list.Count; i++)
            {
                var item = _list[i];
                if (item == null) continue; // null не может быть ключом словаря
                newIndices[item] = i;
            }

            for (var oldIndex = 0; oldIndex < beforeSort.Length; oldIndex++)
            {
                var value = beforeSort[oldIndex];
                if (value == null) continue;
                if (!newIndices.TryGetValue(value, out var newIndex)) continue;
                if (newIndex == oldIndex) continue;

                OnSort.Invoke(new ReactiveListSortingArgs<T>(oldIndex, newIndex, value));
            }
        }
    }
}
