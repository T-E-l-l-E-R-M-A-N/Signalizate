namespace Shared.MessengerModels
{
    public class Dialog
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string[] Members { get; set; }
        public bool HasUnread { get; set; }
    }
}