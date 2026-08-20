using System;
using System.Collections.Generic;

namespace DM.Reactivity
{
    public enum TypeDispatchEventMode
    {
        /// <summary>Событие поднимается на каждую запись, даже если значение не изменилось.</summary>
        Always,

        /// <summary>Событие поднимается только при фактическом изменении значения.</summary>
        ValueChange
    }

    public class ReactiveProperty<T> : IReactiveProperty<T>
    {
        private static readonly EqualityComparer<T> Comparer = EqualityComparer<T>.Default;

        private readonly TypeDispatchEventMode _dispatchEventMode;
        private T _value;

        public ReactiveProperty(TypeDispatchEventMode dispatchEventMode = TypeDispatchEventMode.ValueChange)
        {
            _dispatchEventMode = dispatchEventMode;
        }

        public ReactiveProperty(T value, TypeDispatchEventMode dispatchEventMode = TypeDispatchEventMode.ValueChange)
        {
            _value = value;
            _dispatchEventMode = dispatchEventMode;
        }

        public event Action<T> OnValueChanged;
        public event Action<T, T> OnValueChangedExtended;

        public T Value
        {
            get => _value;
            set
            {
                // EqualityComparer<T>.Default вместо _value.Equals(value):
                // не боксит значимые типы и корректно обрабатывает null с обеих сторон.
                if (_dispatchEventMode == TypeDispatchEventMode.ValueChange && Comparer.Equals(_value, value))
                    return;

                var oldValue = _value;
                _value = value;

                OnValueChanged?.Invoke(_value);
                OnValueChangedExtended?.Invoke(oldValue, _value);
            }
        }

        /// <summary>
        ///     Снимает всех подписчиков. Вызывается владельцем свойства при уничтожении,
        ///     чтобы свойство, пережившее подписчиков, не держало их ссылки.
        /// </summary>
        public void ClearSubscribers()
        {
            OnValueChanged = null;
            OnValueChangedExtended = null;
        }

        public override string ToString()
        {
            return _value != null ? _value.ToString() : "NULL value";
        }
    }
}
