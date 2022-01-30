using BUTR.NexusMods.Server.Core.Extensions;
using BUTR.NexusMods.Server.Core.Options;

using Community.Microsoft.Extensions.Caching.PostgreSql;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NexusMods.ArticleViewer.Server.Helpers;
using NexusMods.ArticleViewer.Server.Services;

using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.ArticleViewer.Server
{
    public class Startup
    {
        private const string JwtSectionName = "Jwt";
        private const string AuthenticationSectionName = "Authentication";

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<JwtOptions>(Configuration.GetSection(JwtSectionName));
            services.Configure<AuthenticationOptions>(Configuration.GetSection(AuthenticationSectionName));

            var assemblyName = Assembly.GetEntryAssembly()?.GetName();
            var userAgent = $"{assemblyName?.Name ?? "NexusMods.ArticleViewer.Server"} v{Assembly.GetEntryAssembly()?.GetName().Version}";
            services.AddHttpClient("NexusModsAPI", client =>
            {
                client.BaseAddress = new Uri("https://api.nexusmods.com/");
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            });
            services.AddHttpClient("NexusMods", client =>
            {
                client.BaseAddress = new Uri("https://www.nexusmods.com/");
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            });

            services.AddSingleton<SqlHelperInit>();
            services.AddSingleton<SqlHelperArticles>();

            services.AddHostedService<ArticleService>();
            services.AddHostedService<SqlService>();

            services.AddServerCore(Configuration, JwtSectionName);

            services.AddControllers().AddServerCore().AddJsonOptions(opts =>
            {
                opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });
            services.AddRazorPages();

            services.AddCors(options =>
            {
                options.AddPolicy("Development", builder => builder
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                );
            });

            services.AddDistributedPostgreSqlCache(options =>
            {
                options.ConnectionString = Configuration.GetConnectionString("Main");
                options.SchemaName = "public";
                options.TableName = "nexusmods_cache_entry";
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseCors("Development");
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }
    }
}