using System;
using System.Collections.Generic;
using System.Windows;
using Server.ViewModel;

namespace Server.View
{
    class NavigationService : INavigationService
    {
        private Dictionary<string,Window> activeWindows = new Dictionary<string, Window>();

        public void OpenWindow(AbstractViewModel vm, bool disable = true)
        {
            string vmName = vm.GetType().Name;
            Window window = null;
            switch (vmName)
            {
                case "MainViewModel":
                    window = new MainView() { DataContext = vm };
                    window.Closing += ((MainViewModel)window.DataContext).OnWindowClosing;
                    break;
                case "SettingsViewModel":
                    window = new SettingsView() { DataContext = vm };
                    break;
                case "CreatePrivateKeyViewModel":
                    window = new CreatePrivateKeyView() { DataContext = vm };
                    break;
                case "TransfersViewModel":
                    window = new TransfersView() { DataContext = vm };
                    break;
            }

            if (window == null) return;
            if (activeWindows.ContainsKey(vmName)) activeWindows.Remove(vmName);
            activeWindows.Add(vmName, window);

            if(disable) window.ShowDialog();
            else window.Show();
        }

        public void CloseWindow(Type vm)
        {
            if (!activeWindows.ContainsKey(vm.Name)) return;
            activeWindows[vm.Name].Close();
            activeWindows.Remove(vm.Name);
        }
    }
}