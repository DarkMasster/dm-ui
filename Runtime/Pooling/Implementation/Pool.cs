using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DM.Pooling
{
    public abstract class Pool<T> : IPool<T> where T : Object
    {
        private readonly Dictionary<string, Stack<T>> _pool = new();
        private readonly IPrefabProvider<T> _prefabProvider;
        private readonly GameObject _stash;
        private readonly Dictionary<T, string> _tracked = new();

        protected Pool(IPrefabProvider<T> prefabProvider, IStashProvider stashProvider)
        {
            _prefabProvider = prefabProvider;
            _stash = stashProvider.GetStash();
        }

        public T Get(string id)
        {
            T obj = null;

            if (_pool.TryGetValue(id, out var stack))
                // Объект из пула мог быть уничтожен извне — такие экземпляры отбрасываем.
                while (stack.Count > 0 && obj == null)
                    obj = stack.Pop();

            if (obj == null)
            {
                var prefab = _prefabProvider.GetPrefab(id);
                if (prefab == null) return null;

                // Стэш может отсутствовать (провайдер отказал на teardown) —
                // инстанс тогда создаётся в корне и живёт до перепарентинга.
                obj = _stash != null
                    ? Object.Instantiate(prefab, _stash.transform)
                    : Object.Instantiate(prefab);
            }

            _tracked[obj] = id;
            UnmountFromStash(obj, _stash);
            return obj;
        }

        public bool TryReturnToPool(T obj)
        {
            // obj == null покрывает и уничтоженные объекты Unity (fake null).
            if (obj == null) return false;
            if (!_tracked.Remove(obj, out var id)) return false;

            MountToStash(obj, _stash);

            if (!_pool.TryGetValue(id, out var stack))
            {
                stack = new Stack<T>();
                _pool[id] = stack;
            }

            stack.Push(obj);
            return true;
        }

        /// <summary>Заранее создаёт указанное количество экземпляров, чтобы не платить за Instantiate в бою.</summary>
        public void Prewarm(string id, int count)
        {
            var warmed = new List<T>(count);

            for (var i = 0; i < count; i++)
            {
                var obj = Get(id);
                if (obj == null) break;
                warmed.Add(obj);
            }

            foreach (var obj in warmed) TryReturnToPool(obj);
        }

        /// <summary>
        ///     Уничтожает всё содержимое пула. Отданные наружу объекты не трогает:
        ///     _tracked хранит только выданные экземпляры (возврат снимает запись),
        ///     поэтому он сохраняется — иначе выданное навсегда теряло бы право
        ///     вернуться в пул.
        /// </summary>
        public void Clear()
        {
            foreach (var stack in _pool.Values)
                while (stack.Count > 0)
                {
                    var obj = stack.Pop();
                    if (obj != null) Object.Destroy(obj);
                }

            _pool.Clear();
        }

        protected abstract void MountToStash(T obj, GameObject stash);
        protected abstract void UnmountFromStash(T obj, GameObject stash);
    }
}
