namespace Shared.MessengerModels
{
    public class Message
    {
        public string Text { get; set; }
        public string Id { get; set; }
        public User Sender { get; set; }
        public User Target { get; set; }
        public byte[] Picture { get; set; }
        public string RoomId { get; set; }
    }
}