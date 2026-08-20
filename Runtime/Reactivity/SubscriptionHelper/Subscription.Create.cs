using System;
using UnityEngine.Events;

namespace DM.Reactivity
{
    public abstract partial class Subscription
    {
        public static Subscription Create( Action unsubscribeAction ) => new ActionUnsubscribableSubscription( unsubscribeAction );

        public static Subscription Create(
            UnityEvent eventObject,
            UnityAction handler
        )
        {
            eventObject.AddListener( handler );
            ActionUnsubscribableSubscription subscription = new ActionUnsubscribableSubscription( () => eventObject.RemoveListener( handler ) );

            return subscription;
        }

        public static Subscription Create< T >(
            UnityEvent< T > eventObject,
            UnityAction< T > handler
        )
        {
            eventObject.AddListener( handler );
            ActionUnsubscribableSubscription subscription = new ActionUnsubscribableSubscription( () => eventObject.RemoveListener( handler ) );

            return subscription;
        }

        public static Subscription Create< T1, T2 >(
            UnityEvent< T1, T2 > eventObject,
            UnityAction< T1, T2 > handler
        )
        {
            eventObject.AddListener( handler );
            ActionUnsubscribableSubscription subscription = new ActionUnsubscribableSubscription( () => eventObject.RemoveListener( handler ) );

            return subscription;
        }

        public static Subscription Create< T1, T2, T3 >(
            UnityEvent< T1, T2, T3 > eventObject,
            UnityAction< T1, T2, T3 > handler
        )
        {
            eventObject.AddListener( handler );
            ActionUnsubscribableSubscription subscription = new ActionUnsubscribableSubscription( () => eventObject.RemoveListener( handler ) );

            return subscription;
        }
    }
}
