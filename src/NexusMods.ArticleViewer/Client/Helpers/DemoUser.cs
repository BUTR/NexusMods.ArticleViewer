using BUTR.NexusMods.Shared.Models;

using NexusMods.ArticleViewer.Shared.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NexusMods.ArticleViewer.Client.Helpers
{
    internal readonly struct DemoUserState
    {
        public static async Task<DemoUserState> CreateAsync()
        {
            return new(
                new(31179975, "Pickysaurus", "demo@demo.com", "https://forums.nexusmods.com/uploads/profile/photo-31179975.png", true, true),
                new()
                {
                    new("demo", 1, "Demo Article 1", 31179975, "Pickysaurus", DateTimeOffset.UtcNow),
                    new("demo", 1, "Demo Article 2", 31179975, "Pickysaurus", DateTimeOffset.UtcNow),
                    new("demo", 1, "Demo Article 3", 31179975, "Pickysaurus", DateTimeOffset.UtcNow),
                    new("demo", 1, "Demo Article 4", 31179975, "Pickysaurus", DateTimeOffset.UtcNow),
                }
            );
        }


        public readonly ProfileModel Profile;
        public readonly List<ArticleModel> Articles;

        private DemoUserState(ProfileModel profile, List<ArticleModel> articles)
        {
            Profile = profile;
            Articles = articles;
        }
    }

    public class DemoUser
    {
        public static async Task<DemoUser> CreateAsync() => new(await DemoUserState.CreateAsync());


        public ProfileModel Profile => _state.Profile;
        public List<ArticleModel> Articles => _state.Articles;

        private readonly DemoUserState _state;

        private DemoUser(DemoUserState state) => _state = state;
    }
}