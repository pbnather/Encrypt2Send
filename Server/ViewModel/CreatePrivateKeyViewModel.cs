using Server.Model;
using System.Windows.Input;

namespace Server.ViewModel
{
    class CreatePrivateKeyViewModel : AbstractViewModel
    {
        public bool PasswordHas8Chars { get; set; }
        private string _password;
        public string Password { get
            {
                return _password;
            }
            set
            {
                _password = value;
                if (_password.Length > 8) PasswordHas8Chars = true;
                else PasswordHas8Chars = false;
                NotifyPropertyChanged("PasswordHas8Chars");
            }
            }
        public CreatePrivateKeyViewModel(INavigationService ns, IApplication app) : base(ns, app) {
            PasswordHas8Chars = false;
        }

        private ICommand _createPrivateKey;
        public ICommand CreatePrivateKey => _createPrivateKey ?? (_createPrivateKey = new RelayCommand(CreateNewPrivateKey));

        private void CreateNewPrivateKey()
        {
            _app.GeneratePrivateKey(Password);
            ExitWindow();
        }
        protected override void ExitWindow()
        {
            _navService.CloseWindow(typeof(CreatePrivateKeyViewModel));
        }
    }
}