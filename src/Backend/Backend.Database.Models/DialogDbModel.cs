using System.Collections.Generic;

namespace Backend.Database
{
    public class DialogDbModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<UserDbModel> Members { get; set; }
        public bool HasUnread { get; set; }

    }
}