using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Server.Model
{
    class Application : IApplication
    {
        private readonly static string APP_NAME = "Encrypt2Send";
        private readonly static string PB_KEY_DIR = "PublicKeys";
        private readonly static string PR_KEY_DIR = "PrivateKeys";
        private readonly static int BUFFER_SIZE = 1024;
        private List<Recipient> _recipients { get; set; }
        private string AppDirectory { get; set; }
        private string PublicKeysDirectory => Path.Combine(AppDirectory, PB_KEY_DIR);
        private string PrivateKeyDirectory => Path.Combine(AppDirectory, PR_KEY_DIR);
        private AesManaged _aes { get; set; }

        public byte[] key = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };
        public byte[] IV = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };


        public Application()
        {
            _recipients = new List<Recipient>();
            _aes = new AesManaged
            {
                Mode = CipherMode.CBC,
                KeySize = 128,
                Padding = PaddingMode.ISO10126,
                BlockSize = 128
            };

            CreateAppDirectoryIfAbsent();

            // Create dummy public key
            using(RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                var rsaParameters = rsa.ExportParameters(false);
                string name = "test@email.com";
                Recipient r = new Recipient(name, rsaParameters);
                System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(typeof(Recipient));
                string path = Path.Combine(AppDirectory, PB_KEY_DIR) + "/serializedpk.xml";
                var file = File.Create(path);
                writer.Serialize(file, r);
                file.Close();
            }
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                var rsaParameters = rsa.ExportParameters(false);
                string name = "greeter@test.com";
                Recipient r = new Recipient(name, rsaParameters);
                System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(typeof(Recipient));
                string path = Path.Combine(AppDirectory, PB_KEY_DIR) + "/serialized2pk.xml";
                var file = File.Create(path);
                writer.Serialize(file, r);
                file.Close();
            }

            ImportPublicKeysFromAppDirectory();
            //ImportPrivateKeyFromAppDirectory();

            //check dummy recipient
            foreach(Recipient rr in _recipients)
            {
                Console.WriteLine(rr.Name);
            }

            if (System.Windows.MessageBox.Show("Server?", "Confirm", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
            {
                TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 6666);
                server.Start();

                TcpClient client = server.AcceptTcpClient();
                NetworkStream netStream = client.GetStream();
                string path = Path.Combine(AppDirectory, PB_KEY_DIR) + "/file.cpp";
                FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);

                byte[] buffer = new byte[BUFFER_SIZE];

                netStream.Read(buffer, 0, 4);
                int keySize = BitConverter.ToInt32(buffer, 0);
                netStream.Read(buffer, 0, 4);
                int blockSize = BitConverter.ToInt32(buffer, 0);
                netStream.Read(buffer, 0, 4);
                CipherMode cipherMode = (CipherMode) BitConverter.ToInt32(buffer, 0);
                netStream.Read(buffer, 0, 4);
                PaddingMode paddingMode = (PaddingMode) BitConverter.ToInt32(buffer, 0);
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
                while((bytesReceived = netStream.Read(buffer, 0, buffer.Length))>0)
                {
                    cryptoStream.Write(buffer, 0, bytesReceived);
                }

                fileStream.Close();
                netStream.Close();
                client.Close();

            }
           
        }

        ~Application()
        {
            Console.Write("Deinitialization");
        }

        private void CreateAppDirectoryIfAbsent()
        {
            var localDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            AppDirectory = Path.Combine(localDirectory, APP_NAME);
            if (!Directory.Exists(AppDirectory))
            {
                Directory.CreateDirectory(AppDirectory);
            }
            Directory.CreateDirectory(PublicKeysDirectory);
            Directory.CreateDirectory(PrivateKeyDirectory);
        }

        private void ImportPublicKeysFromAppDirectory()
        {
            if(Directory.Exists(PublicKeysDirectory)) {
                System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(Recipient));
                foreach (var file in Directory.GetFiles(PublicKeysDirectory, "*pk.xml"))
                {
                    using (StreamReader key = new StreamReader(file))
                    {
                        Recipient recipient = (Recipient)reader.Deserialize(key);
                        _recipients.Add(recipient);
                    }
                }
            }
        }

        public List<Recipient> GetRecipients()
        {
            return _recipients;
        }

        public void EncryptAndSend(List<Recipient> recipients, string file, string newFilename)
        {
            if(File.Exists(file))
            {
                TcpClient connection = new TcpClient("127.0.0.1", 6666);
                NetworkStream netStream = connection.GetStream();
                FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);

                int keySize = _aes.KeySize;
                int blockSize = _aes.BlockSize;
                int cipherMode = (int) _aes.Mode;
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

                while(remainingFileSize > 0)
                {
                    if (remainingFileSize > BUFFER_SIZE) packetSize = BUFFER_SIZE;
                    else packetSize = remainingFileSize;
                    remainingFileSize -= packetSize;

                    buffer = new byte[packetSize];
                    cryptoStream.Read(buffer, 0, packetSize);
                    if(remainingFileSize == 0 && residuum != 0) netStream.Write(buffer, 0, packetSize - (16 - residuum));
                    else netStream.Write(buffer, 0, packetSize);
                }

                cryptoStream.Close();
                fileStream.Close();
                netStream.Close();
                connection.Close();
            }
        }

        public void ChangeEncryptionSettings(CipherMode mode, int blockSize = -1)
        {
            throw new NotImplementedException();
        }

        public bool HasPrivateKey()
        {
            if (Directory.GetFiles(PrivateKeyDirectory, "myprivatekey.xml").Length == 1) return true;
            else return false;
        }

        public void GeneratePrivateKey(string password)
        {
            byte[] passwordHash = GetPasswordHash(password);

            using (AesManaged aes = GetAesForPrivateKey(passwordHash))
            {
                string file = Path.Combine(PrivateKeyDirectory) + "/myprivatekey.xml";
                FileStream fileStream = new FileStream(file, FileMode.Create, FileAccess.ReadWrite);
                CryptoStream cryptoStream = new CryptoStream(fileStream, aes.CreateEncryptor(), CryptoStreamMode.Write);

                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    RSAParameters privateKey = rsa.ExportParameters(true);
                    RSAParameters publicKey = rsa.ExportParameters(false);
                    System.Xml.Serialization.XmlSerializer rsaWriter = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
                    rsaWriter.Serialize(cryptoStream, privateKey);
                    System.Xml.Serialization.XmlSerializer recipientWriter = new System.Xml.Serialization.XmlSerializer(typeof(Recipient));
                    Recipient recipient = new Recipient("myprivatekey", publicKey);
                    string path = Path.Combine(PublicKeysDirectory) + "/mypublickey.xml";
                    var publicKeyFile = File.Create(path);
                    recipientWriter.Serialize(publicKeyFile, recipient);
                    publicKeyFile.Close();
                }

                cryptoStream.Close();
                fileStream.Close();
            }
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

        private RSAParameters DecryptPrivateKey(string password)
        {
            RSAParameters rsaParameters = new RSAParameters();
            byte[] passwordHash = GetPasswordHash(password);
            using(AesManaged aes = GetAesForPrivateKey(passwordHash))
            {
                string file = Path.Combine(PrivateKeyDirectory) + "/myprivatekey.xml";
                FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
                CryptoStream cryptoStream = new CryptoStream(fileStream, aes.CreateDecryptor(), CryptoStreamMode.Read);

                System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
                rsaParameters = (RSAParameters) reader.Deserialize(cryptoStream);

                cryptoStream.Close();
                fileStream.Close();
            }
            return rsaParameters;
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
    }
}
