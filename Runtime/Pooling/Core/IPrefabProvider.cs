using UnityEngine;

namespace DM.Pooling
{
    public interface IPrefabProvider<out T> where T : Object
    {
        T GetPrefab(string id);
    }
}