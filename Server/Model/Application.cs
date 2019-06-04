using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Data;
using System.Windows.Threading;

namespace Server.Model
{
    class Application : IApplication
    {

        private readonly static string APP_NAME = "Encrypt2Send";
        private readonly static string PB_KEY_DIR = "PublicKeys";
        private readonly static string PR_KEY_DIR = "PrivateKeys";
        private readonly static string DOWNLOADS_DIR = "Downloads";
        private readonly static int BUFFER_SIZE = 1024;
        private readonly object Lock = new object();

        private string _appDirectory { get; set; }
        private string _publicKeysDirectory => Path.Combine(_appDirectory, PB_KEY_DIR);
        private string _privateKeyDirectory => Path.Combine(_appDirectory, PR_KEY_DIR);
        private string _downloadsDirectory => Path.Combine(_appDirectory, DOWNLOADS_DIR);
        private List<Recipient> _recipients { get; set; }
        private ItemsChangeObservableCollection<TransferJob> _jobs { get; set; }
        private Thread _serverthread { get; set; }
        private AesManaged _aes { get; set; }
        private Dispatcher _dispatcher { get; set; }
        private volatile bool _appIsRunning;

        public Application()
        {
            _recipients = new List<Recipient>();
            _jobs = new ItemsChangeObservableCollection<TransferJob>();
            BindingOperations.EnableCollectionSynchronization(_jobs, Lock);


            _jobs.Add(new TransferJob(new Thread((object obj) =>
            {
                TransferJob l = (TransferJob)obj;
                TransferJob.JobProgress p = l.Progress;
                p.Maximum = 10;
                p.Minimum = 0;
                for (int i = 0; i < 10; i++)
                {
                    p.Value = i + 1;
                    Thread.Sleep(1000);

                }
            }), null, TransferJob.JobStatus.DOWNLOADING));

            _aes = new AesManaged
            {
                Mode = CipherMode.CBC,
                KeySize = 128,
                Padding = PaddingMode.ISO10126,
                BlockSize = 128
            };

            CreateAppDirectoryIfAbsent();
            ImportPublicKeysFromAppDirectory();

            _appIsRunning = true;
            _serverthread = new Thread(new ThreadStart(StartServer));
            _serverthread.IsBackground = true;
            _serverthread.Start();
        }

        public List<Recipient> GetRecipients()
        {
            return _recipients;
        }

        public void EncryptAndSend(List<Recipient> recipients, string file, string newFilename)
        {
            if (File.Exists(file))
            {
                foreach(Recipient recipient in recipients)
                {
                    TcpClient client = new TcpClient(recipient.IpAddress, 6666);
                    Thread job = new Thread(new ParameterizedThreadStart(SendFile));
                    TransferJob send = new TransferJob(job, client, TransferJob.JobStatus.UPLOADING);
                    _jobs.Add(send);
                    send.SavedFile = file;
                    send.Recipient = recipient;
                    send.Start();
                }
            }
        }

        public void ChangeEncryptionSettings(CipherMode mode, int blockSize = -1)
        {
            _aes.Mode = mode;
        }

        public bool HasPrivateKey()
        {
            if (Directory.GetFiles(_privateKeyDirectory, "myprivatekey.xml").Length == 1) return true;
            else return false;
        }

