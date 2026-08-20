using System;

namespace DM.Reactivity
{
    public abstract partial class Subscription : IDisposable
    {
        protected abstract void OnDispose();

        public void Dispose()
        {
            OnDispose();
        }
    }
}
