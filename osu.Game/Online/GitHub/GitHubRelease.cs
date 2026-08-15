// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json;

namespace osu.Game.Online.GitHub
{
    public class GitHubRelease
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("body")]
        public string? Body { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonProperty("draft")]
        public bool Draft { get; set; }

        [JsonProperty("prerelease")]
        public bool Prerelease { get; set; }

        [JsonProperty("published_at")]
        public DateTimeOffset? PublishedAt { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
