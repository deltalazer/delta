// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Online
{
    public class DevelopmentEndpointConfiguration : EndpointConfiguration
    {
        public DevelopmentEndpointConfiguration()
        {
            WebsiteUrl = APIUrl = @"https://delta.mikuuu.xyz";
            APIClientSecret = @"MzalrFaGupVLEM5ljY5iKJksHvGVWg27laLjxy3C";
            APIClientID = "1";
            SpectatorUrl = $@"{APIUrl}/signalr/spectator";
            MultiplayerUrl = $@"{APIUrl}/signalr/multiplayer";
            MetadataUrl = $@"{APIUrl}/signalr/metadata";
            BeatmapSubmissionServiceUrl = @"https://submit.mikuuu.xyz";
        }
    }
}
