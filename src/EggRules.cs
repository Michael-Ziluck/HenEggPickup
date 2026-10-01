using System;

namespace HenEggPickup;

internal static class EggRules
{
    internal static bool IsChickenEgg(string? prefab) => string.Equals(prefab, "ChickenEgg", StringComparison.Ordinal);
    internal static bool WithinRadius(float squaredDistance, float radius) =>
        radius > 0f && squaredDistance >= 0f && squaredDistance <= radius * radius;
}
