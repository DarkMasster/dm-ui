using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace DM.UI.DOTween
{
    /// <summary>
    ///     Коллекция DOTween-анимаций для UITK-лэйаутов: сериализуемые пресеты
    ///     (fade/slide/scale) по категориям DM. Визуальный компонент DOTweenAnimation
    ///     (Pro) не умеет VisualElement, поэтому твин идёт через core-API по
    ///     style-свойствам (подтверждено спайком Фазы 0).
    /// </summary>
    [RequireComponent( typeof( PanelRenderer ) )]
    public class DOTweenUITKAnimationsCollection : AnimationsCollection
    {
        [SerializeField] private PanelRenderer panel;
        [SerializeField] private List<Preset> presets = new();

        // Делегат кешируется в поле: снять с панели можно только тот же
        // экземпляр, а метод-группа даёт новый делегат на каждом обращении.
        private PanelRenderer.UIReloadCallback _uiReloadCallback;
        private VisualElement _root;

        public override IEnumerable<IUIAnimation> Animations
        {
            get
            {
                foreach ( Preset preset in presets ?? Enumerable.Empty<Preset>() )
                {
                    preset.Bind( _root );
                    yield return preset;
                }
            }
        }

        protected virtual void OnEnable()
        {
            if ( panel == null )
                panel = GetComponent<PanelRenderer>();

            _uiReloadCallback ??= OnUIReloaded;
            panel.RegisterUIReloadCallback( _uiReloadCallback );
        }

        protected virtual void OnDisable()
        {
            if ( panel != null )
                panel.UnregisterUIReloadCallback( _uiReloadCallback );
        }

        /// <summary>
        ///     Корень приходит от панели колбэком: публичного
        ///     <c>rootVisualElement</c> у <see cref="PanelRenderer" /> нет.
        ///     Пресеты пере-привязываются здесь, а не только на перечислении:
        ///     список анимаций лэйаут кеширует один раз, а инстанс корня
        ///     колбэк может подменить.
        /// </summary>
        private void OnUIReloaded( PanelRenderer sender, VisualElement root )
        {
            _root = root;

            foreach ( Preset preset in presets ?? Enumerable.Empty<Preset>() )
                preset.Bind( root );
        }

        private enum EKind
        {
            Fade,
            SlideX,
            SlideY,
            Scale
        }

        [Serializable]
        private class Preset : IUIAnimation
        {
            [SerializeField] private string[] categories = { DefaultAnimationCategories.OpenForwardCloseBackward };

            [Tooltip( "Имя элемента (Q по name). Пусто — корневой контейнер лэйаута." )]
            [SerializeField] private string targetElementName;

            [SerializeField] private EKind kind = EKind.Fade;
            [SerializeField] private float from;
            [SerializeField] private float to = 1f;
            [SerializeField] private float duration = 0.25f;
            [SerializeField] private Ease ease = Ease.OutQuad;

            private VisualElement _root;

            public string[] Categories => categories ?? Array.Empty<string>();

            internal void Bind( VisualElement root )
            {
                _root = root;
            }

            public UniTask Play()
            {
                return Run( from, to );
            }

            public UniTask PlayBackwards()
            {
                return Run( to, from );
            }

            private UniTask Run( float startValue, float endValue )
            {
                VisualElement element = ResolveElement();
                if ( element == null )
                    return UniTask.CompletedTask;

                Apply( element, startValue );

                UniTaskCompletionSource completion = new();
                float current = startValue;
                DG.Tweening.DOTween
                    .To( () => current, v =>
                    {
                        current = v;
                        Apply( element, v );
                    }, endValue, duration )
                    .SetEase( ease )
                    .SetTarget( element )
                    .SetUpdate( true )
                    .OnKill( () => completion.TrySetResult() );

                return completion.Task;
            }

            private VisualElement ResolveElement()
            {
                if ( _root == null )
                    return null;
                return string.IsNullOrEmpty( targetElementName )
                    ? _root
                    : _root.Q<VisualElement>( targetElementName );
            }

            private void Apply( VisualElement element, float value )
            {
                switch ( kind )
                {
                    case EKind.Fade:
                        element.style.opacity = value;
                        break;
                    case EKind.SlideX:
                        element.style.translate = new Translate( value, 0f );
                        break;
                    case EKind.SlideY:
                        element.style.translate = new Translate( 0f, value );
                        break;
                    case EKind.Scale:
                        element.style.scale = new Scale( new Vector2( value, value ) );
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
