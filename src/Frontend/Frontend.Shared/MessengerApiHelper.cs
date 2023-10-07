using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Shared.MessengerModels;

namespace Frontend.Shared
{
    public class MessengerApiHelper
    {
        private HubConnection _connection;
        private readonly string _url = "https://localhost:5001/";

        public List<User> Clients { get; } = new List<User>();

        public event EventHandler<User> UserConnect;
        public event EventHandler<User> UserDisconnect;
        public event EventHandler<DialogCreatedEventArgs> DialogCreated;
        public event EventHandler<MessageSentReceivedEventArgs> MessageReceived;
        public event EventHandler<MessageSentReceivedEventArgs> MessageSent;
        public event EventHandler<User> Typing;
        public User User { get; set; }
        public async Task ConnectAsync()
        {
            _connection = new HubConnectionBuilder()
                 .WithAutomaticReconnect()
                .WithUrl(_url)
                
                .Build();

            Console.WriteLine($"Web Connected as: {_url}");

            _connection.ServerTimeout = TimeSpan.FromSeconds(120);

            await _connection.StartAsync();
        }

        public async Task<string> LoginAsync(AuthParams authParams)
        {
            var token = "";
            
            var result = await _connection.InvokeAsync<AuthorizeResult>("LoginAsync", authParams);
            token = result.Token;
            Clients.AddRange(result.Clients);

            User = result.User;

            return token;
        }

        public async Task<string> RegisterAsync(AuthParams authParams)
        {
            var token = "";

            var result = await _connection.InvokeAsync<AuthorizeResult>("RegisterAsync", authParams);
            token = result.Token;
            Clients.AddRange(result.Clients);

            User = result.User;

            return token;
        }

        public async Task<ClientInfo> GetUserProfileAsync(GetClientInfoParams getClientInfoParams)
        {
            ClientInfo info = await _connection.InvokeAsync<ClientInfo>("GetClientInfo", getClientInfoParams);

            return info;
        }

        public async Task<List<Dialog>> GetDialogsAsync(User request)
        {
            List<Dialog> response = await _connection.InvokeAsync<List<Dialog>>("GetDialogsAsync", request);

            return response;
        }

        public async Task<List<Message>> GetDialogHistoryAsync(Dialog request)
        {
            List<Message> response = await _connection.InvokeAsync<List<Message>>("GetDialogHistoryAsync", request);

            return response;
        }

        public async Task LogoutAsync()
        {
            await UnsubsribeToEventsAsync();
            await _connection.InvokeAsync("LogoutAsync", User.Id);
        }

        public async Task SendAsync(User recipient, string message)
        {
            await _connection.InvokeAsync("UnicastTextMessage", recipient, message);
        }
        public async Task TypingAsync(User c)
        {
            await _connection.InvokeAsync("TypingAsync", c);
        }
        public async Task SubsribeToEventsAsync()
        {
            _connection.On<MessageSentReceivedEventArgs>("BroadcastTextMessage", (e) =>
            {
                MessageReceived?.Invoke(this, e);
            });
            _connection.On<MessageSentReceivedEventArgs>("UnicastTextMessage", (e) =>
            {
                MessageSent?.Invoke(this, e);
            });
            _connection.On<User>("ParticipantTyping", (e) =>
            {
                Typing?.Invoke(this, e);
            });
            _connection.On<AuthorizeResult>("Registered", (e) =>
            {
                Clients.Add(e.User);
                UserConnect.Invoke(this, e.User);
            });
            _connection.On<AuthorizeResult>("Login", (e) =>
            {
                Clients.Add(e.User);
                UserConnect.Invoke(this, e.User);
            });
            _connection.On<User>("ParticipantLogout", (e) =>
            {
                User d = Clients.Find(x => x.Id == e.Id);
                Clients.Remove(d);
                UserDisconnect?.Invoke(this, d);
            });
            _connection.On<DialogCreatedEventArgs>("DialogCreated", (e) =>
            {
                DialogCreated?.Invoke(this, e);
            });
        }
        public async Task UnsubsribeToEventsAsync()
        {
            _connection.Remove("BroadcastTextMessage");
            _connection.Remove("UnicastTextMessage");
            _connection.Remove("ParticipantTyping");
            _connection.Remove("Registered");
            _connection.Remove("Login");
            _connection.Remove("ParticipantLogout");
            _connection.Remove("DialogCreated");
        }
    }
}