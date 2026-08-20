using UnityEngine;

namespace DM.Pooling
{
    public abstract class ComponentPool<T> : Pool<T> where T : Component
    {
        protected ComponentPool(IPrefabProvider<T> prefabProvider, IStashProvider stashProvider) : base(prefabProvider,
            stashProvider)
        {
        }

        protected sealed override void MountToStash(T obj, GameObject stash)
        {
            // Стэш мог быть уничтожен закрытием сцены (teardown): объект умрёт
            // вместе с ней, перепарентинг в мёртвый стэш только бросит исключение.
            if (stash == null) return;
            // worldPositionStays=false: сохраняем локальные значения RectTransform без пересчёта
            obj.transform.SetParent(stash.transform, false);
        }

        protected sealed override void UnmountFromStash(T obj, GameObject stash)
        {
            // worldPositionStays=false: при выходе из Stash НЕ пересчитываем RectTransform,
            // иначе UISystem получает корректный layoutId, но с испорченными offset-ами
            obj.transform.SetParent(null, false);
        }
    }
}