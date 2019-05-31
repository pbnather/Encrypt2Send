namespace Server.Model
{
    interface IApplication
    {
        void SendFile();

        void ChangeEncryptionSettings();

        void AddRecipient();

        void ChangeRecipient();

        void DeleteRecipient();

        void GenerateNewIdentity();

        void DeleteIdentity();

        void RenameIdentity();
    }
}
