using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DM.UI.UIToolkit
{
    /// <summary>
    ///     Лэйаут на UI Toolkit: хост <see cref="PanelRenderer" /> в пуле DM.
    ///     Корень приходит колбэком панели, а не свойством, поэтому
    ///     <see cref="CacheElements" /> зовёт колбэк.
    ///     <para>
    ///     Контракт кеша (замер в 6000.5.3f1, Play Mode, лог по кадрам):
    ///     колбэк — это событие СМЕНЫ дерева, а не выдача корня по запросу.
    ///     Он приходит один раз на построение дерева и отложенно (в том же
    ///     кадре, но после регистрации), а повторная регистрация на уже
    ///     уведомлённой панели его НЕ вызывает — ни при выходе из стэша,
    ///     ни вообще. Round-trip через стэш дерево сохраняет целиком: и корень,
    ///     и инстансы элементов те же.
    ///     </para>
    ///     <para>
    ///     Отсюда время жизни кэша: он живёт столько же, сколько дерево, то есть
    ///     сколько сам объект, — а НЕ один цикл открытия. Сбрасывать его при
    ///     возврате в пул нельзя: собрать заново будет нечем, потому что второго
    ///     колбэка не будет. Поэтому <see cref="OnReleaseElements" /> зовётся
    ///     там, где ссылки действительно устаревают — когда колбэк принёс новое
    ///     дерево или сообщил, что дерева нет.
    ///     </para>
    /// </summary>
    [RequireComponent( typeof( PanelRenderer ) )]
    public abstract class UITKLayout : UILayout
    {
        [SerializeField] private PanelRenderer panel;

        // Делегат кешируется в поле: снять с панели можно только тот же
        // экземпляр, а метод-группа даёт новый делегат на каждом обращении.
        private PanelRenderer.UIReloadCallback _uiReloadCallback;
        private VisualElement _root;

        // Пикинг, снятый на время закрытия, и его исходные значения. Дерево
        // переживает пул, поэтому снятый пикинг обязан быть возвращён поимённо:
        // ни «всем Position», ни пере-запросом CacheElements это не лечится —
        // первое затрёт намеренно неинтерактивные зоны экрана, второго
        // не будет вовсе.
        private readonly List< KeyValuePair< VisualElement, PickingMode > > _suppressedPicking = new();

        public VisualElement Root => _root;

        protected virtual void OnEnable()
        {
            if ( panel == null )
                panel = GetComponent<PanelRenderer>();

            _uiReloadCallback ??= OnUIReloaded;

            // Регистрация в OnEnable, а не в OnInitialize: колбэк на первое
            // построение дерева приходит отложенно, и пропустить его означает
            // не получить корень никогда. Снятие в OnDisable симметрично —
            // панель не должна держать ссылку на выключенный лэйаут.
            panel.RegisterUIReloadCallback( _uiReloadCallback );
        }

        protected virtual void OnDisable()
        {
            if ( panel != null )
                panel.UnregisterUIReloadCallback( _uiReloadCallback );
        }

        protected sealed override void OnInitialize()
        {
            // Проверка инварианта, а не место получения корня: колбэк обязан
            // был сработать при активации объекта. Корня нет — значит лэйаут
            // открыли, не разогрев (см. UILayoutsPreloadInitializable), и
            // виджет получил бы null-элементы молча.
            if ( _root == null )
                throw new UISystemException(
                    $"{name}: the panel root has not arrived. The layout was not warmed up: " +
                    "its first instance must be taken from the pool and returned during " +
                    "initialization, so that the panel reload callback has fired before Construct."
                );

            // Страховка ставится на открытие, а не на закрытие или парковку,
            // потому что открытие — единственная точка, которая гарантированно
            // проходит перед тем, как экран покажут игроку: OnInitialize зовётся
            // при каждом Open, независимо от того, был ли лэйаут запаркован
            // штатно. Если список не пуст, значит между OnCloseStarting и
            // OnRestore что-то сорвалось (парковка не состоялась) и снятый
            // пикинг пережил пул как есть — ровно дефект, на котором D2 уже
            // ловилась: ссылки целы, кликнуть нельзя. Тихо чинить и молчать
            // об этом одинаково плохо: это симптом сбоя в другом месте
            // пайплайна закрытия, а не нормальный режим работы.
            if ( _suppressedPicking.Count > 0 )
            {
                Debug.LogWarning(
                    $"[{nameof(UITKLayout)}] {name}: picking suppressed by a previous close was " +
                    "still active on open — the layout was not parked back into the pool after " +
                    "closing (OnRestore did not run). Picking has been restored so the screen is " +
                    "usable, but the skipped park step needs investigating.",
                    this
                );
                RestoreSuppressedPicking();
            }
        }

        protected sealed override void OnRestore()
        {
            // Возврат в пул отменяет ровно то, что сделало закрытие, и ничего
            // больше. Ссылки на элементы здесь НЕ сбрасываются: дерево живо
            // и то же самое, а второго колбэка, который собрал бы кэш заново,
            // не будет — сброс здесь оставлял бы лэйаут навсегда без элементов.
            RestoreSuppressedPicking();
        }

        protected sealed override void OnCloseStarting()
        {
            // Мёртвый оверлей на время фейда не должен глотать клики по родителю
            // (ревью P2). Пикинг снимается со ВСЕГО дерева: Ignore на корне не
            // отключает детей. Исходные значения запоминаются: дерево переживёт
            // возврат в пул вместе со снятым пикингом, и следующее открытие
            // получило бы экран, на который нельзя нажать.
            VisualElement root = Root;
            if ( root == null )
                return;

            _suppressedPicking.Clear();
            SuppressPicking( root );
            root.Query<VisualElement>().ForEach( SuppressPicking );
        }

        /// <summary>Пере-запрос ссылок на элементы. Вызывается при смене дерева.</summary>
        protected abstract void CacheElements( VisualElement root );

        /// <summary>
        ///     Сброс ссылок на элементы устаревшего дерева. Зовётся при смене
        ///     дерева, а не при возврате в пул: возврат в пул дерево сохраняет.
        /// </summary>
        protected virtual void OnReleaseElements()
        {
        }

        /// <summary>
        ///     Единственное место, где корень попадает в лэйаут. Колбэк может
        ///     прийти больше одного раза (инстанцирование UXML, live reload)
        ///     и подменить инстанс корня — поэтому он перезапускаемый и сам
        ///     отпускает ссылки на прежнее дерево.
        /// </summary>
        private void OnUIReloaded( PanelRenderer sender, VisualElement root )
        {
            if ( _root != null && _root != root )
            {
                // Прежнее дерево больше не действует: снятый на нём пикинг
                // восстанавливать некуда, а ссылки на его элементы обязаны
                // уйти до того, как появятся новые.
                _suppressedPicking.Clear();
                OnReleaseElements();
            }

            _root = root;
            if ( root == null )
            {
                OnReleaseElements();
                return;
            }

            // Две ловушки растяжения (спайк + первый экран): контейнер дочерней
            // панели не растягивается внутри родительской, а обёртка
            // TemplateContainer не растягивается внутри контейнера. У вложенной
            // панели корень остаётся Relative с нулевой высотой, поэтому фикс
            // обязателен: корень лэйаута всегда absolute-оверлей на всю панель.
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;
            foreach ( VisualElement child in root.Children() )
                child.style.flexGrow = 1;

            CacheElements( root );
        }

        private void SuppressPicking( VisualElement element )
        {
            // Запоминается только то, что реально изменено. Это и экономия,
            // и защита от повторного посещения: Query включает сам корень,
            // поэтому без этой проверки корень попадал в список дважды —
            // сначала с исходным Position, потом с уже снятым Ignore, —
            // и восстановление заканчивалось на Ignore, то есть на экране,
            // по которому нельзя кликнуть.
            if ( element.pickingMode == PickingMode.Ignore )
                return;

            _suppressedPicking.Add(
                new KeyValuePair< VisualElement, PickingMode >( element, element.pickingMode )
            );
            element.pickingMode = PickingMode.Ignore;
        }

        private void RestoreSuppressedPicking()
        {
            foreach ( KeyValuePair< VisualElement, PickingMode > suppressed in _suppressedPicking )
                suppressed.Key.pickingMode = suppressed.Value;

            _suppressedPicking.Clear();
        }
    }
}
