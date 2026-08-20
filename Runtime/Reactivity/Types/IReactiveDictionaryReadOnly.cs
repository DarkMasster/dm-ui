using System;
using System.Collections.Generic;

namespace DM.Reactivity
{
	public interface IReactiveDictionaryReadOnly<TKey, TValue>
		: IEnumerable<KeyValuePair<TKey, TValue>>
	{
		event Action<GenericPairEventArgs<TKey, TValue>> OnAddItem;
		event Action<GenericEventArg<IDictionary<TKey, TValue>>> OnClear;
		event Action<GenericPairEventArgs<TKey, TValue>> OnElementChange;
		event Action<GenericPairEventArgs<TKey, TValue>> OnRemoveItem;
		
		TValue this[TKey key] { get; }

		int Count { get; }
		bool Contains(KeyValuePair<TKey, TValue> item);
		bool TryGetValue(TKey key, out TValue value);
		TValue GetSafe(TKey key);
	}
}
