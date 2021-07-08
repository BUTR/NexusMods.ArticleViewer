using System.Collections.Generic;
using System.Linq;

namespace NexusMods.ArticleViewer.Shared.Models.API
{
    public record PagingResponse<T> where T : class
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public PagingMetadata Metadata { get; set; } = new();
    }
}