        public void GeneratePrivateKey(string password)
        {
            byte[] passwordHash = GetPasswordHash(password);

            using (AesManaged aes = GetAesForPrivateKey(passwordHash))
            {
                string file = Path.Combine(_privateKeyDirectory) + "/myprivatekey.xml";
                FileStream fileStream = new FileStream(file, FileMode.Create, FileAccess.ReadWrite);
                CryptoStream cryptoStream = new CryptoStream(fileStream, aes.CreateEncryptor(), CryptoStreamMode.Write);

                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    RSAParameters privateKey = rsa.ExportParameters(true);
                    RSAParameters publicKey = rsa.ExportParameters(false);
                    System.Xml.Serialization.XmlSerializer rsaWriter = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
                    rsaWriter.Serialize(cryptoStream, privateKey);
                    System.Xml.Serialization.XmlSerializer recipientWriter = new System.Xml.Serialization.XmlSerializer(typeof(Recipient));
                    Recipient recipient = new Recipient("mypublickey", publicKey, GetLocalIp(), 6666);
                    string path = Path.Combine(_publicKeysDirectory) + "/mypublickey.xml";
                    var publicKeyFile = File.Create(path);
                    recipientWriter.Serialize(publicKeyFile, recipient);
                    publicKeyFile.Close();
                }

                cryptoStream.Close();
                fileStream.Close();
            }
        }

        public void AddRecipient()
        {
            foreach (TransferJob j in _jobs)
            {
                j.Start();
            }
        }

        public void ChangeRecipient()
        {
            DecryptFile(_jobs[_jobs.Count - 1]);
        }

        public void DeleteRecipient()
        {
            throw new NotImplementedException();
        }

        private void StartServer()
        {
            TcpListener server = new TcpListener(GetLocalIp(), 6666);
            server.Start();

            while (_appIsRunning)
            {

                TcpClient client = server.AcceptTcpClient();

                Thread job = new Thread(new ParameterizedThreadStart(ReceiveFile));
                TransferJob download = new TransferJob(job, client, TransferJob.JobStatus.DOWNLOADING);
                lock(Lock)
                {
                    _jobs.Add(download);
                }
                download.Start();

            }
        }

        private void SendFile(object obj)
        {
            TransferJob transferJob = (TransferJob)obj;

            NetworkStream netStream = transferJob._client.GetStream();
            FileStream fileStream = new FileStream(transferJob.SavedFile, FileMode.Open, FileAccess.Read);
            transferJob.Progress.Minimum = 0;
            transferJob.Progress.Maximum = 100;
            transferJob.Progress.Value = 0;

            int keySize = _aes.KeySize;
            int blockSize = _aes.BlockSize;
            int cipherMode = (int)_aes.Mode;
            int paddingMode = (int)_aes.Padding;
            int fileLength = (int)fileStream.Length;
            int residuum = fileLength % 16;
            int remainingFileSize = residuum == 0 ? fileLength + 16 : fileLength + 32 - residuum;
            int fileSize = remainingFileSize;
            int progressStep = fileSize / 100;
            int packetSize = 0;
            byte[] buffer;

            netStream.Write(BitConverter.GetBytes(fileLength), 0, 4);

            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(transferJob.Recipient.PublicKeyParams);
                netStream.Write(rsa.Encrypt(BitConverter.GetBytes(keySize), false), 0, 128);
                netStream.Write(rsa.Encrypt(BitConverter.GetBytes(blockSize), false), 0, 128);
                netStream.Write(rsa.Encrypt(BitConverter.GetBytes(cipherMode), false), 0, 128);
                netStream.Write(rsa.Encrypt(BitConverter.GetBytes(paddingMode), false), 0, 128);
                netStream.Write(rsa.Encrypt(BitConverter.GetBytes(_aes.Key.Length), false), 0, 128);
                netStream.Write(rsa.Encrypt(BitConverter.GetBytes(_aes.IV.Length), false), 0, 128);
                lock (_aes)
                {
                    _aes.GenerateIV();
                    _aes.GenerateKey();
                    netStream.Write(rsa.Encrypt(_aes.Key, false), 0, 128);
                    netStream.Write(rsa.Encrypt(_aes.IV, false), 0, 128);
                }
            }

            CryptoStream cryptoStream = new CryptoStream(fileStream, _aes.CreateEncryptor(), CryptoStreamMode.Read);

