using HtmlAgilityPack;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NexusMods.ArticleViewer.Server.Helpers;
using NexusMods.ArticleViewer.Server.Models.Database;

using Polly;

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ComposableAsync;
using RateLimiter;

namespace NexusMods.ArticleViewer.Server.Services
{
    public class ArticleService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ArticleService(ILogger<ArticleService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var policy = Policy.Handle<Exception>(ex => ex.GetType() != typeof(TaskCanceledException) || ex.GetType() != typeof(OperationCanceledException))
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromMilliseconds(5000),
                    (ex, time) =>
                    {
                        _logger.LogError(ex, "Exception during asko queue processing. Retrying after {retrySeconds} seconds.", time.TotalSeconds);
                    });

            //var timeLimiter = TimeLimiter.GetFromMaxCountByInterval(30, TimeSpan.FromSeconds(1));
            var timeLimiter = TimeLimiter.GetFromMaxCountByInterval(1, TimeSpan.FromSeconds(1));

            while (!stoppingToken.IsCancellationRequested)
            {
                await policy.ExecuteAsync(async token =>
                {
                    using var scope = _scopeFactory.CreateScope();

                    var sqlHelperArticles = scope.ServiceProvider.GetRequiredService<SqlHelperArticles>();

                    var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                    var httpClient = httpClientFactory.CreateClient("NexusMods");

                    var articleId = await sqlHelperArticles.GetLastIdAsync(token) ?? 1;
                    var notFoundArticles = 0;
                    const int notFoundArticlesTreshold = 20;

                    while (true)
                    {
                        await timeLimiter;

                        var response = await httpClient.GetAsync($"mountandblade2bannerlord/articles/{articleId}", token);

                        var doc = new HtmlDocument();
                        doc.Load(await response.Content.ReadAsStreamAsync(token));

                        var errorElement = doc.GetElementbyId($"{articleId}-title");
                        if (errorElement is not null)
                        {
                            notFoundArticles++;
                            articleId++;
                            if (notFoundArticles >= notFoundArticlesTreshold)
                            {
                                break;
                            }
                            continue;
                        }
                        notFoundArticles = 0;

                        var pagetitleElement = doc.GetElementbyId("pagetitle");
                        var titleElement = pagetitleElement.ChildNodes.FindFirst("h1");
                        var title = titleElement.InnerText;

                        var authorElement = doc.GetElementbyId("image-author-name");
                        var authorUrl = authorElement.GetAttributeValue("href", "0");
                        var authorUrlSplit = authorUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        var authorIdText = authorUrlSplit.LastOrDefault() ?? string.Empty;
                        var authorId = int.TryParse(authorIdText, out var authorVal) ? authorVal : 0;
                        var authorText = authorElement.InnerText;

                        var fileinfoElement = doc.GetElementbyId("fileinfo");
                        var dateTimeText1 = fileinfoElement.ChildNodes.FindFirst("div");
                        var dateTimeText2 = dateTimeText1?.ChildNodes.FindFirst("time");
                        var dateTimeText = dateTimeText2?.GetAttributeValue("datetime", "");
                        var dateTime = DateTimeOffset.TryParse(dateTimeText, out var dateTimeVal) ? dateTimeVal : DateTimeOffset.MinValue;

                        await sqlHelperArticles.UpsertAsync(new ArticleTableEntry
                        {
                            Title = title,
                            GameDomain = "mountandblade2bannerlord",
                            Id = articleId,
                            AuthorId = authorId,
                            AuthorName = authorText,
                            CreateDate = dateTime
                        }, token);

                        articleId++;
                    }

                    await Task.Delay(1000 * 60 * 60 * 24, token);
                }, stoppingToken);
            }

            _logger.LogWarning("Application requested service stopping!.");
        }
    }
}
