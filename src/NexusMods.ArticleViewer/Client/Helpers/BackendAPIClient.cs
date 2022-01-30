using BUTR.NexusMods.Blazor.Core.Services;
using BUTR.NexusMods.Shared.Models.API;

using Microsoft.Extensions.Options;

using NexusMods.ArticleViewer.Shared.Models;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NexusMods.ArticleViewer.Client.Helpers
{
    public class BackendAPIClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITokenContainer _tokenContainer;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private DemoUser? _demoUser;

        public BackendAPIClient(IHttpClientFactory httpClientFactory, ITokenContainer tokenContainer, IOptions<JsonSerializerOptions> jsonSerializerOptions)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _tokenContainer = tokenContainer ?? throw new ArgumentNullException(nameof(tokenContainer));
            _jsonSerializerOptions = jsonSerializerOptions.Value ?? throw new ArgumentNullException(nameof(jsonSerializerOptions));
        }

        public async Task<PagingResponse<ArticleModel>?> GetArticles(int page, CancellationToken ct = default)
        {
            var tokenType = await _tokenContainer.GetTokenTypeAsync(ct);
            if (tokenType?.Equals("demo", StringComparison.OrdinalIgnoreCase) == true)
            {
                _demoUser ??= await DemoUser.CreateAsync();
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

            var token = await _tokenContainer.GetTokenAsync(ct);
            if (token is null)
            {
                return null;
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"Articles?page={page}&pageSize={50}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var httpClient = _httpClientFactory.CreateClient("Backend");
                var response = await httpClient.SendAsync(request, ct);
                return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<PagingResponse<ArticleModel>>(_jsonSerializerOptions, ct) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> RefreshMod(string gameDomain, string modId, CancellationToken ct = default)
        {
            var tokenType = await _tokenContainer.GetTokenTypeAsync(ct);
            if (tokenType?.Equals("demo", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            var token = await _tokenContainer.GetTokenAsync(ct);
            if (token is null)
            {
                return false;
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"Mods/Refresh?gameDomain={gameDomain}&modId={modId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var httpClient = _httpClientFactory.CreateClient("Backend");
                var response = await httpClient.SendAsync(request, ct);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}