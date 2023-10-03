using System.Security;

namespace Shared.MessengerModels
{
    public class AuthParams
    {
        public string Name { get; set; }
        public string Login { get; set; }
        public SecureString Password { get; set; }
        public string Token { get; set; }
        //public byte[] Picture { get; set; }
    }
}