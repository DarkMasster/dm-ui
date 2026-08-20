using System;
using System.Collections.Generic;
using DM.UI;
using VContainer;

namespace DM.Integration
{
    /// <summary>
    ///     Вьюмодель-провайдер поверх VContainer. Экземпляры резолвятся из контейнера
    ///     (регистрировать транзиентами), закрытые вьюмодели с <see cref="IResettableViewModel" />
    ///     переиспользуются через пул по типу, остальные диспозятся. Выданные вьюмодели
    ///     с <see cref="ITickableViewModel" /> тикаются раз в кадр: <see cref="Tick" />
    ///     подписывается на непаузируемый IUpdateSender при инициализации UI —
    ///     сюда переезжает Update()-поллинг вьюмоделей ReUI.
    /// </summary>
    public sealed class VContainerViewModelProvider : IViewModelProvider
    {
        private readonly IObjectResolver _container;
        private readonly Dictionary<Type, Stack<IViewModel>> _pool = new();
        private readonly List<ITickableViewModel> _activeTickables = new();

        // Обход по буферу: вьюмодель во время тика может закрыть виджет
        // и вернуть себя (или соседа) в провайдер прямо посреди обхода.
        private readonly List<ITickableViewModel> _tickBuffer = new();

        public VContainerViewModelProvider( IObjectResolver container )
        {
            _container = container ?? throw new ArgumentNullException( nameof( container ) );
        }

        public IViewModel Get( Type viewModelType )
        {
            if ( viewModelType == null )
                throw new ArgumentNullException( nameof( viewModelType ) );

            IViewModel viewModel =
                _pool.TryGetValue( viewModelType, out Stack<IViewModel> stack ) && stack.Count > 0
                    ? stack.Pop()
                    : (IViewModel)_container.Resolve( viewModelType );

            if ( viewModel is ITickableViewModel tickable )
                _activeTickables.Add( tickable );

            return viewModel;
        }

        public void Return( IViewModel viewModel )
        {
            if ( viewModel == null )
                return;

            if ( viewModel is ITickableViewModel tickable )
                _activeTickables.Remove( tickable );

            if ( viewModel is IResettableViewModel resettable )
            {
                resettable.Reset();
                Type type = viewModel.GetType();
                if ( !_pool.TryGetValue( type, out Stack<IViewModel> stack ) )
                {
                    stack = new Stack<IViewModel>();
                    _pool[ type ] = stack;
                }
                stack.Push( viewModel );
                return;
            }

            viewModel.Dispose();
        }

        public void Tick()
        {
            if ( _activeTickables.Count == 0 )
                return;

            _tickBuffer.Clear();
            _tickBuffer.AddRange( _activeTickables );
            foreach ( ITickableViewModel tickable in _tickBuffer )
                tickable.Tick();
        }
    }
}
