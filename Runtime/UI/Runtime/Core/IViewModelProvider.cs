using System;

namespace DM.UI
{
    public interface IViewModelProvider
    {
        IViewModel Get(Type viewModelType);

        void Return(IViewModel viewModel);
    }
}