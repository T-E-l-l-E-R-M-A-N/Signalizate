using Prism.Mvvm;

namespace Frontend.Desktop.Core
{
    public class MessageViewModel : BindableBase
    {
        public string RoomId { get; set; }
        public string Message { get; set; }
        public UserViewModel Author { get; set; }
        public bool IsOriginNative { get; set; }

    }
}