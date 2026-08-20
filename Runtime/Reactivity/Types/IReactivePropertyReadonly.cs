using System;

namespace DM.Reactivity
{
    public interface IReactivePropertyReadonly<T>
    {
        event Action<T> OnValueChanged;
        event Action<T, T> OnValueChangedExtended;
        
        T Value { get; }
    }
}
