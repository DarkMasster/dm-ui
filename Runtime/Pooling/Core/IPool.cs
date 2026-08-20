namespace DM.Pooling
{
    public interface IPool<T> where T : class
    {
        T Get(string id);
        bool TryReturnToPool(T obj);
    }
}