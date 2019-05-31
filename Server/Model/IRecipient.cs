using System.Security.Cryptography;

namespace Server.Model
{
    interface IRecipient
    {
        RSAParameters PublicKeyParams { get; set; }
        string Name { get; set; }
    }
}
