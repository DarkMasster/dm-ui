using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DM.UI;
using UnityEngine;

namespace DM.Integration
{
    /// <summary>
    ///     Провайдер префабов лэйаутов поверх Addressables. UISystem.Open синхронен,
    ///     поэтому все лэйауты реестра прелоадятся на инициализации (под loading screen),
    ///     а GetPrefab отдаёт уже загруженное. Хэндлы освобождаются в Dispose.
    /// </summary>
    public sealed class AddressablesLayoutsProvider : IUILayoutsProvider, IDisposable
    {
        private readonly UILayoutRegistryAsset _registry;
        private readonly Dictionary<string, UILayout> _prefabs = new();
        private bool _isPreloaded;

        public AddressablesLayoutsProvider( UILayoutRegistryAsset registry )
        {
            _registry = registry != null
                ? registry
                : throw new ArgumentNullException( nameof( registry ) );
        }

        public async UniTask PreloadAsync( IProgress<float> progress = null )
        {
            if ( _isPreloaded )
                return;

            IReadOnlyList<UILayoutRegistryAsset.Entry> entries = _registry.Entries;
            for ( int i = 0; i < entries.Count; i++ )
            {
                UILayoutRegistryAsset.Entry entry = entries[ i ];
                if ( string.IsNullOrEmpty( entry.layoutId ) )
                    throw new UISystemException( $"{_registry.name}: entry #{i} has an empty layoutId." );
                if ( _prefabs.ContainsKey( entry.layoutId ) )
                    throw new UISystemException( $"{_registry.name}: duplicate layoutId '{entry.layoutId}'." );
                if ( entry.layout == null || !entry.layout.RuntimeKeyIsValid() )
                    throw new UISystemException(
                        $"{_registry.name}: layoutId '{entry.layoutId}' has no valid Addressables reference."
                    );

                GameObject prefab = await entry.layout.LoadAssetAsync<GameObject>().Task;
                UILayout layout = prefab != null ? prefab.GetComponent<UILayout>() : null;
                if ( layout == null )
                    throw new UISystemException(
                        $"{_registry.name}: prefab for layoutId '{entry.layoutId}' has no UILayout component."
                    );

                _prefabs[ entry.layoutId ] = layout;
                progress?.Report( ( i + 1f ) / entries.Count );
            }

            _isPreloaded = true;
        }

        public UILayout GetPrefab( string id )
        {
            if ( !_isPreloaded )
                throw new UISystemException(
                    $"{nameof( AddressablesLayoutsProvider )} is not preloaded. " +
                    $"Await {nameof( PreloadAsync )} during initialization before opening widgets."
                );
            if ( _prefabs.TryGetValue( id, out UILayout layout ) )
                return layout;

            throw new UISystemException(
                $"Layout id '{id}' is not registered in {_registry.name}. " +
                $"Known ids: {string.Join( ", ", _prefabs.Keys )}."
            );
        }

        public void Dispose()
        {
            foreach ( UILayoutRegistryAsset.Entry entry in _registry.Entries )
                if ( entry.layout != null && entry.layout.IsValid() )
                    entry.layout.ReleaseAsset();

            _prefabs.Clear();
            _isPreloaded = false;
        }
    }
}
