using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Server.Model
{
    class Application : IApplication
    {
        private readonly static string APP_NAME = "Encrypt2Send";
        private readonly static string PB_KEY_DIR = "PublicKeys";
        private readonly static string PR_KEY_DIR = "PrivateKeys";
        private List<Recipient> _recipients { get; set; }
        private string AppDirectory { get; set; }
        private string PublicKeysDirectory => Path.Combine(AppDirectory, PB_KEY_DIR);
        private string PrivateKeyDirectory => Path.Combine(AppDirectory, PR_KEY_DIR);

        public Application()
        {
            _recipients = new List<Recipient>();

            CreateAppDirectoryIfAbsent();

            // Create dummy public key
            using(RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                var rsaParameters = rsa.ExportParameters(false);
                string name = "test@email.com";
                Recipient r = new Recipient(name, rsaParameters);
                System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(typeof(Recipient));
                string path = Path.Combine(AppDirectory, PB_KEY_DIR) + "/serializedpublickey.xml";
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
                foreach (var file in Directory.GetFiles(PublicKeysDirectory, "*.xml"))
                {
                    using (StreamReader key = new StreamReader(file))
                    {
                        Recipient recipient = (Recipient)reader.Deserialize(key);
                        _recipients.Add(recipient);
                    }
                }
            }
        }

        private void ImportPrivateKeyFromAppDirectory()
        {
            throw new NotImplementedException();
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

        public void GenerateNewIdentity()
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                var parameters = rsa.ExportParameters(false);
            }
        }

        public void RenameIdentity()
        {
            throw new NotImplementedException();
        }

        public void DeleteIdentity()
        {
            throw new NotImplementedException();
        }

        public void ChangeEncryptionSettings()
        {
            throw new NotImplementedException();
        }

        public void SendFile()
        {
            throw new NotImplementedException();
        }
    }
}
