// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.GitHub
{
    public static class GitHubChangelog
    {
        public const string REPOSITORY = @"deltalazer/delta";

        private const long stream_id = 1;

        public static APIChangelogIndex ToIndex(IEnumerable<GitHubRelease> releases)
        {
            var stream = new APIUpdateStream
            {
                Id = stream_id,
                Name = @"delta",
                DisplayName = @"deltalazer",
                IsFeatured = true,
            };

            List<APIChangelogBuild> builds = releases
                                             .Where(r => !r.Draft)
                                             .OrderByDescending(r => r.PublishedAt ?? r.CreatedAt)
                                             .Select(r => toBuild(r, stream))
                                             .ToList();

            stream.LatestBuild = builds.FirstOrDefault();

            return new APIChangelogIndex
            {
                Builds = builds,
                Streams = new List<APIUpdateStream> { stream },
            };
        }

        private static APIChangelogBuild toBuild(GitHubRelease release, APIUpdateStream stream)
        {
            string version = string.IsNullOrEmpty(release.TagName) ? release.Name ?? string.Empty : release.TagName;

            return new APIChangelogBuild
            {
                Id = release.Id,
                Version = version,
                DisplayVersion = version,
                CreatedAt = release.PublishedAt ?? release.CreatedAt,
                UpdateStream = stream,
                ChangelogEntries = new List<APIChangelogEntry>
                {
                    new APIChangelogEntry
                    {
                        Category = release.Prerelease ? @"Pre-release" : @"Changes",
                        Title = string.IsNullOrEmpty(release.Name) ? release.TagName : release.Name,
                        Message = (release.Body ?? string.Empty).Trim(),
                        Url = release.HtmlUrl,
                        GithubUrl = release.HtmlUrl,
                    }
                },
            };
        }
    }
}
