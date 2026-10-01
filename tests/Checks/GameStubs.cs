using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
    public class Object { public bool Destroyed; public static implicit operator bool(Object? value) => value != null && !value.Destroyed; }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 up => new Vector3(0, 1, 0);
        public float sqrMagnitude => x * x + y * y + z * z;
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
    }
    public class Transform { public Vector3 position; }
    public class GameObject { public string name = ""; }
    public class MonoBehaviour : Object { public Transform transform = new Transform(); public GameObject gameObject = new GameObject(); }
}
namespace BepInEx.Configuration
{
    public class ConfigEntry<T> { public T Value; public ConfigEntry(T value) => Value = value; }
    public class AcceptableValueRange<T> { public AcceptableValueRange(T min, T max) { } }
    public class ConfigDescription { public ConfigDescription(string description, object range) { } }
    public class ConfigFile
    {
        public readonly Dictionary<string, object> Entries = new Dictionary<string, object>();
        public ConfigEntry<T> Bind<T>(string section, string key, T value, object description)
        {
            var entry = new ConfigEntry<T>(value); Entries[key] = entry; return entry;
        }
    }
}
namespace BepInEx
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BepInPlugin : Attribute { public BepInPlugin(string guid, string name, string version) { } }
    public class TestLogger { public void LogInfo(object text) { } public void LogError(object text) => throw new Exception(text.ToString()); }
    public class BaseUnityPlugin : UnityEngine.MonoBehaviour
    {
        public BepInEx.Configuration.ConfigFile Config = new BepInEx.Configuration.ConfigFile();
        public TestLogger Logger = new TestLogger();
    }
}
public class Character : UnityEngine.MonoBehaviour
{
    public static readonly List<Character> Characters = new List<Character>();
    public bool Dead;
    public bool IsDead() => Dead;
    public static List<Character> GetAllCharacters() => Characters;
}
public static class Utils
{
    public static string GetPrefabName(UnityEngine.GameObject gameObject) => gameObject.name.Replace("(Clone)", "").Trim();
}
public class ItemDrop : UnityEngine.MonoBehaviour
{
    private static readonly List<ItemDrop> s_instances = new List<ItemDrop>();
    public static List<ItemDrop> All => s_instances;
    public bool m_autoPickup;
    public bool FilterBlocked;
    public bool Collected;
}
public class Player : Character
{
    public static Player m_localPlayer = null!;
    private bool m_enableAutoPickup = true;
    public float m_autoPickupRange = 2f;
    public bool Teleporting, FullInventory, ThrowDuringPickup, Skip;
    public bool IsTeleporting() => Teleporting;
    public void TogglePickup(bool value) => m_enableAutoPickup = value;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AutoPickup(float dt)
    {
        if (Teleporting || !m_enableAutoPickup) return;
        foreach (var item in ItemDrop.All)
        {
            if (!item.m_autoPickup) continue;
            if (ThrowDuringPickup) throw new InvalidOperationException("simulated pickup failure");
            if (!FullInventory && HenEggPickup.EggRules.WithinRadius((item.transform.position - (transform.position + UnityEngine.Vector3.up)).sqrMagnitude, m_autoPickupRange))
                item.Collected = true;
        }
    }
}
