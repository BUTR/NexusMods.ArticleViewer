﻿namespace NexusMods.ArticleViewer.Server.Options
{
    public record AuthenticationOptions
    {
        public string AdminToken { get; set; } = default!;
    }
}