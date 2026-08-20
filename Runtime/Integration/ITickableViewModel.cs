namespace DM.Integration
{
    /// <summary>
    ///     Вьюмодель, которой нужен пер-кадровый тик (замена Update()-поллинга
    ///     вьюмоделей ReUI). Тикается провайдером, пока вьюмодель выдана виджету.
    /// </summary>
    public interface ITickableViewModel
    {
        void Tick();
    }
}
