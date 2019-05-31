using Server.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.ViewModel
{
    class SettingsViewModel : AbstractViewModel
    {
        public SettingsViewModel(INavigationService ns, IApplication app) : base(ns, app) { }
        protected override void ExitWindow()
        {
            _navService.CloseWindow(typeof(SettingsViewModel));
        }
    }
}
