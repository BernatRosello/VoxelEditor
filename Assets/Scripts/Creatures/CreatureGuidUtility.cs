using System;

public static class CreatureGuidUtility
{
    /// <summary>
    /// Generates a globally unique creature identifier.
    /// Safe for save files, trading, networking and
    /// future game versions.
    /// </summary>
    public static string CreateGuid()
    {
        return Guid.NewGuid().ToString("N");
    }
}