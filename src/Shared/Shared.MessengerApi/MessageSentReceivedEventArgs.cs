namespace Shared.MessengerModels
{
    public class MessageSentReceivedEventArgs
    {
        public User Sender { get; set; }
        public string Message { get; set; }
    }
    public class DialogCreatedEventArgs
    {
        public Dialog Dialog { get; set; }
        public Message Message { get; set; }
    }
}