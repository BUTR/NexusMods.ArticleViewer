﻿using Blazored.LocalStorage;
using Blazored.SessionStorage;

using BUTR.NexusMods.Blazor.Core.Services;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NexusMods.ArticleViewer.Client.Extensions;
using NexusMods.ArticleViewer.Client.Helpers;
using NexusMods.ArticleViewer.Client.Options;

using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace NexusMods.ArticleViewer.Client
{
    public class Program
    {
        private static JsonSerializerOptions Configure(JsonSerializerOptions opt)
        {
            opt.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            opt.PropertyNameCaseInsensitive = true;
            opt.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
            opt.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            return opt;
        }

        public static Task Main(string[] args) => CreateHostBuilder(args).Build().RunAsync();

        public static WebAssemblyHostBuilder CreateHostBuilder(string[] args) => WebAssemblyHostBuilder
            .CreateDefault(args)
            .AddRootComponent<App>("#app")
            .ConfigureServices((builder, services) =>
            {
                services.Configure<BackendOptions>(builder.Configuration.GetSection("Backend"));

                var assemblyName = Assembly.GetEntryAssembly()?.GetName();
                var userAgent = $"{assemblyName?.Name} v{assemblyName?.Version}";
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

                services.Configure<JsonSerializerOptions>(opt => Configure(opt));

                services.AddScoped<DefaultNexusModsProvider>();
                services.AddScoped<IAuthenticationProvider, DefaultNexusModsProvider>(sp => sp.GetRequiredService<DefaultNexusModsProvider>());
                services.AddScoped<IProfileProvider, DefaultNexusModsProvider>(sp => sp.GetRequiredService<DefaultNexusModsProvider>());
                services.AddScoped<ITokenContainer, LocalStorageTokenContainer>();

                services.AddScoped<BackendAPIClient>();

                services.AddBlazoredLocalStorage();
                services.AddBlazoredSessionStorage();

                services.AddAuthorizationCore();
                services.AddScoped<AuthenticationStateProvider, SimpleAuthenticationStateProvider>();
            });
    }
}