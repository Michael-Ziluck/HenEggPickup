using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using HenEggPickup;
using UnityEngine;

internal static class Program
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }
    private static void Main(string[] args)
    {
        var plugin = new Plugin();
        typeof(Plugin).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(plugin, null);
        var enabled = (ConfigEntry<bool>)plugin.Config.Entries["Enabled"];
        var minimum = (ConfigEntry<int>)plugin.Config.Entries["Minimum Hens"];
        var radius = (ConfigEntry<float>)plugin.Config.Entries["Hen Detection Radius"];
        Check(minimum.Value == 12 && radius.Value == 10, "default settings");
        var player = new Player(); Player.m_localPlayer = player;
        ItemDrop Egg(string prefab = "ChickenEgg", bool flag = false, float x = 0)
        {
            var item = new ItemDrop { m_autoPickup = flag }; item.gameObject.name = prefab + "(Clone)";
            item.transform.position = new Vector3(x, 1, 0); ItemDrop.All.Add(item); return item;
        }
        Character Hen(string prefab = "Hen", bool dead = false, float x = 0, float y = 0)
        {
            var hen = new Character { Dead = dead }; hen.gameObject.name = prefab + "(Clone)";
            hen.transform.position = new Vector3(x, y, 0); Character.Characters.Add(hen); return hen;
        }
        void Reset() { Character.Characters.Clear(); ItemDrop.All.Clear(); }
        void Flock(int n) { for (int i = 0; i < n; i++) Hen(); }
        Flock(11); var egg = Egg(); player.AutoPickup(.02f);
        Check(!egg.Collected && !egg.m_autoPickup, "11 hens do not enable eggs");
        Hen(); player.AutoPickup(.02f);
        Check(egg.Collected && !egg.m_autoPickup, "12 hens collect eggs and restore flag");
        Reset(); Flock(11); Hen("Chicken"); egg = Egg(); player.AutoPickup(.02f);
        Check(!egg.Collected, "chicks excluded");
        Hen(dead: true); player.AutoPickup(.02f); Check(!egg.Collected, "dead hens excluded");
        Hen(x: 10.01f); player.AutoPickup(.02f); Check(!egg.Collected, "hens outside radius excluded");
        Hen(x: 10); player.AutoPickup(.02f); Check(egg.Collected, "exact radius included");
        Reset(); minimum.Value = 1; Hen(y: 10.01f); egg = Egg(); player.AutoPickup(.02f);
        Check(!egg.Collected, "vertical distance counts");
        radius.Value = 11; player.AutoPickup(.02f); Check(egg.Collected, "configuration changes take effect");
        Reset(); minimum.Value = 12; radius.Value = 10; egg = Egg(flag: true); player.AutoPickup(.02f);
        Check(!egg.Collected && egg.m_autoPickup, "below threshold blocks pre-enabled eggs and restores true");
        enabled.Value = false; player.AutoPickup(.02f); Check(egg.Collected, "disabled preserves existing behavior"); enabled.Value = true;
        Reset(); Flock(12); egg = Egg(); player.TogglePickup(false); player.AutoPickup(.02f);
        Check(!egg.Collected && !egg.m_autoPickup, "player toggle respected"); player.TogglePickup(true);
        player.Teleporting = true; player.AutoPickup(.02f); Check(!egg.Collected, "teleporting respected"); player.Teleporting = false;
        player.FullInventory = true; player.AutoPickup(.02f); Check(!egg.Collected && !egg.m_autoPickup, "inventory guard respected"); player.FullInventory = false;
        var other = new Player(); other.AutoPickup(.02f); Check(!egg.Collected, "other player is unaffected");
        Reset(); Flock(12); egg = Egg(x: 2.01f); player.AutoPickup(.02f); Check(!egg.Collected, "normal pickup range respected");
        Reset(); Flock(12); egg = Egg(); var otherEgg = Egg("DragonEgg"); var asksvin = Egg("AsksvinEgg"); var cooked = Egg("EggCooked");
        var seed = Egg("CarrotSeeds", flag: true); player.AutoPickup(.02f);
        Check(egg.Collected && !otherEgg.Collected && !asksvin.Collected && !cooked.Collected && seed.Collected, "only raw chicken eggs are overridden");
        Reset(); Flock(12); egg = Egg(); player.ThrowDuringPickup = true;
        try { player.AutoPickup(.02f); throw new Exception("exception was swallowed"); }
        catch (InvalidOperationException e) { Check(e.Message == "simulated pickup failure" && !egg.m_autoPickup, "exception propagates and flag restores"); }
        player.ThrowDuringPickup = false;
        Reset(); Flock(12);
        var firstEgg = Egg(); var enabledEgg = Egg(flag: true); var lastEgg = Egg();
        player.AutoPickup(.02f);
        Check(firstEgg.Collected && enabledEgg.Collected && lastEgg.Collected, "all nearby eggs use the same flock threshold");
        Check(!firstEgg.m_autoPickup && enabledEgg.m_autoPickup && !lastEgg.m_autoPickup, "mixed original flags restore independently");
        Reset(); Flock(12); egg = Egg(); var untouchedEgg = Egg(flag: true); var changedEgg = Egg();
        player.ThrowDuringPickup = true;
        try { player.AutoPickup(.02f); throw new Exception("exception was swallowed"); }
        catch (InvalidOperationException)
        {
            Check(!egg.m_autoPickup && untouchedEgg.m_autoPickup && !changedEgg.m_autoPickup, "exception restores every changed egg, including later unvisited eggs");
        }
        player.ThrowDuringPickup = false;
        var filter = new Harmony("checks.feedguard");
        filter.Patch(AccessTools.Method(typeof(Player), nameof(Player.AutoPickup)), transpiler: new HarmonyMethod(typeof(Program), nameof(FeedFilter)), prefix: new HarmonyMethod(typeof(Program), nameof(SkipPickup)));
        egg.FilterBlocked = true; player.AutoPickup(.02f); Check(!egg.Collected && !egg.m_autoPickup, "existing pickup filter respected");
        egg.FilterBlocked = false; player.Skip = true; player.AutoPickup(.02f); Check(!egg.Collected && !egg.m_autoPickup, "skipped original restores flag"); player.Skip = false;
        player.AutoPickup(.02f); Check(egg.Collected && !egg.m_autoPickup, "works with feed-style transpiler installed");
        filter.UnpatchSelf();
        Check(!EggRules.WithinRadius(float.NaN, 10) && !EggRules.WithinRadius(-1, 10), "invalid distances rejected");
        Check(!EggRules.IsChickenEgg(null) && !EggRules.IsChickenEgg("chickenegg"), "only the exact chicken egg prefab matches");
        typeof(Plugin).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(plugin, null);
        Reset(); Flock(12); egg = Egg(); player.AutoPickup(.02f);
        Check(!egg.Collected && !egg.m_autoPickup, "plugin destruction removes its pickup patches");
        using (var game = Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.Combine(args[0], "valheim_Data", "Managed", "assembly_valheim.dll")))
        {
            var itemType = game.MainModule.Types.Single(t => t.Name == "ItemDrop");
            Check(itemType.Fields.Any(f => f.Name == "s_instances" && f.IsStatic && f.FieldType.FullName == "System.Collections.Generic.List`1<ItemDrop>"), "installed item registry shape");
            var playerType = game.MainModule.Types.Single(t => t.Name == "Player");
            Check(playerType.Fields.Any(f => f.Name == "m_enableAutoPickup" && f.FieldType.FullName == "System.Boolean"), "installed pickup toggle injectable");
            var method = playerType.Methods.Single(m => m.Name == "AutoPickup");
            Check(method.Parameters.Count == 1 && method.Parameters[0].ParameterType.FullName == "System.Single", "installed patch target signature");
            Check(method.Body.Instructions.Any(i => i.Operand is Mono.Cecil.FieldReference f && f.Name == "m_autoPickup"), "installed pickup checks item flag");
            Check(method.Body.Instructions.Any(i => i.Operand is Mono.Cecil.MethodReference m && m.Name == "CanAddItem"), "installed inventory guard remains in pipeline");
        }
        using (var release = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[1]))
        {
            Check(!release.MainModule.AssemblyReferences.Any(r => r.Name.Contains("ServerSync") || r.Name.Contains("AutoPicker") || r.Name.Contains("AnimalFeedGuard")), "no external mod dependencies");
            var code = release.MainModule.Types.SelectMany(t => t.Methods.Concat(t.NestedTypes.SelectMany(n => n.Methods))).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions);
            Check(!code.Any(i => i.Operand is Mono.Cecil.MethodReference m && (m.DeclaringType.Name == "ZDO" || m.DeclaringType.Name == "ZRoutedRpc")), "no network state writes or settings sync");
        }
        Console.WriteLine($"PASS: {checks} hook, behavior, compatibility and installed-code checks. Gameplay not exercised.");
    }
    private static bool SkipPickup(Player __instance) => !__instance.Skip;
    public static bool CanPick(ItemDrop item) => item.m_autoPickup && !item.FilterBlocked;
    private static IEnumerable<CodeInstruction> FeedFilter(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.LoadsField(AccessTools.Field(typeof(ItemDrop), "m_autoPickup")))
            { instruction.opcode = OpCodes.Call; instruction.operand = AccessTools.Method(typeof(Program), nameof(CanPick)); }
            yield return instruction;
        }
    }
}
