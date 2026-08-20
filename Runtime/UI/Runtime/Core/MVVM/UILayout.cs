using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DM.UI
{
    public abstract class UILayout : MonoBehaviour
    {
        private static readonly IUIAnimation[] EmptyAnimations = new IUIAnimation[0];

        [SerializeField] private AnimationsCollection[] animationsCollection;

        [SerializeField] private string[] customAnimationCategories;

        private string[] _animationCategories;

        private Dictionary<string, IUIAnimation[]> _categorizedAnimations;

        private bool _isAnimationsInitialized;

        public IEnumerable<string> CustomAnimationCategories =>
            customAnimationCategories ?? Enumerable.Empty<string>();

        public IEnumerable<string> AnimationCategories => _animationCategories ?? Enumerable.Empty<string>();

        internal void Initialize()
        {
            InitializeAnimations();
            OnInitialize();
        }

        internal IEnumerable<IUIAnimation> GetAnimations(params string[] categories)
        {
            InitializeAnimations();

            if (categories == null || categories.Length == 0) return EmptyAnimations;

            var animations = new List<IUIAnimation>();

            foreach (var category in categories)
            {
                if (_categorizedAnimations.TryGetValue(category, out var categoryAnimations))
                    animations.AddRange(categoryAnimations);
                else
                    throw new UISystemException(
                        $"Animation category '{category}' is not defined on layout '{name}'. " +
                        $"Known categories: {string.Join(", ", _animationCategories)}.");
            }

            return animations.Distinct().ToArray();
        }

        internal void Restore()
        {
            OnRestore();
        }

        internal void CloseStarting()
        {
            OnCloseStarting();
        }

        protected virtual void OnRestore()
        {
        }

        /// <summary>
        ///     Начало закрытия виджета: дерево остаётся смонтированным на время
        ///     close-анимации и обязано перестать участвовать в пикинге.
        /// </summary>
        protected virtual void OnCloseStarting()
        {
        }

        protected virtual void OnInitialize()
        {
        }

        private void InitializeAnimations()
        {
            if (_isAnimationsInitialized) return;
            _isAnimationsInitialized = true;

            _animationCategories = (customAnimationCategories ?? new string[0])
                .Where(c => !string.IsNullOrEmpty(c))
                .Concat(DefaultAnimationCategories.Categories)
                .Distinct()
                .ToArray();

            // Пустые слоты в массиве коллекций — обычное дело в инспекторе,
            // они не должны ронять инициализацию лэйаута.
            var animations = (animationsCollection ?? new AnimationsCollection[0])
                .Where(a => a != null)
                .SelectMany(a => a.Animations ?? Enumerable.Empty<IUIAnimation>())
                .Where(a => a != null)
                .ToArray();

            _categorizedAnimations = _animationCategories.ToDictionary(
                category => category,
                category => animations.Where(a => a.Categories != null && a.Categories.Contains(category)).ToArray());
        }
    }
}
