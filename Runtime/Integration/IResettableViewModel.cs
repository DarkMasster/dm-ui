namespace DM.Integration
{
    /// <summary>
    ///     Вьюмодель, пригодная к переиспользованию через пул провайдера.
    ///     Reset вызывается при возврате; без этого интерфейса вьюмодель
    ///     не пулится, а диспозится.
    /// </summary>
    public interface IResettableViewModel
    {
        void Reset();
    }
}
