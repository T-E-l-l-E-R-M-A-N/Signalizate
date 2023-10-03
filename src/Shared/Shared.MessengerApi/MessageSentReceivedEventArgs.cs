namespace Shared.MessengerModels
{
    public class MessageSentReceivedEventArgs
    {
        public User Sender { get; set; }
        public string Message { get; set; }
    }
}