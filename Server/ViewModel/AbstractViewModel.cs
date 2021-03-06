﻿using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Server.Model;

namespace Server.ViewModel
{
    abstract class AbstractViewModel: INotifyPropertyChanged
    {
        protected readonly INavigationService _navService;
        protected readonly IApplication _app;

        public AbstractViewModel(INavigationService ns, IApplication app)
        {
            _navService = ns;
            _app = app;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private ICommand _cancel;
        public ICommand Cancel => _cancel ?? (_cancel = new RelayCommand(ExitWindow));

        protected abstract void ExitWindow();
    }
}
