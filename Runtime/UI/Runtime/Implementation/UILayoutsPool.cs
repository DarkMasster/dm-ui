using DM.Pooling;

namespace DM.UI
{
    public class UILayoutsPool : ComponentPool<UILayout>
    {
        public UILayoutsPool(IUILayoutsProvider prefabProvider, IStashProvider stashProvider) : base(
            prefabProvider, stashProvider)
        {
        }
    }
}