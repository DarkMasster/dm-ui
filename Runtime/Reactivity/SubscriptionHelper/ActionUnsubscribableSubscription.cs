using System;

namespace DM.Reactivity
{
    public class ActionUnsubscribableSubscription : Subscription
    {
        private readonly Action _unsubscribeAction;

        public ActionUnsubscribableSubscription( Action unsubscribeAction ) => _unsubscribeAction = unsubscribeAction;

        protected override void OnDispose()
        {
            _unsubscribeAction();
        }
    }
}
