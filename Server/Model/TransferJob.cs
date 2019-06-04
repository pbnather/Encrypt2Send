using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;

namespace Server.Model
{
    class TransferJob : INotifyPropertyChanged
    {
        public Thread _thread { get; set; }
        public TcpClient _client { get; set; }
        public JobStatus Type { get; set; }
        public JobProgress Progress { get; set; }
        public EncryptionInfo Encryption { get; set; }
        public Recipient Recipient { get; set; }
        public RSAParameters Rsa { get; set; }

        private int _fileSize { get; set; }
        public int FileSize
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
                    NotifyPropertyChanged(nameof(FileSize));
                }
            }
        }
        private string _savedFileName { get; set; }
        public string SavedFile
        {
            get
            {
                return _savedFileName;
            }
            set
            {
                if (_savedFileName != value)
                {
                    _savedFileName = value;
                    NotifyPropertyChanged(nameof(SavedFile));
                }
            }
        }
        private string _newFilename { get; set; }
        public string NewFile
        {
            get
            {
                return _newFilename;
            }
            set
            {
                if (_newFilename != value)
                {
                    _newFilename = value;
                    NotifyPropertyChanged(nameof(NewFile));
                }
            }
        }
        public class EncryptionInfo
        {
            public byte[] keySize = new byte[128];
            public byte[] blockSize = new byte[128];
            public byte[] cipherMode = new byte[128];
            public byte[] paddingMode = new byte[128];
            public byte[] keyLength = new byte[128];
            public byte[] ivLength = new byte[128];
            public byte[] aesKey = new byte[128];
            public byte[] aesIV = new byte[128];
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
            FINISHED,
            DECRYPTING
        }

        public TransferJob(Thread job, TcpClient client, JobStatus jobType)
        {
            _thread = job;
            _thread.IsBackground = true;
            _client = client;
            Type = jobType;
            Progress = new JobProgress();
            Encryption = new EncryptionInfo();
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
