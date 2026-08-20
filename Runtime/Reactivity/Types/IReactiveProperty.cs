namespace DM.Reactivity
{
    public interface IReactiveProperty<T> : IReactivePropertyReadonly<T>
    {
        T Value { get; set; }
    }
}
