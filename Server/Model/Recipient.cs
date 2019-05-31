using System.Security.Cryptography;

namespace Server.Model
{
    public class Recipient
    {
        public string Name { get; set; }
        public RSAParameters PublicKeyParams { get; set; }

        public Recipient() { }
        public Recipient(string name, RSAParameters publicKeyParams)
        {
            Name = name;
            PublicKeyParams = publicKeyParams;
        }
    }
}
