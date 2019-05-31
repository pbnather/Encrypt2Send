using System;
using System.Collections.Generic;
using System.Windows;
using Server.ViewModel;

namespace Server.View
{
    class NavigationService : INavigationService
    {
        private Dictionary<string,Window> activeWindows = new Dictionary<string, Window>();

        public void OpenWindow(AbstractViewModel vm)
        {
            string vmName = vm.GetType().Name;
            Window window = null;
            switch (vmName)
            {
                case "MainViewModel":
                    window = new MainView() { DataContext = vm };
                    break;
                case "SettingsViewModel":
                    window = new SettingsView() { DataContext = vm };
                    break;
            }

            if (window == null) return;
            if (activeWindows.ContainsKey(vmName)) activeWindows.Remove(vmName);
            activeWindows.Add(vmName, window);

            window.ShowDialog();
        }

        public void CloseWindow(Type vm)
        {
            if (!activeWindows.ContainsKey(vm.Name)) return;
            activeWindows[vm.Name].Close();
            activeWindows.Remove(vm.Name);
        }
    }
}