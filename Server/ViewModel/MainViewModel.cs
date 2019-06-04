using Microsoft.Win32;
using System.Windows.Input;
using Server.Model;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Server.ViewModel
{
    class MainViewModel : AbstractViewModel
    {
        public string SelectedFileName { get; set; }
        public string EncryptedFileName { get; set; }

        public CipherModeCheckBoxable SelectedChiperMode { get; set; }
        public List<CipherModeCheckBoxable> CipherModes { get; set; }

        public int SelectedRecipientsCount => _checkableRecipients.FindAll(r => r.IsChecked == true).Count;
        public class CipherModeCheckBoxable : INotifyPropertyChanged
        {
            private string _mode;
            private int _modeNumber;

            public string Mode
            {
                get
                {
                    return _mode;
                }
                set
                {
                    if (_mode != value)
                    {
                        _mode = value;
                        NotifyPropertyChanged(nameof(Mode));
                    }
                }
            }
            public int ModeNumber
            {
                get
                {
                    return _modeNumber;
                }
                set
                {
                    if (_modeNumber != value)
                    {
                        _modeNumber = value;
                        NotifyPropertyChanged(nameof(ModeNumber));
                    }
                }
            }

            public CipherModeCheckBoxable(string mode, int number)
            {
                _mode = mode;
                _modeNumber = number;
            }

            public override string ToString()
            {
                return _mode;
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

        }
        public class CheckableRecipient : INotifyPropertyChanged
        {
            private Recipient _recipient;
            private bool _isChecked;

            public Recipient Recipient
            {
                get
                {
                    return _recipient;
                }
                set
                {
                    if (_recipient != value)
                    {
                        _recipient = value;
                        NotifyPropertyChanged("Recipient");
                    }
                }
            }
            public bool IsChecked
            {
                get
                {
                    return _isChecked;
                }
                set
                {
                    if (_isChecked != value)
                    {
                        _isChecked = value;
                        NotifyPropertyChanged("IsChecked");
                    }
                }
            }

            public CheckableRecipient(Recipient recipient, bool isChecked = false)
            {
                _recipient = recipient;
                _isChecked = isChecked;
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

        }
        private List<Recipient> _recipients { get; set; }
        private List<CheckableRecipient> _checkableRecipients { get; set; }
        public List<CheckableRecipient> Recipients
        {
            get
            {
                return _checkableRecipients;
            }
            set
            {
                _checkableRecipients = value;
                NotifyPropertyChanged("Recipients");
            }
        }

        public MainViewModel(INavigationService ns, IApplication app) : base(ns, app)
        {
            _recipients = _app.GetRecipients();
            _checkableRecipients = _recipients.ConvertAll(r => new CheckableRecipient(r));
            if (!_app.HasPrivateKey())
            {
                _navService.OpenWindow(ViewModelFactory.CreateCreatePrivateKeyViewModel());
            }
            CipherModes = new List<CipherModeCheckBoxable>();
            CipherModes.Add(new CipherModeCheckBoxable(CipherMode.CBC.ToString(), 1));
            CipherModes.Add(new CipherModeCheckBoxable(CipherMode.CFB.ToString(), 4));
            CipherModes.Add(new CipherModeCheckBoxable(CipherMode.ECB.ToString(), 2));
            //CipherModes.Add(new CipherModeCheckBoxable(CipherMode.OFB.ToString(), 3));
            NotifyPropertyChanged(nameof(CipherModes));
        }

        private ICommand _chooseFile;
        public ICommand ChooseFile => _chooseFile ?? (_chooseFile = new RelayCommand(OpenFilePickerDialog));

        private void OpenFilePickerDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            {
                if (openFileDialog.ShowDialog() == true)
                {
                    SelectedFileName = openFileDialog.FileName;
                    EncryptedFileName = openFileDialog.SafeFileName;
                    NotifyPropertyChanged(nameof(SelectedFileName));
                    NotifyPropertyChanged(nameof(EncryptedFileName));
                }
            }
        }

        private ICommand _changeSettings;
        public ICommand ChangeSettings => _changeSettings ?? (_changeSettings = new RelayCommand(NavigateToSettingsView));

        private void NavigateToSettingsView()
        {
            _app.ChangeRecipient();
            _navService.OpenWindow(ViewModelFactory.CreateSettingsViewModel());
        }

        private ICommand _showTransfers;
        public ICommand ShowTransfers => _showTransfers ?? (_showTransfers = new RelayCommand(NavigateToTransfersView));

        private void NavigateToTransfersView()
        {
            _app.AddRecipient();
            _navService.OpenWindow(ViewModelFactory.CreateTransfersViewModel(), false);
        }

        private ICommand _encrypt2Send;
        public ICommand Encrypt2Send => _encrypt2Send ?? (_encrypt2Send = new RelayCommand(EncryptAndSend));

        private void EncryptAndSend()
        {
            List<Recipient> selectedRecipients = _checkableRecipients
                .FindAll(r => r.IsChecked == true)
                .ConvertAll(r => r.Recipient);
            _app.ChangeEncryptionSettings((CipherMode)SelectedChiperMode.ModeNumber);
            _app.EncryptAndSend(selectedRecipients, SelectedFileName, EncryptedFileName);
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            _app.Shutdown();
        }

        protected override void ExitWindow()
        {
            _navService.CloseWindow(typeof(MainViewModel));
        }
    }
}
