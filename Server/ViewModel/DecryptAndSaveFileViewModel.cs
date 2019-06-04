using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using Server.Model;

namespace Server.ViewModel
{
    class DecryptAndSaveFileViewModel : AbstractViewModel
    {
        public bool PasswordHas8Chars { get; set; }
        private string _password;
        public string Password
        {
            get
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

        public DecryptAndSaveFileViewModel(INavigationService ns, IApplication app) : base(ns, app)
        {
            PasswordHas8Chars = false;
        }

        private ICommand _saveFileDialog;
        public ICommand SaveFileDialog => _saveFileDialog ?? (_saveFileDialog = new RelayCommand(SaveFile));

        private void SaveFile()
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            //saveFileDialog1.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif";
            saveFileDialog1.Title = "Save decrypted File";
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName != "")
            {
                _app.DecryptAndSaveFile(saveFileDialog1.FileName, Password);
            } else
            {
                //messagebox
            }
            ExitWindow();
        }

        protected override void ExitWindow()
        {
            _navService.CloseWindow(typeof(DecryptAndSaveFileViewModel));
        }
    }
}
