// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Online.API;

namespace osu.Game.Online.GitHub
{
    public class GetGitHubChangelogRequest : APIRequest<List<GitHubRelease>>
    {
        protected override string Target => string.Empty;

        protected override string Uri => $@"https://api.github.com/repos/{GitHubChangelog.REPOSITORY}/releases?per_page=50";
    }
}
