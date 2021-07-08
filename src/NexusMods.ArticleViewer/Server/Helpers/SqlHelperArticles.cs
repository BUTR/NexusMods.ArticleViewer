using Microsoft.Extensions.Configuration;

using NexusMods.ArticleViewer.Server.Extensions;
using NexusMods.ArticleViewer.Server.Models.Database;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NexusMods.ArticleViewer.Server.Helpers
{
    public class SqlHelperArticles
    {
        /// <summary>
        ///
        /// </summary>
        public static string ArticlesGetCountByUser = @"
SELECT
    COUNT(*)
FROM
    article_entity
;";

        /// <summary>
        /// @offset, @limit
        /// </summary>
        public static string ArticlesGetPaginatedByUser = @"
SELECT
    *
FROM
    article_entity
ORDER BY
    id
OFFSET
    @offset
LIMIT
    @limit
;";

        /// <summary>
        /// @gameDomain, @id, @title, @authorId, @authorName, @createDate
        /// </summary>
        public static string AriclesUpsert = @"
INSERT INTO article_entity(game_domain, id, title, author_id, author_name, create_date)
VALUES (@gameDomain, @id, @title, @authorId, @authorName, @createDate)
ON CONFLICT ON CONSTRAINT article_entity_pkey
DO UPDATE SET title=@title
RETURNING *
;";

        /// <summary>
        /// @offset, @limit
        /// </summary>
        public static string ArticlesGetLastId= @"
SELECT
    id
FROM
    article_entity
ORDER BY
    id DESC
LIMIT
    1
;";


        private readonly IConfiguration _configuration;

        public SqlHelperArticles(IConfiguration configuration) => _configuration = configuration;


        public async Task<int?> GetLastIdAsync(CancellationToken ct = default)
        {
            await using var connection = new NpgsqlConnection(_configuration.GetConnectionString("Main"));
            await connection.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(ArticlesGetLastId, connection);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? await reader.GetNullableInt32Async("id", ct) : null;
        }

        public async Task<ArticleTableEntry?> UpsertAsync(ArticleTableEntry mod, CancellationToken ct = default)
        {
            await using var connection = new NpgsqlConnection(_configuration.GetConnectionString("Main"));
            await connection.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(AriclesUpsert, connection);
            cmd.Parameters.AddWithValue("gameDomain", mod.GameDomain);
            cmd.Parameters.AddWithValue("id", mod.Id);
            cmd.Parameters.AddWithValue("title", mod.Title);
            cmd.Parameters.AddWithValue("authorId", mod.AuthorId);
            cmd.Parameters.AddWithValue("authorName", mod.AuthorName);
            cmd.Parameters.AddWithValue("createDate", mod.CreateDate);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? await CreateArticleTableFromReaderAsync(reader, ct) : null;
        }

        public async Task<(int, List<ArticleTableEntry>)> GetAsync(int skip, int take, CancellationToken ct = default)
        {
            static async Task<int> GetCount(NpgsqlDataReader reader, CancellationToken ct = default)
            {
                return await reader.ReadAsync(ct) ? reader.GetInt32(0) : 0;
            }
            static async IAsyncEnumerable<ArticleTableEntry> GetAllMods(NpgsqlDataReader reader, [EnumeratorCancellation] CancellationToken ct = default)
            {
                while (await reader.ReadAsync(ct))
                {
                    yield return await CreateArticleTableFromReaderAsync(reader, ct);
                }
            }

            await using var connection = new NpgsqlConnection(_configuration.GetConnectionString("Main"));
            await connection.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                ArticlesGetCountByUser +
                ArticlesGetPaginatedByUser, connection);
            cmd.Parameters.AddWithValue("offset", skip);
            cmd.Parameters.AddWithValue("limit", take);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            var count = await GetCount(reader, ct);
            await reader.NextResultAsync(ct);
            var result = await GetAllMods(reader, ct).ToListAsync(ct);
            return (count, result);
        }

        private static async Task<ArticleTableEntry> CreateArticleTableFromReaderAsync(NpgsqlDataReader reader, CancellationToken ct = default)
        {
            var gameDomain = await reader.GetNullableStringAsync("game_domain", ct);
            var modId = await reader.GetNullableInt32Async("id", ct);
            var title = await reader.GetNullableStringAsync("title", ct);
            var authorId = await reader.GetNullableInt32Async("author_id", ct);
            var authorName = await reader.GetNullableStringAsync("author_name", ct);
            var createDate = await reader.GetNullableTimeStampAsync("create_date", ct);

            return new ArticleTableEntry
            {
                GameDomain = gameDomain ?? string.Empty,
                Id = modId ?? 0,
                Title = title ?? string.Empty,
                AuthorId = authorId ?? 0,
                AuthorName = authorName ?? string.Empty,
                CreateDate = createDate ?? DateTime.MinValue
            };
        }
    }
}