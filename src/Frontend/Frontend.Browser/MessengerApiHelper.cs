using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Shared.MessengerModels;

namespace Frontend.Browser
{
    public class MessengerApiHelper
    {
        private HubConnection _connection;
        private readonly string _url = "https://localhost:5001/";

        public List<User> Clients { get; } = new List<User>();

        public EventHandler<MessageSentReceivedEventArgs> MessageReceived;
        public EventHandler<MessageSentReceivedEventArgs> MessageSent;
        public EventHandler<User> Typing;
        public User User { get; set; }
        public async Task ConnectAsync()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(_url)
                .Build();


            


            await _connection.StartAsync();
        }

        public async Task<string> LoginAsync(AuthParams authParams)
        {
            var token = "";
            
            var result = await _connection.InvokeAsync<AuthorizeResult>("LoginAsync", authParams);
            token = result.Token;
            Clients.AddRange(result.Clients);
            return token;
        }

        public async Task<string> RegisterAsync(AuthParams authParams)
        {
            var token = "";
            

            var result = await _connection.InvokeAsync<AuthorizeResult>("RegisterAsync", authParams);
            token = result.Token;
            Clients.AddRange(result.Clients);

            return token;
        }

        public async Task<ClientInfo> GetUserProfileAsync(GetClientInfoParams getClientInfoParams)
        {
            ClientInfo info = null!;
            _connection.On<ClientInfo>("GetClientInfo", async (e) =>
            {
                info = e;
            });

            return info;
        }

        public async Task LogoutAsync()
        {
            await _connection.InvokeAsync("LogoutAsync");
        }

        public async Task SendAsync(User recipient, string message)
        {
            await _connection.InvokeAsync("UnicastTextMessage", recipient, message);
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
            });
            _connection.On<AuthorizeResult>("Login", (e) =>
            {
                Clients.Add(e.User);
            });
            _connection.On<User>("ParticipantLogout", (e) =>
            {
                User d = Clients.Find(x => x.Id == e.Id);
                Clients.Add(d);
            });
        }
    }
}