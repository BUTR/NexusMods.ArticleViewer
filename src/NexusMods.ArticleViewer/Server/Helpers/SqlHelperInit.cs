using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Npgsql;

using Polly;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace NexusMods.ArticleViewer.Server.Helpers
{
    public class SqlHelperInit
    {
        public static string CreateModsTable = @"
CREATE TABLE IF NOT EXISTS ""public"".""article_entity"" (
    ""game_domain"" text NOT NULL,
    ""id"" int4 NOT NULL,
    ""title"" text NOT NULL,
    ""author_id"" int4 NOT NULL,
    ""author_name"" text NOT NULL,
    ""create_date"" timestamptz NOT NULL,
    PRIMARY KEY (""game_domain"",""id"")
)
;";

        public static string CreateCacheTable = @"
CREATE TABLE IF NOT EXISTS ""public"".""nexusmods_cache_entry"" (
    ""Id"" text NOT NULL,
    ""AbsoluteExpiration"" timestamptz,
    ""ExpiresAtTime"" timestamptz NOT NULL,
    ""SlidingExpirationInSeconds"" int8,
    ""Value"" bytea,
    PRIMARY KEY (""Id"")
)
;";


        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public SqlHelperInit(ILogger<SqlHelperInit> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task CreateTablesIfNotExistAsync(CancellationToken ct)
        {
            var policy = Policy.Handle<Exception>(ex => ex.GetType() != typeof(TaskCanceledException) || ex.GetType() != typeof(OperationCanceledException))
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(5000), (ex, time) =>
                {
                    _logger.LogError(ex, "Exception during sql init. Retrying after {RetrySeconds} seconds", time.TotalSeconds);
                });

            await policy.ExecuteAsync(async token =>
            {
                await using var connection = new NpgsqlConnection(_configuration.GetConnectionString("Main"));
                await connection.OpenAsync(token);
                await using var cmd = new NpgsqlCommand(
                    CreateModsTable +
                    CreateCacheTable, connection);

                await using var reader = await cmd.ExecuteReaderAsync(token);
                while (await reader.NextResultAsync(token)) { }
            }, ct);
        }
    }
}