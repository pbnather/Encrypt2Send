﻿using System.Collections.Generic;
using System.Security.Cryptography;

namespace Server.Model
{
    interface IApplication
    {
        ItemsChangeObservableCollection<TransferJob> GetTransfers();

        List<Recipient> GetRecipients();

        void EncryptAndSend(List<Recipient> recipients, string file, string newFilename);

        void ChangeEncryptionSettings(CipherMode mode, int blockSize = -1);

        bool HasPrivateKey();

        void GeneratePrivateKey(string password);

        void DecryptAndSaveFile(string fileName, string password);

        void AddRecipient();

        void ChangeRecipient();

        void DeleteRecipient();

        void Shutdown();
    }
}
