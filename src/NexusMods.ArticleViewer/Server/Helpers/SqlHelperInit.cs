using Microsoft.Extensions.Configuration;

using Npgsql;

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


        private readonly IConfiguration _configuration;

        public SqlHelperInit(IConfiguration configuration) => _configuration = configuration;

        public async Task CreateTablesIfNotExistAsync(CancellationToken ct)
        {
            await using var connection = new NpgsqlConnection(_configuration.GetConnectionString("Main"));
            await connection.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                CreateModsTable +
                CreateCacheTable, connection);
            try
            {
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.NextResultAsync(ct)) { }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}