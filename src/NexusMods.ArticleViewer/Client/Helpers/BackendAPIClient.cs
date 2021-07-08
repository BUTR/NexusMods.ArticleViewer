using NexusMods.ArticleViewer.Shared.Models;
using NexusMods.ArticleViewer.Shared.Models.API;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace NexusMods.ArticleViewer.Client.Helpers
{
    public class BackendAPIClient
    {
        private static JsonSerializerOptions JsonSerializerOptions { get; } = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private DemoUser _demoUser = default!;

        public BackendAPIClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task<string?> Authenticate(string apiKey, string? type = null)
        {
            if (apiKey.Equals("demo", StringComparison.OrdinalIgnoreCase))
            {
                return "demo";
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"Authentication/Authenticate{(string.IsNullOrEmpty(type) ? string.Empty : $"?type={type}")}");
                request.Headers.Add("apikey", apiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var httpClient = _httpClientFactory.CreateClient("Backend");
                var response = await httpClient.SendAsync(request);
                return response.IsSuccessStatusCode ? (await response.Content.ReadFromJsonAsync<JwtTokenResponse>(JsonSerializerOptions))?.Token : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> Validate(string token)
        {
            if (token.Equals("demo", StringComparison.OrdinalIgnoreCase))
            {
                _demoUser ??= await DemoUser.Create();
                return true;
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "Authentication/Validate");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var httpClient = _httpClientFactory.CreateClient("Backend");
                var response = await httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<ProfileModel?> GetProfile(string token)
        {
            if (token.Equals("demo", StringComparison.OrdinalIgnoreCase))
            {
                return _demoUser.Profile;
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "Authentication/Profile");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var httpClient = _httpClientFactory.CreateClient("Backend");
                var response = await httpClient.SendAsync(request);
                return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<ProfileModel>(JsonSerializerOptions) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<PagingResponse<ArticleModel>?> GetArticles(string token, int page)
        {
            if (token.Equals("demo", StringComparison.OrdinalIgnoreCase))
            {
                return new PagingResponse<ArticleModel>
                {
                    Items = _demoUser.Articles,
                    Metadata = new PagingMetadata
                    {
                        PageSize = 10,
                        CurrentPage = 1,
                        TotalCount = _demoUser.Articles.Count,
                        TotalPages = (int) Math.Ceiling((double) _demoUser.Articles.Count / 10d),
                    }
                };
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"Articles?page={page}&pageSize={50}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var httpClient = _httpClientFactory.CreateClient("Backend");
                var response = await httpClient.SendAsync(request);
                return response.IsSuccessStatusCode? await response.Content.ReadFromJsonAsync<PagingResponse<ArticleModel>>(JsonSerializerOptions) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> RefreshMod(string token, string gameDomain, string modId)
        {
            if (token.Equals("demo", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"Mods/Refresh?gameDomain={gameDomain}&modId={modId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var httpClient = _httpClientFactory.CreateClient("Backend");
                var response = await httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}