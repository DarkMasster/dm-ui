using System;
using System.Collections;
using System.Collections.Generic;

namespace DM.Reactivity
{
    /// <summary>
    ///     Словарь с событиями об изменениях.
    ///     ВАЖНО: <see cref="Clear" /> поднимает только <see cref="OnClear" /> и НЕ поднимает
    ///     <see cref="OnRemoveItem" /> на каждый элемент — подписчик обязан обрабатывать оба события.
    ///     <see cref="AddRange" />, в отличие от прежней версии, поднимает <see cref="OnAddItem" />
    ///     на каждый добавленный элемент, поэтому пакетное добавление больше не «теряется».
    /// </summary>
    public class ReactiveDictionary<TKey, TValue> : IDictionary<TKey, TValue>,
        IReactiveDictionaryReadOnly<TKey, TValue>
    {
        private static readonly EqualityComparer<TValue> ValueComparer = EqualityComparer<TValue>.Default;

        private readonly Dictionary<TKey, TValue> _dictionary = new();

        // Пер-ключевые подписки: позволяют слушать конкретный элемент без фильтрации всех событий.
        private Dictionary<TKey, List<Action<GenericPairEventArgs<TKey, TValue>>>> _onAddItemHandlers;
        private Dictionary<TKey, List<Action<GenericPairEventArgs<TKey, TValue>>>> _onChangeItemHandlers;
        private Dictionary<TKey, List<Action<GenericPairEventArgs<TKey, TValue>>>> _onRemoveItemHandlers;

        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;
        public ICollection<TKey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;

        public event Action<GenericPairEventArgs<TKey, TValue>> OnAddItem;
        public event Action<GenericEventArg<IDictionary<TKey, TValue>>> OnClear;
        public event Action<GenericPairEventArgs<TKey, TValue>> OnElementChange;
        public event Action<GenericPairEventArgs<TKey, TValue>> OnRemoveItem;

        public TValue this[TKey key]
        {
            get
            {
                if (_dictionary.TryGetValue(key, out var value)) return value;
                throw new KeyNotFoundException($"{GetType().Name}: key not found: {key}");
            }
            set
            {
                if (_dictionary.TryGetValue(key, out var currentValue))
                {
                    // Comparer вместо currentValue.Equals(value): не падает на null и не боксит.
                    if (ValueComparer.Equals(currentValue, value)) return;

                    _dictionary[key] = value;
                    FireOnChangeItem(key, value);
                }
                else
                {
                    _dictionary[key] = value;
                    FireOnAddItem(key, value);
                }
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
            FireOnAddItem(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void AddRange(IDictionary<TKey, TValue> dictionary)
        {
            foreach (var pair in dictionary) Add(pair.Key, pair.Value);
        }

        public bool Remove(TKey key)
        {
            if (!_dictionary.TryGetValue(key, out var removedValue)) return false;

            _dictionary.Remove(key);
            FireOnRemoveItem(key, removedValue);
            return true;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!Contains(item)) return false;
            return Remove(item.Key);
        }

        public void Clear()
        {
            if (OnClear == null)
            {
                _dictionary.Clear();
                return;
            }

            var oldDictionary = new Dictionary<TKey, TValue>(_dictionary);
            _dictionary.Clear();
            OnClear.Invoke(new GenericEventArg<IDictionary<TKey, TValue>>(oldDictionary));
        }

        /// <summary>Сравнивает и ключ, и значение — в отличие от прежней версии, которая смотрела только ключ.</summary>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.TryGetValue(item.Key, out var value) && ValueComparer.Equals(value, item.Value);
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        /// <summary>Возвращает default вместо исключения, если ключ не найден.</summary>
        public TValue GetSafe(TKey key)
        {
            _dictionary.TryGetValue(key, out var value);
            return value;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            // Раньше здесь был NotImplementedException — использовать словарь как IDictionary было нельзя.
            ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);
        }

        public IDisposable SubscribeOnAddItem(TKey key, Action<GenericPairEventArgs<TKey, TValue>> handler)
        {
            return SubscribePerKey(ref _onAddItemHandlers, key, handler);
        }

        public IDisposable SubscribeOnChangeItem(TKey key, Action<GenericPairEventArgs<TKey, TValue>> handler)
        {
            return SubscribePerKey(ref _onChangeItemHandlers, key, handler);
        }

        public IDisposable SubscribeOnRemoveItem(TKey key, Action<GenericPairEventArgs<TKey, TValue>> handler)
        {
            return SubscribePerKey(ref _onRemoveItemHandlers, key, handler);
        }

        /// <summary>Снимает всех подписчиков. Вызывается владельцем словаря при уничтожении.</summary>
        public void ClearSubscribers()
        {
            OnAddItem = null;
            OnClear = null;
            OnElementChange = null;
            OnRemoveItem = null;
            _onAddItemHandlers = null;
            _onChangeItemHandlers = null;
            _onRemoveItemHandlers = null;
        }

        private static IDisposable SubscribePerKey(
            ref Dictionary<TKey, List<Action<GenericPairEventArgs<TKey, TValue>>>> storage,
            TKey key,
            Action<GenericPairEventArgs<TKey, TValue>> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            storage ??= new Dictionary<TKey, List<Action<GenericPairEventArgs<TKey, TValue>>>>();

            if (!storage.TryGetValue(key, out var handlers))
            {
                handlers = new List<Action<GenericPairEventArgs<TKey, TValue>>>();
                storage[key] = handlers;
            }

            if (!handlers.Contains(handler)) handlers.Add(handler);

            // Возвращаем IDisposable, а не «не забудь вызвать Unsubscribe» — как и остальные подписки модуля.
            return Subscription.Create(() => handlers.Remove(handler));
        }

        private void FireOnAddItem(TKey key, TValue value)
        {
            var args = new GenericPairEventArgs<TKey, TValue>(key, value);
            OnAddItem?.Invoke(args);
            FirePerKey(_onAddItemHandlers, key, args);
        }

        private void FireOnChangeItem(TKey key, TValue value)
        {
            var args = new GenericPairEventArgs<TKey, TValue>(key, value);
            OnElementChange?.Invoke(args);
            FirePerKey(_onChangeItemHandlers, key, args);
        }

        private void FireOnRemoveItem(TKey key, TValue value)
        {
            var args = new GenericPairEventArgs<TKey, TValue>(key, value);
            OnRemoveItem?.Invoke(args);
            FirePerKey(_onRemoveItemHandlers, key, args);
        }

        private static void FirePerKey(
            Dictionary<TKey, List<Action<GenericPairEventArgs<TKey, TValue>>>> storage,
            TKey key,
            GenericPairEventArgs<TKey, TValue> args)
        {
            if (storage == null || !storage.TryGetValue(key, out var handlers)) return;

            // Обход по снимку: обработчик имеет право отписаться прямо во время вызова.
            for (var i = handlers.Count - 1; i >= 0; i--) handlers[i].Invoke(args);
        }
    }
}
