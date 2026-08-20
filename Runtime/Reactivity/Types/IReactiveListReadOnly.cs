using System;
using System.Collections.Generic;

namespace DM.Reactivity
{
	public interface IReactiveListReadOnly<T> : IEnumerable<T>
	{
		event Action<GenericPairEventArgs<int, T>> OnAddItem;
		event Action<GenericEventArg<IEnumerable<T>>> OnClear;
		event Action<GenericPairEventArgs<int, T>> OnElementChange;
		event Action<GenericPairEventArgs<int, T>> OnRemoveItem;
		event Action<ReactiveListSortingArgs<T>> OnSort;
		
		int Count { get; }
		T this[int index] { get; }
		int IndexOf(T item);
	}
}
