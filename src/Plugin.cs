using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace HenEggPickup;

[BepInPlugin(Guid, "Hen Egg Pickup", "2.0.1")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Guid = "com.ziluck.valheim.heneggpickup";
    private static ConfigEntry<bool> pickupEnabled = null!;
    private static ConfigEntry<int> minimumHens = null!;
    private static ConfigEntry<float> henRadius = null!;
    private static readonly FieldInfo Instances = AccessTools.Field(typeof(ItemDrop), "s_instances")
        ?? throw new MissingFieldException(typeof(ItemDrop).FullName, "s_instances");
    private Harmony? harmony;

    private void Awake()
    {
        pickupEnabled = Config.Bind("General", "Enabled", true, "Automatically collect chicken eggs only when enough adult hens are near you. Manual pickup is unaffected. Settings apply only to this client.");
        minimumHens = Config.Bind("General", "Minimum Hens", 12, new ConfigDescription("Minimum number of living adult hens within Hen Detection Radius of the player. Chicks do not count.", new AcceptableValueRange<int>(1, 1000)));
        henRadius = Config.Bind("General", "Hen Detection Radius", 10f, new ConfigDescription("Distance in metres from the player to adult hens, including height. Walls do not block detection. Only loaded hens count. This does not change the game's item pickup range.", new AcceptableValueRange<float>(1f, 100f)));
        try
        {
            harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo($"Hen Egg Pickup ready: {minimumHens.Value} adult hens within {henRadius.Value}m. Local settings only.");
        }
        catch (Exception error)
        {
            harmony?.UnpatchSelf();
            Logger.LogError($"Hen Egg Pickup could not patch automatic pickup: {error}");
        }
    }

    private void OnDestroy() => harmony?.UnpatchSelf();

    private static bool HasEnoughHens(Player player)
    {
        int henCount = 0;
        Vector3 playerPosition = player.transform.position;
        foreach (Character character in Character.GetAllCharacters())
        {
            if (!character || character.IsDead() || Utils.GetPrefabName(character.gameObject) != "Hen") continue;
            if (!EggRules.WithinRadius((character.transform.position - playerPosition).sqrMagnitude, henRadius.Value)) continue;
            if (++henCount >= minimumHens.Value) return true;
        }
        return false;
    }

    internal readonly struct PickupFlag
    {
        internal readonly ItemDrop Egg;
        internal readonly bool OriginalAutoPickup;

        internal PickupFlag(ItemDrop egg)
        {
            Egg = egg;
            OriginalAutoPickup = egg.m_autoPickup;
        }
    }

    // Scope the flag override to the local automatic pickup call. This leaves
    // the existing pickup pipeline (including Animal Feed Guard's filter) intact.
    // The finalizer also restores flags if another mod throws or skips the call.
    [HarmonyPatch(typeof(Player), "AutoPickup")]
    private static class PickupPatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Player __instance, bool ___m_enableAutoPickup, ref List<PickupFlag>? __state)
        {
            __state = null;
            if (!pickupEnabled.Value || __instance != Player.m_localPlayer || !___m_enableAutoPickup || __instance.IsTeleporting()) return;
            var drops = Instances.GetValue(null) as List<ItemDrop>;
            if (drops == null) return;
            bool? canCollect = null;
            Vector3 pickupOrigin = __instance.transform.position + Vector3.up;
            foreach (ItemDrop egg in drops)
            {
                if (!egg || !EggRules.IsChickenEgg(Utils.GetPrefabName(egg.gameObject))) continue;
                if (!EggRules.WithinRadius((egg.transform.position - pickupOrigin).sqrMagnitude, __instance.m_autoPickupRange)) continue;
                canCollect ??= HasEnoughHens(__instance);
                if (egg.m_autoPickup == canCollect.Value) continue;
                __state ??= new List<PickupFlag>();
                __state.Add(new PickupFlag(egg));
                egg.m_autoPickup = canCollect.Value;
            }
        }

        [HarmonyPriority(Priority.Last)]
        private static Exception? Finalizer(Exception? __exception, List<PickupFlag>? __state)
        {
            if (__state != null)
                foreach (PickupFlag saved in __state)
                    if (saved.Egg) saved.Egg.m_autoPickup = saved.OriginalAutoPickup;
            return __exception;
        }
    }
}
