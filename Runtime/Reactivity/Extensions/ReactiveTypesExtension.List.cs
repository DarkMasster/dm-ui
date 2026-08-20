using System;
using System.Collections.Generic;

namespace DM.Reactivity
{
    public static partial class ReactiveTypesExtension
    {
        public static IDisposable SubscribeOnAddItem< T >(
            this IReactiveListReadOnly< T > list,
            Action< GenericPairEventArgs< int, T > > action
        )
        {
            list.OnAddItem += action;
            Subscription subscription = Subscription.Create( () => list.OnAddItem -= action );
            return subscription;
        }

        public static IDisposable SubscribeOnClear< T >(
            this IReactiveListReadOnly< T > list,
            Action< GenericEventArg< IEnumerable< T > > > action
        )
        {
            list.OnClear += action;
            Subscription subscription = Subscription.Create( () => list.OnClear -= action );
            return subscription;
        }

        public static IDisposable SubscribeOnElementChange< T >(
            this IReactiveListReadOnly< T > list,
            Action< GenericPairEventArgs< int, T > > action
        )
        {
            list.OnElementChange += action;
            Subscription subscription = Subscription.Create( () => list.OnElementChange -= action );
            return subscription;
        }

        public static IDisposable SubscribeOnRemoveItem< T >(
            this IReactiveListReadOnly< T > list,
            Action< GenericPairEventArgs< int, T > > action
        )
        {
            list.OnRemoveItem += action;
            Subscription subscription = Subscription.Create( () => list.OnRemoveItem -= action );
            return subscription;
        }

        public static IDisposable SubscribeOnSort< T >(
            this IReactiveListReadOnly< T > list,
            Action< ReactiveListSortingArgs< T > > action
        )
        {
            list.OnSort += action;
            Subscription subscription = Subscription.Create( () => list.OnSort -= action );
            return subscription;
        }
    }
}
