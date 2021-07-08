using System;

namespace NexusMods.ArticleViewer.Server.Models.Database
{
    public record ArticleTableEntry
    {
        public string GameDomain { get; set; } = default!;
        public int Id { get; set; } = default!;

        public string Title { get; set; } = default!;

        public int AuthorId { get; set; } = default!;
        public string AuthorName { get; set; } = default!;

        public DateTimeOffset CreateDate { get; set; } = default!;
    }
}