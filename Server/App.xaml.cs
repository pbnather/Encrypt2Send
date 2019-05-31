using System.Windows;

namespace Server
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Window win = new View.MainView
            {
                DataContext = ViewModel.ViewModelFactory.CreateMainViewModel()
            };
            win.Show();
        }
    }
}
