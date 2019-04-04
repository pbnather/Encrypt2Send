namespace Server.ViewModel
{
    class MainViewModel : AbstractViewModel
    {
        public MainViewModel(INavigationService ns) : base(ns) {}

        protected override void ExitWindow()
        {
            _navService.CloseWindow(typeof(MainViewModel));
        }
    }
}
