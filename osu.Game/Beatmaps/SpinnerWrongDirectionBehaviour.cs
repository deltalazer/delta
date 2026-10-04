// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Beatmaps
{
    public enum SpinnerWrongDirectionBehaviour
    {
        Inherit = -1,
        NoProgress = 0,
        SubtractProgress,
        InstantMiss,
    }
}
