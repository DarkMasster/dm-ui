using System;

namespace DM.Reactivity
{
    public static partial class ReactiveTypesExtension
    {
        public static IDisposable SubscribeOnValueChanged< T >(
            this IReactivePropertyReadonly< T > property,
            Action< T > action,
            bool invoke
        )
        {
            property.OnValueChanged += action;
            Subscription subscription = Subscription.Create( () => property.OnValueChanged -= action );
            if ( invoke )
                action?.Invoke( property.Value );
            return subscription;
        }

        public static IDisposable SubscribeOnValueChangedExtended< T >(
            this IReactivePropertyReadonly< T > property,
            Action< T, T > action
        )
        {
            property.OnValueChangedExtended += action;
            Subscription subscription = Subscription.Create( () => property.OnValueChangedExtended -= action );
            return subscription;
        }
    }
}
