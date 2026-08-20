using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DM.UI
{
    /// <summary>
    ///     Коллекция анимаций на базе штатного Unity Animation.
    ///     Раньше свойство Animations было get-only автосвойством и всегда возвращало null,
    ///     из-за чего UILayout падал с NRE при инициализации анимаций.
    /// </summary>
    public class UnityAnimationsCollection : AnimationsCollection
    {
        [SerializeField] private List<UnityAnimationWrapper> animations = new();

        public override IEnumerable<IUIAnimation> Animations => animations ?? Enumerable.Empty<IUIAnimation>();

        [System.Serializable]
        private class UnityAnimationWrapper : IUIAnimation
        {
            [SerializeField] private Animation animationComponent;
            [SerializeField] private string clipName;
            [SerializeField] private string[] categories = new string[0];

            public string[] Categories => categories ?? new string[0];

            public Cysharp.Threading.Tasks.UniTask Play()
            {
                return PlayClip(false);
            }

            public Cysharp.Threading.Tasks.UniTask PlayBackwards()
            {
                return PlayClip(true);
            }

            private async Cysharp.Threading.Tasks.UniTask PlayClip(bool backwards)
            {
                if (animationComponent == null || string.IsNullOrEmpty(clipName)) return;

                var state = animationComponent[clipName];
                if (state == null) return;

                state.speed = backwards ? -1f : 1f;
                state.time = backwards ? state.length : 0f;

                animationComponent.Play(clipName);

                await Cysharp.Threading.Tasks.UniTask.Delay(
                    Mathf.RoundToInt(state.length * 1000f), cancelImmediately: true);
            }
        }
    }
}
