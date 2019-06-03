using System;

namespace Server.ViewModel
{
    interface INavigationService
    {

        void OpenWindow(AbstractViewModel vm, bool disable = true);
        void CloseWindow(Type vm);

    }
}
