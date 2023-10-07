using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Frontend.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Shared.MessengerModels;

namespace Frontend.Browser
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddAuthorizationCore();

            builder.Services.AddScoped<AuthorizeApi>();
            builder.Services.AddScoped<MessengerApiHelper>();
            builder.Services.AddScoped<AuthenticationStateProvider, IdentityAuthenticationStateProvider>();


            var app = builder.Build();
            
                await app.RunAsync();
        }
    }

    public class AuthorizeApi
    {
        private readonly ILocalStorageService _localStorage;
        private readonly MessengerApiHelper _apiHelper;
        private readonly IdentityAuthenticationStateProvider _authenticationStateProvider;

        public AuthorizeApi(AuthenticationStateProvider authenticationStateProvider,
            ILocalStorageService localStorage, MessengerApiHelper apiHelper)
        {
            _localStorage = localStorage;
            _apiHelper = apiHelper;
            _authenticationStateProvider = (IdentityAuthenticationStateProvider)authenticationStateProvider;
        }

        public async Task<bool> Login(string login, string password)
        {
            try
            {
                var securestr = new SecureString();
                foreach (var c in password)
                {
                    securestr.AppendChar(c);
                }
                var tokenResponse = await _apiHelper.LoginAsync(new AuthParams()
                {
                    Login = login,
                    Password = password
                });

                if (!string.IsNullOrEmpty(tokenResponse))
                {
                    var token = tokenResponse;
                    Console.WriteLine(token);
                    await _localStorage.SetItemAsync("token", token);
                    _authenticationStateProvider.MarkUserAsAuthenticated(token);

                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return false;
        }

        public async Task<bool> Register(string name, string username, string password)
        {
            try
            {
                var securestr = new SecureString();
                foreach (var c in password)
                {
                    securestr.AppendChar(c);
                }
                var tokenResponse = await _apiHelper.RegisterAsync(new AuthParams
                {
                    Name = name,
                    Login = username,
                    Password = password
                });

                if (!string.IsNullOrEmpty(tokenResponse))
                {
                    var token = tokenResponse;

                    await _localStorage.SetItemAsync("token", token);
                    _authenticationStateProvider.MarkUserAsAuthenticated(token);

                    return true;
                }
            }
            catch (Exception e)
            {
            }

            return false;
        }
    }

    public class IdentityAuthenticationStateProvider : AuthenticationStateProvider
    {
        #region Private Fields

        private readonly ILocalStorageService _localStorage;
        private readonly MessengerApiHelper _apiHelper;

        #endregion

        #region Constructor

        public IdentityAuthenticationStateProvider(ILocalStorageService localStorage, MessengerApiHelper apiHelper)
        {
            _localStorage = localStorage;
            _apiHelper = apiHelper;
        }

        #endregion

        #region Public Methods

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _localStorage.GetItemAsync<string>("Token");

            return await IsTokenValid(token)
                ? Jwt.GetStateFromJwt(token)
                : Empty();
        }

        public void MarkUserAsAuthenticated(string token)
        {
            var authState = Task.FromResult(Jwt.GetStateFromJwt(token));
            NotifyAuthenticationStateChanged(authState);
        }

        public async Task MarkLogout()
        {
            await _localStorage.RemoveItemAsync("Token");
            NotifyAuthenticationStateChanged(Task.FromResult(Empty()));
        }

        #endregion

        #region Private Fields

        private async Task<bool> IsTokenValid(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            try
            {
                var authUser =
                    await _apiHelper.GetUserProfileAsync(new GetClientInfoParams()
                    {
                        Token = token
                    });

                if (authUser != null)
                    return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return false;
        }

        private static AuthenticationState Empty()
            => new(new ClaimsPrincipal(new ClaimsIdentity()));

        #endregion
    }
}
