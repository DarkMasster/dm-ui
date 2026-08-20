using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DM.UI
{
    public class UISystem : IUISystem
    {
        private readonly Dictionary<Type, WidgetBinding> _bindings = new();
        private readonly HashSet<Widget> _openedWidgets = new();
        private readonly UILayoutsPool _pool;
        private readonly IViewModelProvider _viewModelProvider;
        private readonly Transform _mountPoint;

        private bool _isInitialized;

        public UISystem(
            Transform mountPoint,
            UILayoutsPool pool,
            IViewModelProvider viewModelProvider)
        {
            _mountPoint = mountPoint != null ? mountPoint : throw new ArgumentNullException(nameof(mountPoint));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _viewModelProvider = viewModelProvider ?? throw new ArgumentNullException(nameof(viewModelProvider));
        }

        /// <summary>Виджеты, открытые через <see cref="Open{TWidget}" /> и ещё не закрытые.</summary>
        public IReadOnlyCollection<Widget> OpenedWidgets => _openedWidgets;

        public void Initialize()
        {
            Initialize(null);
        }

        /// <summary>
        ///     Сканирует сборки на предмет <see cref="DefaultWidgetBindingAttribute" />.
        ///     Передайте список сборок, чтобы не обходить весь AppDomain на старте.
        /// </summary>
        public void Initialize(IEnumerable<Assembly> assembliesToScan)
        {
            if (_isInitialized) return;
            _isInitialized = true;

            _bindings.Clear();
            foreach (var binding in WidgetBinding.GetBindings(assembliesToScan))
                _bindings[binding.WidgetType] = binding;
        }

        public TWidget Open<TWidget>(IViewModel viewModel = null, string layoutId = null, bool animated = true)
            where TWidget : Widget, new()
        {
            var widget = Create<TWidget>(viewModel, null, layoutId, null);

            widget.OnCloseRequested += Close;
            _openedWidgets.Add(widget);

            widget.Initialize();
            // Forget(): открытие анимируется в фоне, но исключения анимации больше не проглатываются.
            widget.Open(animated).Forget();

            return widget;
        }

        public void Close(Widget widget, bool animated = true)
        {
            if (widget == null || !_openedWidgets.Remove(widget)) return;

            widget.OnCloseRequested -= Close;
            widget.DeInitialize();

            // Всё поддерево возвращается в пул ПОСЛЕ close-анимаций: дети
            // остаются в дереве до конца закрытия и получают свой OnClose.
            widget.Close(animated).ContinueWith(widget.ReturnTreeToPool).Forget();
        }

        /// <summary>Закрывает все открытые виджеты — например, при выгрузке сцены.</summary>
        public void CloseAll(bool animated = false)
        {
            foreach (var widget in _openedWidgets.ToArray()) Close(widget, animated);
        }

        TWidget IUISystem.Create<TWidget>(IViewModel viewModel, UILayout layout, string layoutId,
            Transform mountingPoint)
        {
            return Create<TWidget>(viewModel, layout, layoutId, mountingPoint);
        }

        void IUISystem.ReturnLayout(UILayout layout)
        {
            ReturnLayout(layout);
        }

        void IUISystem.ReturnViewModel(IViewModel viewModel)
        {
            ReturnViewModel(viewModel);
        }

        private void ReturnLayout(UILayout layout)
        {
            if (layout == null) return;
            if (_pool.TryReturnToPool(layout)) layout.Restore();
        }

        private void ReturnViewModel(IViewModel viewModel)
        {
            if (viewModel == null) return;
            _viewModelProvider.Return(viewModel);
        }

        private TWidget Create<TWidget>(
            IViewModel viewModel,
            UILayout layout,
            string layoutId,
            Transform mountingPoint) where TWidget : Widget, new()
        {
            if (!_isInitialized)
                throw new UISystemException(
                    $"{nameof(UISystem)} is not initialized. Call {nameof(Initialize)}() before opening widgets.");

            var currentMountingPoint = mountingPoint ?? _mountPoint;
            _bindings.TryGetValue(typeof(TWidget), out var binding);

            var currentViewModel = viewModel ?? ResolveViewModel<TWidget>(binding);
            var currentLayout = layout ?? ResolveLayout<TWidget>(binding, layoutId, currentMountingPoint);

            var widget = new TWidget();
            widget.Construct(currentLayout, currentViewModel, this);

            return widget;
        }

        private IViewModel ResolveViewModel<TWidget>(WidgetBinding binding) where TWidget : Widget, new()
        {
            if (binding?.ViewModelType == null)
                // Сообщение было инвертировано по смыслу («Binding for view model found»).
                throw new UISystemException(
                    $"Can't resolve view model for widget of type {typeof(TWidget).Name}: no view model binding found. " +
                    $"Either pass a view model explicitly or declare [{nameof(DefaultWidgetBindingAttribute)}] on the widget.");

            var viewModel = _viewModelProvider.Get(binding.ViewModelType);

            if (viewModel == null)
                throw new UISystemException(
                    $"Can't resolve view model of type {binding.ViewModelType}. " +
                    $"View model provider {_viewModelProvider.GetType().Name} returned null.");

            return viewModel;
        }

        private UILayout ResolveLayout<TWidget>(WidgetBinding binding, string layoutId, Transform mountingPoint)
            where TWidget : Widget, new()
        {
            var currentId = layoutId ?? binding?.LayoutId;

            if (currentId == null)
                throw new UISystemException(
                    $"Can't resolve layout for widget of type {typeof(TWidget).Name}: no layout binding found. " +
                    $"Either pass a layout/layoutId explicitly or declare [{nameof(DefaultWidgetBindingAttribute)}] on the widget.");

            var layout = _pool.Get(currentId);

            if (layout == null)
                throw new UISystemException(
                    $"Can't instantiate layout with id '{currentId}'. Pool returned null. " +
                    "Check that a layout with such id is registered in the layouts provider.");

            layout.transform.SetParent(mountingPoint, false);

            return layout;
        }
    }
}
