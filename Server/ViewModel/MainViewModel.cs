using Microsoft.Win32;
using System.Windows.Input;

namespace Server.ViewModel
{
    class MainViewModel : AbstractViewModel
    {
        public string SelectedFileName { get; set; }
        public MainViewModel(INavigationService ns) : base(ns) {}

        private ICommand _chooseFile;
        public ICommand ChooseFile => _chooseFile ?? (_chooseFile = new RelayCommand(OpenFilePickerDialog));

        private void OpenFilePickerDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            {
                if (openFileDialog.ShowDialog() == true)
                {
                    SelectedFileName = openFileDialog.FileName;
                    NotifyPropertyChanged(nameof(SelectedFileName));
                }
            }
        }

        protected override void ExitWindow()
        {
            _navService.CloseWindow(typeof(MainViewModel));
        }
    }
}
