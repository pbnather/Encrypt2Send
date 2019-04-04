using System;

namespace Server.ViewModel
{
    interface INavigationService
    {

        void OpenWindow(AbstractViewModel vm);
        void CloseWindow(Type vm);

    }
}
