using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace HenEggPickup;

[BepInPlugin(Guid, "Hen Egg Pickup", "2.0.0")]
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
        int count = 0;
        Vector3 origin = player.transform.position;
        foreach (Character character in Character.GetAllCharacters())
        {
            if (!character || character.IsDead() || Utils.GetPrefabName(character.gameObject) != "Hen") continue;
            if (!EggRules.WithinRadius((character.transform.position - origin).sqrMagnitude, henRadius.Value)) continue;
            if (++count >= minimumHens.Value) return true;
        }
        return false;
    }

    internal readonly struct PickupFlag
    {
        internal readonly ItemDrop Item;
        internal readonly bool Original;
        internal PickupFlag(ItemDrop item) { Item = item; Original = item.m_autoPickup; }
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
            Vector3 origin = __instance.transform.position + Vector3.up;
            foreach (ItemDrop item in drops)
            {
                if (!item || !EggRules.IsChickenEgg(Utils.GetPrefabName(item.gameObject))) continue;
                if (!EggRules.WithinRadius((item.transform.position - origin).sqrMagnitude, __instance.m_autoPickupRange)) continue;
                canCollect ??= HasEnoughHens(__instance);
                if (item.m_autoPickup == canCollect.Value) continue;
                __state ??= new List<PickupFlag>();
                __state.Add(new PickupFlag(item));
                item.m_autoPickup = canCollect.Value;
            }
        }

        [HarmonyPriority(Priority.Last)]
        private static Exception? Finalizer(Exception? __exception, List<PickupFlag>? __state)
        {
            if (__state != null)
                foreach (PickupFlag saved in __state)
                    if (saved.Item) saved.Item.m_autoPickup = saved.Original;
            return __exception;
        }
    }
}
