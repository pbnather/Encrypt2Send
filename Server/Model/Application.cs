using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Server.Model
{
    class Application : IApplication
    {
        private readonly static string APP_NAME = "Encrypt2Send";
        private readonly static string PB_KEY_DIR = "PublicKeys";
        private readonly static string PR_KEY_DIR = "PrivateKeys";
        private readonly static int BUFFER_SIZE = 1024;

        private string _appDirectory { get; set; }
        private string _publicKeysDirectory => Path.Combine(_appDirectory, PB_KEY_DIR);
        private string _privateKeyDirectory => Path.Combine(_appDirectory, PR_KEY_DIR);
        private List<Recipient> _recipients { get; set; }
        private List<TransferJob> _jobs { get; set; }
        private Thread _serverthread { get; set; }
        private AesManaged _aes { get; set; }
        private volatile bool _appIsRunning;

        public Application()
        {
            _recipients = new List<Recipient>();
            _jobs = new List<TransferJob>();
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
                    Thread job = new Thread(() => SendFile(client, file, newFilename))
                    {
                        IsBackground = true
                    };
                    TransferJob send = new TransferJob(job, client, TransferJob.JobType.UPLOAD);
                    send.Start(false);
                }
            }
        }

        public void ChangeEncryptionSettings(CipherMode mode, int blockSize = -1)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public void ChangeRecipient()
        {
            throw new NotImplementedException();
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

                Thread job = new Thread(new ParameterizedThreadStart(ReceiveFile))
                {
                    IsBackground = true
                };
                TransferJob download = new TransferJob(job, client, TransferJob.JobType.DOWNLOAD);
                _jobs.Add(download);
                download.Start();

            }
        }

        private void SendFile(TcpClient connection, string file, string fileName)
        {

            NetworkStream netStream = connection.GetStream();
            FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);

            int keySize = _aes.KeySize;
            int blockSize = _aes.BlockSize;
            int cipherMode = (int)_aes.Mode;
            int paddingMode = (int)_aes.Padding;

            netStream.Write(BitConverter.GetBytes(keySize), 0, 4);
            netStream.Write(BitConverter.GetBytes(blockSize), 0, 4);
            netStream.Write(BitConverter.GetBytes(cipherMode), 0, 4);
            netStream.Write(BitConverter.GetBytes(paddingMode), 0, 4);
            netStream.Write(BitConverter.GetBytes(_aes.Key.Length), 0, 4);
            netStream.Write(BitConverter.GetBytes(_aes.IV.Length), 0, 4);

            netStream.Write(_aes.Key, 0, _aes.Key.Length);
            netStream.Write(_aes.IV, 0, _aes.IV.Length);

            CryptoStream cryptoStream = new CryptoStream(fileStream, _aes.CreateEncryptor(), CryptoStreamMode.Read);

            int fileLength = (int)fileStream.Length;
            int residuum = fileLength % 16;
            int remainingFileSize = residuum == 0 ? fileLength + 16 : fileLength + 32 - residuum;
            int packetSize = 0;
            byte[] buffer;

            while (remainingFileSize > 0)
            {
                if (remainingFileSize > BUFFER_SIZE) packetSize = BUFFER_SIZE;
                else packetSize = remainingFileSize;
                remainingFileSize -= packetSize;

                buffer = new byte[packetSize];
                cryptoStream.Read(buffer, 0, packetSize);
                if (remainingFileSize == 0 && residuum != 0) netStream.Write(buffer, 0, packetSize - (16 - residuum));
                else netStream.Write(buffer, 0, packetSize);
            }

            cryptoStream.Close();
            fileStream.Close();
            netStream.Close();
            connection.Close();
        }

        private void ReceiveFile(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream netStream = client.GetStream();
            string path = Path.Combine(_appDirectory, PB_KEY_DIR) + "/file2.cpp";
            FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);

            byte[] buffer = new byte[BUFFER_SIZE];

            netStream.Read(buffer, 0, 4);
            int keySize = BitConverter.ToInt32(buffer, 0);
            netStream.Read(buffer, 0, 4);
            int blockSize = BitConverter.ToInt32(buffer, 0);
            netStream.Read(buffer, 0, 4);
            CipherMode cipherMode = (CipherMode)BitConverter.ToInt32(buffer, 0);
            netStream.Read(buffer, 0, 4);
            PaddingMode paddingMode = (PaddingMode)BitConverter.ToInt32(buffer, 0);
            netStream.Read(buffer, 0, 4);
            int keyLength = BitConverter.ToInt32(buffer, 0);
            netStream.Read(buffer, 0, 4);
            int ivLength = BitConverter.ToInt32(buffer, 0);

            byte[] k = new byte[keyLength];
            byte[] iv = new byte[ivLength];

            netStream.Read(k, 0, keyLength);
            netStream.Read(iv, 0, ivLength);

            AesManaged aes = new AesManaged()
            {
                KeySize = keySize,
                Key = k,
                IV = iv,
                BlockSize = blockSize,
                Mode = cipherMode,
                Padding = paddingMode
            };

            CryptoStream cryptoStream = new CryptoStream(fileStream, aes.CreateDecryptor(), CryptoStreamMode.Write);

            int bytesReceived;
            while ((bytesReceived = netStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                cryptoStream.Write(buffer, 0, bytesReceived);
            }

            fileStream.Close();
            netStream.Close();
            client.Close();
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

        ~Application()
        {
            Shutdown();
        }
    }
}
