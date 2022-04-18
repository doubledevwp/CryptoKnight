using CoinbasePro;
using CoinbasePro.Network.Authentication;

namespace CryptoKnight
{
    public class PrivateClient : CoinbaseProClient
    {
        public PrivateClient(string name, string email, Authenticator authenticator, bool sandbox) : base(authenticator, sandbox)
        {
            this.Name = name;
            this.Email = email;
        }

        public string Name { get; set; }

        /// <summary>
        /// For reports
        /// </summary>
        public string Email { get; set; }
    }
}
