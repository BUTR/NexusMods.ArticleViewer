using Blazored.LocalStorage;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NexusMods.ArticleViewer.Client.Extensions;
using NexusMods.ArticleViewer.Client.Helpers;
using NexusMods.ArticleViewer.Client.Options;
using NexusMods.ArticleViewer.Shared.Helpers;

using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace NexusMods.ArticleViewer.Client
{
    public class Program
    {
        public static Task Main(string[] args) => CreateHostBuilder(args).Build().RunAsync();

        public static WebAssemblyHostBuilder CreateHostBuilder(string[] args) => WebAssemblyHostBuilder
            .CreateDefault(args)
            .AddRootComponent<App>("#app")
            .ConfigureServices((builder, services) =>
            {
                services.Configure<BackendOptions>(builder.Configuration.GetSection("Backend"));

                var assemblyName = Assembly.GetEntryAssembly()?.GetName();
                var userAgent = $"{assemblyName?.Name ?? "NexusMods.ArticleViewer.Client"} v{Assembly.GetEntryAssembly()?.GetName().Version}";
                services.AddScoped(_ => new HttpClient
                {
                    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
                    DefaultRequestHeaders =
                    {
                        {"User-Agent", userAgent}
                    }
                });
                services.AddHttpClient("Backend", (sp, client) =>
                {
                    var backendOptions = sp.GetRequiredService<IOptions<BackendOptions>>().Value;
                    client.BaseAddress = new Uri(backendOptions.Endpoint);
                    client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                });

                services.AddSingleton<DefaultJsonSerializer>();

                services.AddScoped<BackendAPIClient>();

                services.AddBlazoredLocalStorage();

                services.AddAuthorizationCore();
                services.AddScoped<AuthenticationStateProvider, SimpleAuthenticationStateProvider>();
            });
    }
}