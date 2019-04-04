namespace Server.ViewModel
{
    class ViewModelFactory
    {
        static private readonly INavigationService ns = new View.NavigationService();

        static public MainViewModel CreateMainViewModel() => new MainViewModel(ns);
    }
}
