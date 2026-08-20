using System;
using System.Collections.Generic;

namespace DM.Reactivity
{
    public static partial class ReactiveTypesExtension
    {
        public static IDisposable SubscribeOnAddItem< TKey, TValue >(
            this IReactiveDictionaryReadOnly< TKey, TValue > dictionary,
            Action< GenericPairEventArgs< TKey, TValue > > action
        )
        {
            dictionary.OnAddItem += action;
            Subscription subscription = Subscription.Create( () => dictionary.OnAddItem -= action );
            return subscription;
        }

        public static IDisposable SubscribeOnClear< TKey, TValue >(
            this IReactiveDictionaryReadOnly< TKey, TValue > dictionary,
            Action< GenericEventArg< IDictionary< TKey, TValue > > > action
        )
        {
            dictionary.OnClear += action;
            Subscription subscription = Subscription.Create( () => dictionary.OnClear -= action );
            return subscription;
        }

        public static IDisposable SubscribeOnElementChange< TKey, TValue >(
            this IReactiveDictionaryReadOnly< TKey, TValue > dictionary,
            Action< GenericPairEventArgs< TKey, TValue > > action
        )
        {
            dictionary.OnElementChange += action;
            Subscription subscription = Subscription.Create( () => dictionary.OnElementChange -= action );
            return subscription;
        }

        public static IDisposable SubscribeOnRemoveItem< TKey, TValue >(
            this IReactiveDictionaryReadOnly< TKey, TValue > dictionary,
            Action< GenericPairEventArgs< TKey, TValue > > action
        )
        {
            dictionary.OnRemoveItem += action;
            Subscription subscription = Subscription.Create( () => dictionary.OnRemoveItem -= action );
            return subscription;
        }
    }
}
