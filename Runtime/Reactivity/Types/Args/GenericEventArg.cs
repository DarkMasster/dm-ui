namespace DM.Reactivity
{
    /// <summary>
    ///     Аргумент события с одним значением.
    ///     Структура намеренно: экземпляр не переиспользуется между вызовами,
    ///     поэтому реентрантный обработчик не может испортить аргументы внешнего,
    ///     и при этом нет аллокации на каждое событие.
    /// </summary>
    public readonly struct GenericEventArg<TValue>
    {
        public readonly TValue Value;

        public GenericEventArg(TValue value)
        {
            Value = value;
        }
    }
}
