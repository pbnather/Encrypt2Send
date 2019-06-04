using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Server.Model
{
    class TransferJob : INotifyPropertyChanged
    {
        public Thread _thread { get; set; }
        public TcpClient _client { get; set; }
        public JobStatus Type { get; private set; }
        public JobProgress Progress { get; set; }
        public Recipient Recipient { get; set; }

        private string _fileSize { get; set; }
        public string FileName
        {
            get
            {
                return _fileSize;
            }
            set
            {
                if (_fileSize != value)
                {
                    _fileSize = value;
                    NotifyPropertyChanged(nameof(FileName));
                }
            }
        }

        public class JobProgress : INotifyPropertyChanged
        {
            private double _value { get; set; }
            private double _maximum { get; set; }
            private double _minimum { get; set; }
            public double Value { get
                {
                    return _value;
                }
                set
                {
                    if (_value != value)
                    {
                        _value = value;
                        NotifyPropertyChanged(nameof(Value));
                    }
                }
            }
            public double Maximum
            {
                get
                {
                    return _maximum;
                }
                set
                {
                    if (_maximum != value)
                    {
                        _maximum = value;
                        NotifyPropertyChanged(nameof(Maximum));
                    }
                }
            }
            public double Minimum
            {
                get
                {
                    return _minimum;
                }
                set
                {
                    if (_minimum != value)
                    {
                        _minimum = value;
                        NotifyPropertyChanged(nameof(Minimum));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public enum JobStatus
        {
            DOWNLOADING,
            UPLOADING,
            FINISHED
        }

        public TransferJob(Thread job, TcpClient client, JobStatus jobType)
        {
            _thread = job;
            _thread.IsBackground = true;
            _client = client;
            Type = jobType;
            Progress = new JobProgress();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Start()
        {
            if (!_thread.IsAlive && _thread.ThreadState != ThreadState.Stopped)
            {
                _thread.Start(this);
            }
        }
    }
}
