using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Shared.MessengerModels;

namespace Backend.Server
{
    public class MyHub : Hub
    {
        private static readonly List<User> _clients = new List<User>();

        #region Private Fields

        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<UserDbModel> _userManager;
        private readonly TokenParameters _tokenParameters;
        private readonly MyDbContext _dbContext;

        #endregion

        #region Constructor
        public MyHub(UserManager<UserDbModel> userManager, TokenParameters tokenParameters, RoleManager<IdentityRole> roleManager, MyDbContext dbContext)
        {
            _userManager = userManager;
            _tokenParameters = tokenParameters;
            _roleManager = roleManager;
            _dbContext = dbContext;
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

            var result = await _userManager.CreateAsync(user, auth.Password);

            if (result.Succeeded)
            {
                var userIdentity = await _userManager.FindByNameAsync(user.UserName);

                var token = await userIdentity.GenerateJwtToken(_tokenParameters, _roleManager, _userManager);

                var client = new User
                {
                    Id = userIdentity.StringId,
                    Name = userIdentity.Name,
                    Online = true
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
                user.Online = true;
                _dbContext.Users.Update(user);

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
            var isValidPassword = await _userManager.CheckPasswordAsync(user, auth.Password);

            if (!isValidPassword)
            {
                Console.WriteLine("!! Invalid password");
                return null;
            }

            var client = new User
            {
                Id = user.StringId,
                Name = user.Name,
                Online = true
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

            user.Online = true;
            _dbContext.Users.Update(user);
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

        public async Task<List<Dialog>> GetDialogsAsync(User request)
        {
            List<Dialog> dialogs = new();

            await Task.Run(() =>
            {
                foreach (var d in _dbContext.Dialogs.Where(x => 
                x.Members.FirstOrDefault(x => x.StringId == request.Id) != null))
                {
                    dialogs.Add(new Dialog
                    {
                        Id = d.Id,
                        Members = d.Members.Select(m => m.StringId).ToArray(),
                        Name = d.Name
                    });
                }
            });

            return dialogs;
        }
        public async Task<List<Message>> GetDialogHistoryAsync(Dialog dlg)
        {
            List<Message> messages = new();

            await Task.Run(() =>
            {
                foreach (var d in _dbContext.Messages.Where(x => x.Id == dlg.Id))
                {
                    var senderDbModel = _dbContext.Users.FirstOrDefault(x => x.StringId == d.Sender);
                    var targetDbModel = _dbContext.Users.FirstOrDefault(x => x.StringId == d.Target);


                    messages.Add(new Message
                    {
                        Id = d.Id,
                        RoomId = d.Room,
                        Sender = new User
                        {
                            Id = senderDbModel.StringId,
                            Name = senderDbModel.Name
                        },
                        Target = new User
                        {
                            Id = targetDbModel.StringId,
                            Name = targetDbModel.Name
                        },
                        Text = d.Text
                    });
                }
            });

            return messages;
        }
        public async Task LogoutAsync(string Id)
        {
            var user = _dbContext.Users.FirstOrDefault(x => x.StringId == Id);
            user.Online = false;
            _dbContext.Users.Update(user);
            if (user != null)
            {
                User client = _clients.FirstOrDefault(u => u.Id == user.StringId);
                client.Online = false;
                await Clients.Others.SendAsync("ParticipantLogout", client);
                _clients.Remove(client);
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

                    var dbModel = new MessageDbModel
                    {
                        Id = Guid.NewGuid().ToString(),
                        Sender = sender.StringId,
                        Target = recipient.Id,
                        Text = message,
                    };

                    if (_dbContext.Dialogs.FirstOrDefault(x =>
                        x.Members.Count == 2 &&
                        x.Members.FirstOrDefault(l => l.StringId == sender.StringId) != null &&
                        x.Members.FirstOrDefault(i => i.StringId == recipient.Id) != null) is DialogDbModel dialog)
                    {
                        dbModel.Room = dialog.Id;
                        await _dbContext.Messages.AddAsync(dbModel);
                    }
                    else
                    {
                        var d = new DialogDbModel
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = string.Join(" - ", recipient.Id, sender.Id),
                            Members = new List<UserDbModel>()
                            {
                                sender,
                                _dbContext.Users.FirstOrDefault(x => x.StringId == recipient.Id)
                            }
                        };

                        dbModel.Room = d.Id;

                        await _dbContext.Dialogs.AddAsync(d);
                        await _dbContext.Messages.AddAsync(dbModel);

                        var senderModel = new User
                        {
                            Id = sender.Id,
                            Name = sender.Name
                        };
                        var targetModel = new User
                        {
                            Id = recipient.Id,
                            Name = recipient.Name
                        };

                        await Clients.Users(new string[] { recipient.Id, sender.StringId }).SendAsync("DialogCreated", new DialogCreatedEventArgs(){ Dialog = new Dialog()
                        {
                            Id = d.Id,
                            Members = d.Members.Select(c => c.StringId).ToArray(),
                            Name = d.Name
                        }, Message = new Message
                        {
                            Id = dbModel.Id,
                            RoomId = dbModel.Room,
                            Text = dbModel.Text,
                            Sender = senderModel,
                            Target = targetModel
                        }
                        });
                    }

                    await _dbContext.SaveChangesAsync();

                    

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