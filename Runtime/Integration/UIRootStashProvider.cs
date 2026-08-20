using DM.Pooling;
using DM.UI;
using UnityEngine;

namespace DM.Integration
{
    /// <summary>
    ///     Стэш пула лэйаутов: неактивный GameObject под UIRoot.
    ///     Живёт под корнем UI, чтобы запуленные хосты панелей не теряли связь
    ///     со сценой и не попадали под чужие DontDestroyOnLoad-правила.
    /// </summary>
    public sealed class UIRootStashProvider : IStashProvider
    {
        private readonly UIRoot _uiRoot;
        private GameObject _stash;

        public UIRootStashProvider( UIRoot uiRoot )
        {
            _uiRoot = uiRoot;
        }

        public GameObject GetStash()
        {
            if ( _stash == null )
            {
                // UIRoot уже уничтожен (teardown сцены или гонка асинхронного
                // бута с её закрытием): создавать нечего и негде — сирота в
                // закрывающейся сцене валит leak-проверку тест-раннера.
                if ( _uiRoot == null )
                    return null;
                _stash = new GameObject( "UILayoutsStash" );
                _stash.transform.SetParent( _uiRoot.transform, false );
                _stash.SetActive( false );
            }

            return _stash;
        }
    }
}
