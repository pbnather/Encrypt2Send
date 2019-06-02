using System.Net;
using System.Security.Cryptography;

namespace Server.Model
{
    public class Recipient
    {
        private string _ipAddress { get; set; }
        private int _port { get; set; }

        public string Name { get; set; }
        public RSAParameters PublicKeyParams { get; set; }

        public string IpAddress
        {

            get
            {
                return _ipAddress;
            }
            set
            {
                _ipAddress = value;
            }
        }

        public int Port
        {
            get
            {
                return _port;
            }
            set
            {
                _port = value;
            }
        }

        public Recipient() { }
        public Recipient(string name, RSAParameters publicKeyParams)
        {
            Name = name;
            PublicKeyParams = publicKeyParams;
        }
        public Recipient(string name, RSAParameters publicKeyParams, IPAddress ipAddress, int port)
        {
            Name = name;
            PublicKeyParams = publicKeyParams;
            _ipAddress = ipAddress.ToString();
            _port = port;
        }
    }
}
