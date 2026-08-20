using System;
using UnityEngine.UIElements;

namespace DM.Reactivity
{
    public abstract partial class Subscription
    {
        /// <summary>
        ///     Подписка на событие панели UI Toolkit. Обязательный путь для виджетов:
        ///     колбэк снимается в Dispose, поэтому слушатели не переживают виджет,
        ///     когда лэйаут возвращается в пул.
        /// </summary>
        public static Subscription Create< TEvent >(
            CallbackEventHandler element,
            EventCallback< TEvent > handler,
            TrickleDown useTrickleDown = TrickleDown.NoTrickleDown
        ) where TEvent : EventBase< TEvent >, new()
        {
            element.RegisterCallback( handler, useTrickleDown );
            return new ActionUnsubscribableSubscription(
                () => element.UnregisterCallback( handler, useTrickleDown )
            );
        }

        public static Subscription Create( Clickable clickable, Action handler )
        {
            clickable.clicked += handler;
            return new ActionUnsubscribableSubscription( () => clickable.clicked -= handler );
        }

        public static Subscription Create( Button button, Action handler )
        {
            button.clicked += handler;
            return new ActionUnsubscribableSubscription( () => button.clicked -= handler );
        }
    }
}
