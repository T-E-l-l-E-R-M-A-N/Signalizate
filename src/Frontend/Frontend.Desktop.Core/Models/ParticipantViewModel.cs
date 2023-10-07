using System.Collections.Generic;
using System.Collections.ObjectModel;
using Prism.Mvvm;

namespace Frontend.Desktop.Core
{
    public class ParticipantViewModel : BindableBase
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public List<UserViewModel> Members { get; set; }
        public ObservableCollection<MessageViewModel> Chatter { get; set; }

        private bool _isLoggedIn = true;
        public bool IsLoggedIn { get; set; }

        private bool _hasSentNewMessage;
        public bool HasSentNewMessage { get; set; }

        private bool _isTyping;
        public bool IsTyping { get; set; }

        public ParticipantViewModel() { Chatter = new ObservableCollection<MessageViewModel>(); }

    }
}