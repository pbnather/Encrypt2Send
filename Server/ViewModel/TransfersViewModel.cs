using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Server.Model;

namespace Server.ViewModel
{
    class TransfersViewModel : AbstractViewModel
    {
        public int SelectedTransfer { get; set; }

        public ItemsChangeObservableCollection<TransferJob> Transfers { get; set; }

        public TransfersViewModel(INavigationService ns, IApplication app) : base(ns, app)
        {
            Transfers = _app.GetTransfers();
        }

        private ICommand _decryptFile;
        public ICommand Decrypt => _decryptFile ?? (_decryptFile = new RelayCommand(DecryptDialog));

        private void DecryptDialog()
        {
            _app.GetTransfers().ElementAt(SelectedTransfer).Type = TransferJob.JobStatus.DECRYPTING;
            _navService.OpenWindow(ViewModelFactory.CreateDecryptAndSaveFileViewModel());
        }

        protected override void ExitWindow()
        {
            _navService.CloseWindow(typeof(TransfersViewModel));
        }
    }
}
