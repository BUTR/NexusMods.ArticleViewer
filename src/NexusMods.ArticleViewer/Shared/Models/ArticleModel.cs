using System;

namespace NexusMods.ArticleViewer.Shared.Models
{
    public record ArticleModel(string GameDomain, int Id, string Title, int AuthorId, string AuthorName, DateTimeOffset CreateDate)
    {
        public string Url => $"https://nexusmods.com/{GameDomain}/articles/{Id}";
        public string AuthorUrl => $"https://nexusmods.com/users/{AuthorId}";
    }
}