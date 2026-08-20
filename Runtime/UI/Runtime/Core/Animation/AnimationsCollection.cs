using System.Collections.Generic;
using UnityEngine;

namespace DM.UI
{
    public abstract class AnimationsCollection : MonoBehaviour
    {
        public abstract IEnumerable<IUIAnimation> Animations { get; }
    }
}