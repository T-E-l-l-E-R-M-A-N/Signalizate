using System.Collections.Generic;

namespace Shared.MessengerModels
{
    public class AuthorizeResult
    {
        public User User { get; set; }
        public List<User> Clients { get; set; }
        public string Token { get; set; }
    }
}