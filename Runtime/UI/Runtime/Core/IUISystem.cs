using UnityEngine;

namespace DM.UI
{
    public interface IUISystem
    {
        /// <summary>Собирает биндинги виджетов. Должен быть вызван до первого Open.</summary>
        void Initialize();

        TWidget Open<TWidget>(IViewModel viewModel = null, string layoutId = null, bool animated = true)
            where TWidget : Widget, new();

        void Close(Widget widget, bool animated = true);

        internal TWidget Create<TWidget>(
            IViewModel viewModel = null,
            UILayout layout = null,
            string layoutId = null,
            Transform mountingPoint = null) where TWidget : Widget, new();

        internal void ReturnLayout(UILayout layout);

        internal void ReturnViewModel(IViewModel viewModel);
    }
}
