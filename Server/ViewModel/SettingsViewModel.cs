using Server.Model;

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
