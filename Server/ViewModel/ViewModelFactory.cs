namespace Server.ViewModel
{
    class ViewModelFactory
    {

        static private readonly Model.IApplication app = new Model.Application();
        static public readonly INavigationService ns = new View.NavigationService();

        static public MainViewModel CreateMainViewModel() => new MainViewModel(ns, app);
        static public SettingsViewModel CreateSettingsViewModel() => new SettingsViewModel(ns, app);
        static public CreatePrivateKeyViewModel CreateCreatePrivateKeyViewModel() => new CreatePrivateKeyViewModel(ns, app);
    }
}
