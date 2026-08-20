using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DM.UI
{
    public abstract class Widget<TLayout, TViewModel> : Widget
        where TLayout : UILayout
        where TViewModel : IViewModel
    {
        protected TLayout Layout { get; private set; }
        protected TViewModel ViewModel { get; private set; }

        protected override void OnConstruct(UILayout layout, IViewModel viewModel)
        {
            if (layout is TLayout tLayout)
                Layout = tLayout;
            else
                throw new ArgumentException($"Expected layout of type {typeof(TLayout)} but got {layout.GetType()}");

            if (viewModel is TViewModel tViewModel)
                ViewModel = tViewModel;
            else
                throw new ArgumentException(
                    $"Expected view model of type {typeof(TViewModel)} but got {viewModel?.GetType().ToString() ?? "null"}");
        }
    }

    public abstract class Widget
    {
        private readonly HashSet<Widget> _children = new();
        private readonly List<IDisposable> _disposables = new();

        private bool _isInitialized;
        private bool _isClosed;
        private bool _isReturnedToPool;
        private IUISystem _uiSystem;

        internal Action<Widget, bool> OnCloseRequested;

        public UILayout LayoutUntyped { get; private set; }
        public IViewModel ViewModelUntyped { get; private set; }
        protected IReadOnlyCollection<Widget> Children => _children;

        /// <summary>
        ///     Регистрирует подписку, которая будет освобождена в DeInitialize.
        ///     Для UnityEvent-ов используйте Subscription.Create из модуля DM.Reactivity —
        ///     иначе слушатели переживут виджет и продублируются, когда лэйаут вернётся из пула.
        /// </summary>
        protected void AddDisposable(IDisposable disposable)
        {
            if (disposable == null) return;
            _disposables.Add(disposable);
        }

        protected abstract void OnConstruct(UILayout layout, IViewModel viewModel);

        protected virtual void OnInitialize()
        {
        }

        protected virtual void OnDeInitialize()
        {
        }

        protected virtual void OnOpen()
        {
        }

        protected virtual void OnClose()
        {
        }

        internal void Construct(UILayout layout, IViewModel viewModel, IUISystem uiSystem)
        {
            _uiSystem = uiSystem;
            ViewModelUntyped = viewModel;
            LayoutUntyped = layout;
            OnConstruct(layout, viewModel);
            layout.Initialize();
        }

        internal void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            OnInitialize();

            // Снимок: OnInitialize мог создать детей, и дети могут создавать своих.
            foreach (var child in _children.ToArray()) child.Initialize();
        }

        internal void DeInitialize()
        {
            // Идемпотентность: DeInitialize может прийти и от родителя, и от UISystem.
            if (!_isInitialized) return;
            _isInitialized = false;

            OnDeInitialize();

            // Дети деинициализируются с родителем, но их ресурсы возвращаются
            // ПОЗЖЕ, через ReturnTreeToPool после close-анимаций: раньше дети
            // уходили в стэш до первого кадра анимации родителя и не получали
            // OnClose (ревью UI-миграции, P2).
            foreach (var child in _children.ToArray())
            {
                child.DeInitialize();
                child.OnCloseRequested -= OnChildRequestedClose;
            }

            foreach (var disposable in _disposables) disposable.Dispose();
            _disposables.Clear();
        }

        /// <summary>
        ///     Возвращает ресурсы всего поддерева (лэйауты и вьюмодели, дети
        ///     первыми) в пул. Вызывается владельцем закрытия ПОСЛЕ завершения
        ///     close-анимаций; к этому моменту поддерево уже деинициализировано.
        /// </summary>
        internal void ReturnTreeToPool()
        {
            // Самозащита вместо дисциплины вызывающих (ревью P2): ReturnLayout
            // идемпотентен через _tracked пула, а ReturnViewModel — нет; второй
            // возврат раздвоил бы вьюмодель между двумя виджетами.
            if (_isReturnedToPool) return;
            _isReturnedToPool = true;

            foreach (var child in _children.ToArray()) child.ReturnTreeToPool();
            _children.Clear();

            _uiSystem.ReturnViewModel(ViewModelUntyped);
            _uiSystem.ReturnLayout(LayoutUntyped);
        }

        internal async UniTask Open(bool animated)
        {
            OnOpen();

            if (!animated)
            {
                await UniTask.WhenAll(_children.ToArray().Select(child => child.Open(false)));
                return;
            }

            var animations = LayoutUntyped.GetAnimations(
                DefaultAnimationCategories.Open,
                DefaultAnimationCategories.OpenForwardCloseBackward);

            var openTasks = animations.Select(animation => animation.Play())
                .Concat(_children.ToArray().Select(child => child.Open(true)));

            await UniTask.WhenAll(openTasks);
        }

        internal async UniTask Close(bool animated)
        {
            // Одноразовость закрытия — структурная, а не следствие того, что
            // DeInitialize снял все колбэки (ревью P3): виджеты не переиспользуются,
            // повторный Close не должен реиграть OnClose и анимации.
            if (_isClosed) return;
            _isClosed = true;

            // Закрывающееся дерево — мёртвый full-panel оверлей поверх родителя:
            // на время фейда оно не должно перехватывать указатель (ревью P2).
            LayoutUntyped.CloseStarting();

            OnClose();

            if (!animated)
            {
                await UniTask.WhenAll(_children.ToArray().Select(child => child.Close(false)));
                return;
            }

            var closeAnimations = LayoutUntyped.GetAnimations(DefaultAnimationCategories.Close);
            var closeBackward = LayoutUntyped.GetAnimations(DefaultAnimationCategories.OpenForwardCloseBackward);

            var closeTasks = closeAnimations
                .Select(a => a.Play())
                .Concat(closeBackward.Select(a => a.PlayBackwards()))
                .Concat(_children.ToArray().Select(child => child.Close(true)));

            await UniTask.WhenAll(closeTasks);
        }

        public UniTask PlayAnimationCategories(params string[] categories)
        {
            var animations = LayoutUntyped.GetAnimations(categories).ToArray();
            return UniTask.WhenAll(animations.Select(a => a.Play()));
        }

        public UniTask PlayBackwardAnimationCategories(params string[] categories)
        {
            var animations = LayoutUntyped.GetAnimations(categories).ToArray();
            return UniTask.WhenAll(animations.Select(a => a.PlayBackwards()));
        }

        protected void RequestClose(bool animated = true)
        {
            OnCloseRequested?.Invoke(this, animated);
        }

        protected TWidget CreateChild<TWidget>(
            IViewModel viewModel = null,
            UILayout layout = null,
            string layoutId = null,
            Transform mountingPoint = null) where TWidget : Widget, new()
        {
            var widget = _uiSystem.Create<TWidget>(viewModel, layout, layoutId, mountingPoint);
            AddChild(widget);
            return widget;
        }

        protected TWidget OpenChild<TWidget>(
            IViewModel viewModel = null,
            UILayout layout = null,
            string layoutId = null,
            bool animated = true,
            Transform mountingPoint = null) where TWidget : Widget, new()
        {
            var widget = CreateChild<TWidget>(viewModel, layout, layoutId, mountingPoint);
            widget.Initialize();
            // Forget(): анимация открытия выполняется в фоне, но её исключения теперь не теряются молча.
            widget.Open(animated).Forget();
            return widget;
        }

        protected TWidget Open<TWidget>(IViewModel viewModel = null, string layoutId = null)
            where TWidget : Widget, new()
        {
            return _uiSystem.Open<TWidget>(viewModel, layoutId);
        }

        protected void CloseChild(Widget widget, bool animated = true)
        {
            if (widget == null || !_children.Contains(widget)) return;

            // Убираем из списка детей сразу, чтобы повторный CloseChild
            // до конца анимации не запустил второй возврат в пул.
            RemoveChild(widget);
            widget.DeInitialize();

            // ReturnTreeToPool вместо точечного возврата: у ребёнка могут быть
            // собственные дети — их ресурсы возвращаются тем же поддеревом.
            widget.Close(animated).ContinueWith(widget.ReturnTreeToPool).Forget();

            OnChildClosed(widget);
        }

        /// <summary>
        ///     Хук закрытия ребёнка (любым путём: RequestClose ребёнка или прямой
        ///     CloseChild). Родитель-модаль обязан вернуть себе точку геймпад-фокуса.
        /// </summary>
        protected virtual void OnChildClosed(Widget child)
        {
        }

        private void AddChild(Widget child)
        {
            _children.Add(child);
            child.OnCloseRequested += OnChildRequestedClose;
        }

        private void RemoveChild(Widget child)
        {
            _children.Remove(child);
            child.OnCloseRequested -= OnChildRequestedClose;
        }

        private void OnChildRequestedClose(Widget widget, bool animated)
        {
            CloseChild(widget, animated);
        }
    }
}
