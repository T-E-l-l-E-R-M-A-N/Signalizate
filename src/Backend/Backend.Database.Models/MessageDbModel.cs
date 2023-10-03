namespace Backend.Database
{
    public class MessageDbModel
    {
        public string Id { get; set; }
        public string Sender { get; set; }
        public string Target { get; set; }
        public string Text { get; set; }
        public string Room { get; set; }
    }
}