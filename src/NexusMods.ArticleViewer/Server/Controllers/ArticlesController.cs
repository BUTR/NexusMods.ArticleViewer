using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using NexusMods.ArticleViewer.Server.Helpers;
using NexusMods.ArticleViewer.Shared.Models;
using NexusMods.ArticleViewer.Shared.Models.API;

using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NexusMods.ArticleViewer.Server.Controllers
{
    [ApiController, Route("[controller]"), Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ArticlesController : ControllerBase
    {
        public record ModsQuery(int Page, int PageSize);

        private readonly ILogger _logger;
        private readonly SqlHelperArticles _sqlHelperArticles;

        public ArticlesController(ILogger<ArticlesController> logger, SqlHelperArticles sqlHelperArticles)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sqlHelperArticles = sqlHelperArticles ?? throw new ArgumentNullException(nameof(sqlHelperArticles));
        }

        [HttpGet("")]
        public async Task<ActionResult> GetAll([FromQuery] ModsQuery query)
        {
            var page = query.Page;
            var pageSize = Math.Max(Math.Min(query.PageSize, 100), 5);

            var (userModsTotalCount, userMods) = await _sqlHelperArticles.GetAsync((page - 1) * pageSize, pageSize, CancellationToken.None);

            var metadata = new PagingMetadata
            {
                PageSize = pageSize,
                CurrentPage = page,
                TotalCount = userModsTotalCount,
                TotalPages = (int) Math.Ceiling((double) userModsTotalCount / (double) pageSize),
            };

            return StatusCode((int) HttpStatusCode.OK, new PagingResponse<ArticleModel>
            {
                Items = userMods.Select(m => new ArticleModel(m.GameDomain, m.Id, m.Title, m.AuthorId, m.AuthorName, m.CreateDate)),
                Metadata = metadata
            });
        }
    }
}