namespace DM.Reactivity
{
    /// <summary>
    ///     Аргумент события «ключ-значение» (для списков ключ — это индекс).
    ///     См. комментарий к <see cref="GenericEventArg{TValue}" /> о том, почему это структура.
    /// </summary>
    public readonly struct GenericPairEventArgs<TKey, TValue>
    {
        public readonly TKey Key;
        public readonly TValue Value;

        public GenericPairEventArgs(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
}
