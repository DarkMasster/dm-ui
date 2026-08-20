using UnityEngine;

namespace DM.UI
{
    /// <summary>
    /// Корень UI-системы. Живёт на объекте Canvas.
    /// Хранит ссылки на слои Canvas, которые передаются в UISystem как точки монтирования.
    /// </summary>
    public class UIRoot : MonoBehaviour
    {
        [SerializeField] private Transform _screenLayer;
        [SerializeField] private Transform _popupLayer;

        /// <summary>Слой полноэкранных интерфейсов (Canvas/Screen).</summary>
        public Transform ScreenLayer => _screenLayer;

        /// <summary>Слой диалогов поверх экрана (Canvas/Popups).</summary>
        public Transform PopupLayer => _popupLayer;
    }
}
