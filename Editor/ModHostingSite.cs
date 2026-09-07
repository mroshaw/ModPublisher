using System;

namespace DaftAppleGames.Editor.ModPublisher
{
    /// <summary>
    /// Hosting sites that can receive a mod publish.
    /// </summary>
    [Flags]
    public enum ModHostingSite
    {
        None = 0,
        Nexus = 1 << 0,
        GitHub = 1 << 1
    }
}
