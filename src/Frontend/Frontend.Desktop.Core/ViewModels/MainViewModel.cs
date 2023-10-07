using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Security;
using System.Threading.Tasks;
using Frontend.Shared;
using Prism.Commands;
using Prism.Mvvm;
using Shared.MessengerModels;

namespace Frontend.Desktop.Core
{
    public sealed class MainViewModel : BindableBase
    {
        private readonly MessengerApiHelper _apiHelper;


        private string _token;
        private TaskFactory _ctxTaskFactory;
        public string Name { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Text { get; set; }
        public bool IsRegister { get; set; }
        public bool IsAuthorized { get; set; }
        public bool IsConnected { get; set; }
        public bool IsLoggedIn { get; set; }
        public bool WritingToSuggestion { get; set; }

        public ObservableCollection<UserViewModel> Suggestions { get; set; } = new ObservableCollection<UserViewModel>();
        public UserViewModel SelectedSuggestion { get; set; }
        public ObservableCollection<ParticipantViewModel> DialogCollection { get; set; } =
            new();
        public ParticipantViewModel Client { get; set; }

        public event EventHandler Authorized;
        public MainViewModel(MessengerApiHelper apiHelper)
        {
            _apiHelper = apiHelper;
        }

        public async void Init()
        {

            _ctxTaskFactory = new TaskFactory();

            await _apiHelper.ConnectAsync();
            IsConnected = true;
            Authorized += OnAuthorized;
        }

        private void UserDisconnect(object sender, User e)
        {
            var ptp = Suggestions.FirstOrDefault(p => string.Equals(p.Id, e.Id));
            if (IsAuthorized && ptp == null)
            {
                //var index = Suggestions.IndexOf(Suggestions.FirstOrDefault(d => e.Id == d.Id));
                //_ctxTaskFactory.StartNew(() => Suggestions.RemoveAt(index)).Wait();

                Observable.Timer(TimeSpan.FromMilliseconds(1500)).Subscribe(t => ptp.Online = false);
            }
        }

        private void UserConnect(object sender, User e)
        {
            var ptp = Suggestions.FirstOrDefault(p => string.Equals(p.Id, e.Id));
            if (IsAuthorized && ptp == null)
            {
                //_ctxTaskFactory.StartNew(() => Suggestions.Add(new UserViewModel()
                //{
                //    Name = e.Name,
                //    Id = e.Id
                //})).Wait();

                Observable.Timer(TimeSpan.FromMilliseconds(1500)).Subscribe(t => ptp.Online = true);
            }
        }

        public DelegateCommand LoginCommand => new(async () =>
        {
            var pass = new SecureString();
            foreach (var c in Password)
            {
                pass.AppendChar(c);
            }

            _token = await _apiHelper.LoginAsync(new AuthParams()
            {
                Login = Login,
                Password = Password
            });

            if (_token != "")
            {
                IsAuthorized = true;
                Authorized?.Invoke(this, EventArgs.Empty);
            }
        });

        public DelegateCommand<UserViewModel> OpenWritingToSuggestionCommand => new(async (d) =>
        {
            WritingToSuggestion = true;
            SelectedSuggestion = d;
        });
        public DelegateCommand RegisterCommand => new(async () =>
        {
            //var pass = new SecureString();
            //foreach (var c in Password)
            //{
            //    pass.AppendChar(c);
            //}

            _token = await _apiHelper.RegisterAsync(new AuthParams()
            {
                Name = Name,
                Login = Login,
                Password = Password
            });

            if (_token != "")
            {
                IsAuthorized = true;
                Authorized?.Invoke(this, EventArgs.Empty);
            }
        });

        public DelegateCommand SendCommand => new(async () =>
        {
            try
            {
                var recepient = Client.Name;
                await _apiHelper.SendAsync(new User()
                {
                    Id = Client.Id,
                    Name = Client.Name
                }, Text);

            }
            catch (Exception)
            {
                //ignored
            }
            finally
            {
                MessageViewModel msg = new MessageViewModel
                {
                    Author = new UserViewModel()
                    {
                        Name = _apiHelper.User.Name,
                        Id = _apiHelper.User.Id
                    },
                    Message = Text,
                    IsOriginNative = true
                };
                Client.Chatter.Add(msg);
                DialogCollection.Move(DialogCollection.IndexOf(Client), 0);
                Text = string.Empty;
            }
        });
        public DelegateCommand SendToSuggestionCommand => new(async () =>
        {
            try
            {
                var recepient = Client.Name;
                await _apiHelper.SendAsync(new User()
                {
                    Id = Client.Id,
                    Name = Client.Name
                }, Text);

            }
            catch (Exception)
            {
                //ignored
            }
            finally
            {
                MessageViewModel msg = new MessageViewModel
                {
                    Author = new UserViewModel()
                    {
                        Name = _apiHelper.User.Name,
                        Id = _apiHelper.User.Id
                    },
                    Message = Text,
                    IsOriginNative = true
                };
                Client.Chatter.Add(msg);
                DialogCollection.Move(DialogCollection.IndexOf(Client), 0);
                WritingToSuggestion = false;
                SelectedSuggestion = null;
                Text = string.Empty;
            }
        });
        public DelegateCommand TypingCommand => new(async () =>
        {
            await _apiHelper.TypingAsync(new User()
            {
                Id = Client.Id,
                Name = Client.Name
            });
        });


        public async Task ClientClosing()
        {
            await _apiHelper.LogoutAsync();
        }
        private async void OnAuthorized(object sender, EventArgs e)
        {
            Authorized -= OnAuthorized;

            _apiHelper.Typing += ApiHelperOnTyping;
            _apiHelper.MessageReceived += ApiHelperOnMessageReceived;
            _apiHelper.DialogCreated += _apiHelper_DialogCreated;
            _apiHelper.UserConnect += UserConnect;
            _apiHelper.UserDisconnect += UserDisconnect;

            await _apiHelper.SubsribeToEventsAsync();

            foreach(var dlg in await _apiHelper.GetDialogsAsync(new User
            {
                Id = _apiHelper.User.Id,
                Name = _apiHelper.User.Name
            }))
            {
                var d = new ParticipantViewModel
                {
                    Id = dlg.Id,
                    Name = dlg.Members.Length == 2 ? dlg.Members.FirstOrDefault(x => x != _apiHelper.User.Id) : "Group Chat"
                };

                var messages = await _apiHelper.GetDialogHistoryAsync(dlg);

                d.Chatter = new ObservableCollection<MessageViewModel>(messages.Select(m => new MessageViewModel
                {
                    Author = new UserViewModel
                    {
                        Id = m.Sender.Id,
                        Name = m.Sender.Name
                    },
                    RoomId = m.RoomId,
                    Message = m.Text,
                    IsOriginNative = true
                }));

                DialogCollection.Add(d);
            }

            foreach(var u in _apiHelper.Clients.Where(x=> x.Id != _apiHelper.User.Id))
            {
                Suggestions.Add(new UserViewModel
                {
                    Id = u.Id,
                    Name = u.Name,
                    Online = u.Online
                });
            }

            IsLoggedIn = true;
        }

        private void _apiHelper_DialogCreated(object sender, DialogCreatedEventArgs e)
        {
            _ctxTaskFactory.StartNew(() =>
            {
                var d = new ParticipantViewModel
                {
                    Id = e.Dialog.Id,
                    Name = e.Dialog.Members.Length == 2 ? e.Dialog.Members.FirstOrDefault(x => x != _apiHelper.User.Id) : "Group Chat"
                };

                d.Chatter.Add(new MessageViewModel
                {
                    Author = new UserViewModel
                    {
                        Id = e.Message.Sender.Id,
                        Name = e.Message.Sender.Name
                    },
                    RoomId = e.Message.RoomId,
                    Message = e.Message.Text,
                    IsOriginNative = true
                });

                DialogCollection.Insert(0, d);
            }).Wait();
        }

        private void ApiHelperOnMessageReceived(object s, MessageSentReceivedEventArgs e)
        {
            MessageViewModel cm = new MessageViewModel { Author = new UserViewModel(){Id= e.Sender.Id, Name = e.Sender.Name }, Message = e.Message };
            var sender = DialogCollection.FirstOrDefault(u => string.Equals(u.Id, e.Sender.Id));
            _ctxTaskFactory.StartNew(() => sender.Chatter.Add(cm)).Wait();

            if (!(Client != null && sender.Id.Equals(Client.Id)))
            {
                _ctxTaskFactory.StartNew(() =>
                {
                    sender.HasSentNewMessage = true;
                    DialogCollection.Move(DialogCollection.IndexOf(sender), 0);
                }).Wait();
            }
        }

        private void ApiHelperOnTyping(object sender, User e)
        {
            var person = DialogCollection.FirstOrDefault(p => string.Equals(p.Id, e.Id));
            if (person != null && !person.IsTyping)
            {
                person.IsTyping = true;
                Observable.Timer(TimeSpan.FromMilliseconds(1500)).Subscribe(t => person.IsTyping = false);
            }
        }
    }
    public enum UserModes
    {
        Login,
        Chat
    }
    public enum MessageType
    {
        Broadcast,
        Unicast
    }
}
