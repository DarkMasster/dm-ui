using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DM.Integration
{
    /// <summary>
    ///     Реестр UI-лэйаутов: layoutId → Addressables-ссылка на префаб с UILayout.
    ///     Единственный источник правды для <see cref="AddressablesLayoutsProvider" /> —
    ///     прямые ссылки на префабы лэйаутов запрещены конвенцией проекта (§1.1 плана).
    /// </summary>
    [CreateAssetMenu( menuName = "Game/UI/UI Layout Registry", fileName = "UILayoutRegistry" )]
    public class UILayoutRegistryAsset : ScriptableObject
    {
        public const string ResourcePath = "UI/UILayoutRegistry";

        [SerializeField] private List<Entry> entries = new();

        public IReadOnlyList<Entry> Entries => entries;

        [Serializable]
        public class Entry
        {
            public string layoutId;
            public AssetReferenceGameObject layout;
        }
    }
}