            while (_appIsRunning && remainingFileSize > 0)
            {
                if (remainingFileSize > BUFFER_SIZE) packetSize = BUFFER_SIZE;
                else packetSize = remainingFileSize;
                remainingFileSize -= packetSize;

                buffer = new byte[packetSize];
                cryptoStream.Read(buffer, 0, packetSize);
                netStream.Write(buffer, 0, packetSize);

                transferJob.Progress.Value = (fileSize - remainingFileSize) / progressStep;
            }

            transferJob.Progress.Value = transferJob.Progress.Maximum;

            cryptoStream.Close();
            fileStream.Close();
            netStream.Close();
            transferJob._client.Close();
            transferJob.Type = TransferJob.JobStatus.FINISHED;
        }

        private void ReceiveFile(object obj)
        {
            TransferJob transferJob = (TransferJob)obj;
            NetworkStream netStream = transferJob._client.GetStream();
            string path = _downloadsDirectory + "/" + Guid.NewGuid().ToString();
            FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
            transferJob.SavedFile = path;

            byte[] buffer = new byte[BUFFER_SIZE];

            netStream.Read(buffer, 0, 4);
            int fileSize = BitConverter.ToInt32(buffer, 0);

            int progressStep = fileSize / 100;
            int progress = 0;
            transferJob.Progress.Minimum = 0;
            transferJob.Progress.Maximum = 100;
            transferJob.Progress.Value = 0;
            transferJob.FileSize = fileSize;

            netStream.Read(transferJob.Encryption.keySize, 0, 128);
            netStream.Read(transferJob.Encryption.blockSize, 0, 128);
            netStream.Read(transferJob.Encryption.cipherMode, 0, 128);
            netStream.Read(transferJob.Encryption.paddingMode, 0, 128);
            netStream.Read(transferJob.Encryption.keyLength, 0, 128);
            netStream.Read(transferJob.Encryption.ivLength, 0, 128);
            netStream.Read(transferJob.Encryption.aesKey, 0, 128);
            netStream.Read(transferJob.Encryption.aesIV, 0, 128);

            int bytesReceived;
            while ((bytesReceived = netStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                progress = progress + bytesReceived;

                fileStream.Write(buffer, 0, bytesReceived);
                transferJob.Progress.Value = progress / progressStep;
            }

            transferJob.Progress.Value = transferJob.Progress.Maximum;
            fileStream.Close();
            netStream.Close();
            transferJob._client.Close();
            transferJob.Type = TransferJob.JobStatus.FINISHED;
        }

        private void DecryptFile(object obj)
        {
            TransferJob transferJob = (TransferJob)obj;
            RSAParameters privateKey = transferJob.Rsa;
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(privateKey);

            AesManaged aes = new AesManaged();
            aes.KeySize = BitConverter.ToInt32(rsa.Decrypt(transferJob.Encryption.keySize, false), 0);
            aes.Key = rsa.Decrypt(transferJob.Encryption.aesKey, false);
            aes.IV = rsa.Decrypt(transferJob.Encryption.aesIV, false);
            aes.BlockSize = BitConverter.ToInt32(rsa.Decrypt(transferJob.Encryption.blockSize, false), 0);
            aes.Mode = (CipherMode)BitConverter.ToInt32(rsa.Decrypt(transferJob.Encryption.cipherMode, false), 0);
            aes.Padding = PaddingMode.Zeros;

            FileStream fileStream = new FileStream(transferJob.SavedFile, FileMode.Open, FileAccess.Read);
            FileStream fileStream2 = new FileStream(transferJob.NewFile, FileMode.Create, FileAccess.ReadWrite);
            CryptoStream cryptoStream = new CryptoStream(fileStream, aes.CreateDecryptor(), CryptoStreamMode.Read);

            int progress = 0;
            byte[] buffer = new byte[1024];
            while (progress < transferJob.FileSize)
            {
                if(transferJob.FileSize - progress < 1024)
                {
                    cryptoStream.Read(buffer, 0, transferJob.FileSize - progress);
                    fileStream2.Write(buffer, 0, transferJob.FileSize - progress);
                    progress = transferJob.FileSize;
                } else
                {
                    cryptoStream.Read(buffer, 0, 1024);
                    fileStream2.Write(buffer, 0, 1024);
                    progress = progress + 1024;
                }
            }
            fileStream2.Close();
            fileStream.Close();
        }

        private void CreateAppDirectoryIfAbsent()
        {
            var localDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _appDirectory = Path.Combine(localDirectory, APP_NAME);
            if (!Directory.Exists(_appDirectory))
            {
                Directory.CreateDirectory(_appDirectory);
            }
            Directory.CreateDirectory(_publicKeysDirectory);
            Directory.CreateDirectory(_privateKeyDirectory);
            Directory.CreateDirectory(_downloadsDirectory);
        }

        private void ImportPublicKeysFromAppDirectory()
        {
            if(Directory.Exists(_publicKeysDirectory)) {
                System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(Recipient));
                foreach (var file in Directory.GetFiles(_publicKeysDirectory, "*pk.xml"))
                {
                    using (StreamReader key = new StreamReader(file))
                    {
                        Recipient recipient = (Recipient)reader.Deserialize(key);
                        _recipients.Add(recipient);
                    }
                }
            }
        }

        private byte[] GetPasswordHash(string password)
        {
            byte[] passwordHash;
            byte[] passwordUnicode;
            UnicodeEncoding ue = new UnicodeEncoding();
            passwordUnicode = ue.GetBytes(password);
            SHA256Managed hasher = new SHA256Managed();
            passwordHash = hasher.ComputeHash(passwordUnicode);
            return passwordHash;
        }

        private AesManaged GetAesForPrivateKey(byte[] passwordHash)
        {
            byte[] passwordKey = new byte[16];
            byte[] passwordIV = new byte[16];

            for (int i = 0; i < 16; i++)
            {
                passwordKey[i] = passwordHash[i];
                passwordIV[i] = passwordHash[16 + i];
            }

            AesManaged aes = new AesManaged
            {
                BlockSize = 128,
                KeySize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.ISO10126,
                Key = passwordKey,
                IV = passwordIV
            };
            return aes;
        }

        private RSAParameters DecryptPrivateKey(string password)
        {
            RSAParameters rsaParameters = new RSAParameters();
            byte[] passwordHash = GetPasswordHash(password);
            using(AesManaged aes = GetAesForPrivateKey(passwordHash))
            {
                string file = Path.Combine(_privateKeyDirectory) + "/myprivatekey.xml";
                FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
                CryptoStream cryptoStream = new CryptoStream(fileStream, aes.CreateDecryptor(), CryptoStreamMode.Read);

                System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
                rsaParameters = (RSAParameters) reader.Deserialize(cryptoStream);

                cryptoStream.Close();
                fileStream.Close();
            }
            return rsaParameters;
        }

        private IPAddress GetLocalIp()
        {
            IPAddress ip_Address = null;
            IPHostEntry host = default(IPHostEntry);
            string hostName = null;
            hostName = Environment.MachineName;
            host = Dns.GetHostEntry(hostName);
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ip_Address = ip;
                }
            }
            return ip_Address;
        }

        public void Shutdown()
        {
            _appIsRunning = false;
        }

        public ItemsChangeObservableCollection<TransferJob> GetTransfers()
        {
            return _jobs;
        }

        public void DecryptAndSaveFile(string fileName, string password)
        {
            foreach(TransferJob job in _jobs)
            {
                if(job.Type == TransferJob.JobStatus.DECRYPTING)
                {
                    job.Rsa = DecryptPrivateKey(password);
                    job._thread = new Thread(new ParameterizedThreadStart(DecryptFile));
                    job._thread.IsBackground = true;
                    job.Type = TransferJob.JobStatus.FINISHED;
                    job.NewFile = fileName;
                    job.Start();
                    break;
                }
            }
        }

        ~Application()
        {
            Shutdown();
        }
    }
}
