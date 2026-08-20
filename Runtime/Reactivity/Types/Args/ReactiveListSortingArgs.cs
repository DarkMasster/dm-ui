namespace DM.Reactivity
{
    /// <summary>
    ///     Описывает перемещение одного элемента при сортировке списка.
    ///     <see cref="NewIndex" /> — индекс в уже отсортированном списке.
    /// </summary>
    public readonly struct ReactiveListSortingArgs<TValue>
    {
        public readonly int OldIndex;
        public readonly int NewIndex;
        public readonly TValue Value;

        public ReactiveListSortingArgs(int oldIndex, int newIndex, TValue value)
        {
            OldIndex = oldIndex;
            NewIndex = newIndex;
            Value = value;
        }
    }
}
