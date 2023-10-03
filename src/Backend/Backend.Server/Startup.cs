using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Shared.MessengerModels;
using System.Security;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Server
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<MyHub>("/");
            });
        }
    }

    public class MyHub : Hub
    {
        private static readonly List<User> _clients = new List<User>();

        #region Private Fields

        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<UserDbModel> _userManager;
        private readonly TokenParameters _tokenParameters;


        #endregion

        #region Constructor
        public MyHub(UserManager<UserDbModel> userManager, TokenParameters tokenParameters, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _tokenParameters = tokenParameters;
            _roleManager = roleManager;
        }

        #endregion

        public async Task<AuthorizeResult> RegisterAsync(AuthParams auth)
        {
            if (string.IsNullOrWhiteSpace(auth.Login))
            {
                Console.WriteLine("!! Invalid login");
                return null;
            }

            UserDbModel user = new()
            {
                StringId = Guid.NewGuid().ToString(),
                UserName = auth.Login,
                Name = auth.Name,
                //Picture = auth.Picture
            };

            var result = await _userManager.CreateAsync(user, auth.Password.SecureStringToString());

            if (result.Succeeded)
            {
                var userIdentity = await _userManager.FindByNameAsync(user.UserName);

                var token = await userIdentity.GenerateJwtToken(_tokenParameters, _roleManager, _userManager);

                var client = new User
                {
                    Id = userIdentity.StringId,
                    Name = userIdentity.Name,
                    //Picture = userIdentity.Picture
                };

                _clients.Add(client);
                Console.WriteLine($"++ {userIdentity.StringId} registered and logged in");
                var authorizeResult = new AuthorizeResult()
                {
                    User = client,
                    Clients = _clients,
                    Token = token
                };
                await Clients.Others.SendAsync("Registered", authorizeResult);
                return authorizeResult;
            }

            Console.WriteLine($"!! {user.StringId} is not registered and logged");
            return null;
        }

        public async Task<AuthorizeResult> LoginAsync(AuthParams auth)
        {
            var user = await _userManager.FindByNameAsync(auth.Login);

            if (user == null)
            {
                Console.WriteLine("!! Invalid login");
                return null;
            }

            var isValidPassword = await _userManager.CheckPasswordAsync(user, auth.Password.SecureStringToString());

            if (!isValidPassword)
            {
                Console.WriteLine("!! Invalid password");
                return null;
            }

            var client = new User
            {
                Id = user.StringId,
                Name = user.Name,
                //Picture = user.Picture
            };

            var token = await user.GenerateJwtToken(_tokenParameters, _roleManager, _userManager);

            _clients.Add(client);
            Console.WriteLine($"++ {user.StringId} logged in");
            var result = new AuthorizeResult()
            {
                User = client,
                Clients = _clients,
                Token = token
            };
            await Clients.Others.SendAsync("Login", result);
            return result;
        }

        public async Task<ClientInfo> GetClientInfoAsync(GetClientInfoParams getClientInfoParams)
        {
            var user = await _userManager.GetUserAsync(Context.User);

            SecurityToken t = null!;

            new ChatJwtValidator(_tokenParameters).ValidateToken(getClientInfoParams.Token, null, out t);

            if (user == null)
            {
                Console.WriteLine("!! No access");
                return null;
            }

            Console.WriteLine("@@ Info got");
            return new ClientInfo()
            {
                Token = getClientInfoParams.Token,
                User = new User()
                {
                    Id = user.StringId,
                    Name = user.Name,
                    //Picture = user.Picture
                }
            };
        }

        public async Task LogoutAsync()
        {
            var user = await _userManager.GetUserAsync(Context.User);
            if (user != null)
            {
                User client = _clients.FirstOrDefault(u => u.Id == user.StringId);
                _clients.Remove(client);
                await Clients.Others.SendAsync("ParticipantLogout", client);
                Console.WriteLine($"-- {user.StringId} logged out");
            }
        }

        public async Task BroadcastTextMessageAsync(string message)
        {
            var user = await _userManager.GetUserAsync(Context.User);
            if (user != null && !string.IsNullOrEmpty(message))
            {
                User client = _clients.FirstOrDefault(u => u.Id == user.StringId);
                await Clients.Others.SendAsync("BroadcastTextMessage", new MessageSentReceivedEventArgs()
                {
                    Message = message,
                    Sender = client
                });
                Console.WriteLine($"== message \" {message} \" received by: {client.Id}");
            }
        }

        public async Task UnicastTextMessage(User user, string message)
        {
            var sender = await _userManager.GetUserAsync(Context.User);
            if(sender != null)
            {
                User client = _clients.FirstOrDefault(u => u.Id == sender.StringId);
                if (user.Id != client.Id &&
                !string.IsNullOrEmpty(message) &&
                _clients.FirstOrDefault(u => u.Id == user.Id) != null)
                {
                    var recipient = _clients.FirstOrDefault(u => u.Id == user.Id);
                    await Clients.Client(recipient.Id).SendAsync("UnicastTextMessage", new MessageSentReceivedEventArgs()
                    {
                        Message = message,
                        Sender = client
                    });
                    Console.WriteLine($"== message \" {message} \" sent to: {recipient.Id}");
                }
            }
            
        }

        public async Task TypingAsync(User recepient)
        {
            if (recepient == null) return;
            var sender = await _userManager.GetUserAsync(Context.User);
            User senderClient = _clients.FirstOrDefault(u => u.Id == sender.StringId);
            User resepientClient = _clients.FirstOrDefault(u => u.Id == recepient.Id);
            await Clients.Client(resepientClient.Id).SendAsync("ParticipantTyping", senderClient);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var user = await _userManager.GetUserAsync(Context.User);
            if (user != null)
            {
                User client = _clients.FirstOrDefault(u => u.Id == user.StringId);
                _clients.Remove(client);
                await Clients.Others.SendAsync("ParticipantLogout", client);
                Console.WriteLine($"-- {user.StringId} logged out");
            }
        }
    }
}
