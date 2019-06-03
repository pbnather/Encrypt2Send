using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Model;

namespace Server.ViewModel
{
    class TransfersViewModel : AbstractViewModel
    {
        public ItemsChangeObservableCollection<TransferJob> Transfers { get; set; }

        public TransfersViewModel(INavigationService ns, IApplication app) : base(ns, app)
        {
            Transfers = _app.GetTransfers();
        }

        protected override void ExitWindow()
        {
            _navService.CloseWindow(typeof(TransfersViewModel));
        }
    }
}
