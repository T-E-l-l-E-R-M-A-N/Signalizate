using Microsoft.AspNetCore.Identity;

namespace Backend.Database
{
    public class UserDbModel : IdentityUser
    {
        public string StringId { get; set; }
        public string Name { get; set; }
        public bool Online { get; set; }
        //public byte[] Picture { get; set; }
    }
}