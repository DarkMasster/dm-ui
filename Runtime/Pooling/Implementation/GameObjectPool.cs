using UnityEngine;

namespace DM.Pooling
{
    public class GameObjectPool : Pool<GameObject>
    {
        public GameObjectPool(IPrefabProvider<GameObject> prefabProvider, IStashProvider stashProvider) : base(
            prefabProvider, stashProvider)
        {
        }

        protected sealed override void MountToStash(GameObject obj, GameObject stash)
        {
            // Паритет с ComponentPool: стэш мог быть уничтожен закрытием сцены.
            if (stash == null) return;
            obj.transform.SetParent(stash.transform);
        }

        protected sealed override void UnmountFromStash(GameObject obj, GameObject stash)
        {
            obj.transform.SetParent(null);
        }
    }
}