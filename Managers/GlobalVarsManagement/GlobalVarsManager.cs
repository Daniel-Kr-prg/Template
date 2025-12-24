using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using DanieloZ.Managers;


/// <summary>
/// Перечень ключей для enum-хранилища. Расширяй под проект.
/// </summary>
public enum GlobalKey
{
    // App / Environment
    AppReady,
    NetworkOnline,
    LocaleCode,      // string
    BuildVersion,    // string

    // User / Session
    UserAuthenticated,
    UserCountry,     // string
    DisableAds,      // bool

    // Gameplay
    CurrentLevelId,  // int
    CurrentChapter,  // int
    TutorialDone,    // bool

    // Monetization
    RewardedReady,
    InterstitialReady,

    // A/B
    ABBucket         // string
}


/// <summary>
/// Единый менеджер глобальных переменных (любой тип значений).
/// - Ключи-энумы (GlobalKey) и произвольные string-ключи.
/// - Типобезопасные Set/Get<T>.
/// - Подписки на изменения (Observe).
/// - Интеграция с SaveManager: галочками выбираешь, что писать в сейв.
/// </summary>
public sealed class GlobalVarsManager : SingletonManager<GlobalVarsManager>
{
    #region Inspector

    [TitleGroup("Persistence")]
    [LabelText("Enum-ключи в сейв"), InlineProperty, Sirenix.OdinInspector.HideLabel]
    [SerializeField] private GlobalKeyChecklist persistedEnumKeys = new GlobalKeyChecklist();

    [TitleGroup("Persistence")]
    [LabelText("String-ключи в сейв")]
    [ListDrawerSettings(ShowPaging = false, DraggableItems = false)]
    [SerializeField] private List<string> persistedStringKeys = new();

    [FoldoutGroup("Debug"), ReadOnly, ShowInInspector] private int EnumCount => _enumValues.Count;
    [FoldoutGroup("Debug"), ReadOnly, ShowInInspector] private int StringCount => _stringValues.Count;

    

    #endregion

    #region Storage

    private readonly Dictionary<GlobalKey, object> _enumValues = new();
    private readonly Dictionary<string, object> _stringValues = new();

    private readonly Dictionary<GlobalKey, Action<object>> _enumChanged = new();
    private readonly Dictionary<string, Action<object>> _stringChanged = new();

    private GlobalStateSaveItem _saveItem; // регистрируетcя в SaveManager

    #endregion

    #region Lifecycle

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_GlobalVarsManagerReady");
        _saveItem = new GlobalStateSaveItem("GlobalState", this);
    }

    #endregion

    #region Public API (Enum keys)

    public static void Set<T>(GlobalKey key, T value, bool notify = true)
    {
        var m = Instance; if (m == null) return;
        m._enumValues[key] = value;
        if (notify && m._enumChanged.TryGetValue(key, out var del)) del?.Invoke(value);
    }

    public static bool TryGet<T>(GlobalKey key, out T value)
    {
        value = default;
        var m = Instance; if (m == null) return false;
        if (m._enumValues.TryGetValue(key, out var raw) && raw is T cast) { value = cast; return true; }
        return false;
    }

    public static T GetOr<T>(GlobalKey key, T fallback = default) => TryGet<T>(key, out var v) ? v : fallback;

    public static void Remove(GlobalKey key, bool notify = true)
    {
        var m = Instance; if (m == null) return;
        m._enumValues.Remove(key);
        if (notify && m._enumChanged.TryGetValue(key, out var del)) del?.Invoke(null);
    }

    /// <summary>Подписка на изменения значения по enum-ключу.</summary>
    public static IDisposable Observe(GlobalKey key, Action<object> onChanged, bool invokeImmediately = false)
    {
        var m = Instance; if (m == null) return DummyDisposable.Instance;
        if (!m._enumChanged.ContainsKey(key)) m._enumChanged[key] = null;
        m._enumChanged[key] += onChanged;
        if (invokeImmediately && m._enumValues.TryGetValue(key, out var cur)) onChanged?.Invoke(cur);

        return new Subscription(() =>
        {
            if (Instance == null) return;
            if (Instance._enumChanged.TryGetValue(key, out var del))
                Instance._enumChanged[key] = (Action<object>)Delegate.Remove(del, onChanged);
        });
    }

    #endregion

    #region Public API (String keys)

    public static void Set<T>(string key, T value, bool notify = true)
    {
        if (string.IsNullOrEmpty(key)) return;
        var m = Instance; if (m == null) return;

        m._stringValues[key] = value;
        if (notify && m._stringChanged.TryGetValue(key, out var del)) del?.Invoke(value);
    }

    public static bool TryGet<T>(string key, out T value)
    {
        value = default;
        var m = Instance; if (m == null || string.IsNullOrEmpty(key)) return false;

        if (m._stringValues.TryGetValue(key, out var raw) && raw is T cast) { value = cast; return true; }
        return false;
    }

    public static T GetOr<T>(string key, T fallback = default) => TryGet<T>(key, out var v) ? v : fallback;

    public static void Remove(string key, bool notify = true)
    {
        var m = Instance; if (m == null || string.IsNullOrEmpty(key)) return;
        m._stringValues.Remove(key);
        if (notify && m._stringChanged.TryGetValue(key, out var del)) del?.Invoke(null);
    }

    public static IDisposable Observe(string key, Action<object> onChanged, bool invokeImmediately = false)
    {
        var m = Instance; if (m == null || string.IsNullOrEmpty(key)) return DummyDisposable.Instance;
        if (!m._stringChanged.ContainsKey(key)) m._stringChanged[key] = null;
        m._stringChanged[key] += onChanged;
        if (invokeImmediately && m._stringValues.TryGetValue(key, out var cur)) onChanged?.Invoke(cur);

        return new Subscription(() =>
        {
            if (Instance == null) return;
            if (Instance._stringChanged.TryGetValue(key, out var del))
                Instance._stringChanged[key] = (Action<object>)Delegate.Remove(del, onChanged);
        });
    }

    #endregion

    #region Debug helpers

    [FoldoutGroup("Debug"), Button(ButtonSizes.Medium)]
    private void DumpToConsole()
    {
        Debug.Log($"[GlobalVarsManager] ENUM ({_enumValues.Count})");
        foreach (var kv in _enumValues) Debug.Log($"  {kv.Key} = {kv.Value}");
        Debug.Log($"[GlobalVarsManager] STR ({_stringValues.Count})");
        foreach (var kv in _stringValues) Debug.Log($"  {kv.Key} = {kv.Value}");
    }

    #endregion

    #region Persistence (SaveManager integration)

    internal PersistSpec BuildPersistSpec()
    {
        var e = persistedEnumKeys.ToSet(); // вот тут берём отметки галочек
        var s = new HashSet<string>(persistedStringKeys ?? new List<string>());
        return new PersistSpec(e, s);
    }

    internal GlobalVarsSnapshot MakeSnapshot(PersistSpec spec)
    {
        var snap = new GlobalVarsSnapshot
        {
            enumValues = new Dictionary<string, object>(),
            stringValues = new Dictionary<string, object>()
        };

        foreach (var kv in _enumValues)
            if (spec.EnumKeys.Contains(kv.Key))
                snap.enumValues[kv.Key.ToString()] = kv.Value;

        foreach (var kv in _stringValues)
            if (spec.StringKeys.Contains(kv.Key))
                snap.stringValues[kv.Key] = kv.Value;

        return snap;
    }

    internal void ApplySnapshot(GlobalVarsSnapshot snapshot, bool notify = true)
    {
        if (snapshot == null) return;

        foreach (var kv in snapshot.enumValues)
            if (Enum.TryParse(kv.Key, out GlobalKey key))
                Set<object>(key, kv.Value, notify);

        foreach (var kv in snapshot.stringValues)
            Set<object>(kv.Key, kv.Value, notify);
    }

    #endregion

    // ===== subscription handle =====
    private sealed class Subscription : IDisposable
    {
        private Action _dispose;
        public Subscription(Action dispose) { _dispose = dispose; }
        public void Dispose() { _dispose?.Invoke(); _dispose = null; }
    }
    private sealed class DummyDisposable : IDisposable { public static readonly DummyDisposable Instance = new(); public void Dispose() { } }
}

/// <summary>
/// Снапшот глобальных переменных для сейва. Сериализуется Newtonsoft.Json с TypeNameHandling.Auto,
/// чтобы сохранить любые типы (bool/int/float/string/DTO).
/// </summary>
[Serializable]
public sealed class GlobalVarsSnapshot
{
    public Dictionary<string, object> enumValues;
    public Dictionary<string, object> stringValues;
}

/// <summary>
/// Выбор ключей, которые попадают в сейв.
/// </summary>
public readonly struct PersistSpec
{
    public readonly HashSet<GlobalKey> EnumKeys;
    public readonly HashSet<string> StringKeys;
    public PersistSpec(HashSet<GlobalKey> e, HashSet<string> s) { EnumKeys = e; StringKeys = s; }
}

/// <summary>
/// SaveItem для SaveManager: сохраняет только отмеченные ключи GlobalVarsManager.
/// </summary>
public sealed class GlobalStateSaveItem : SaveItem
{
    private readonly GlobalVarsManager globalVarsManager;

    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto,
        Formatting = Formatting.None
    };

    public GlobalStateSaveItem(string id, GlobalVarsManager globalVars) : base(id, globalVars)
    {
        globalVarsManager = globalVars;
    }

    protected override void LoadCallback()
    {
        var snapshot = SaveManager.Load<GlobalVarsSnapshot>(id);
        if (snapshot != null)
            globalVarsManager.ApplySnapshot(snapshot, notify: true);
    }

    public override string CreateSaveData(object sourceObject)
    {
        var spec = globalVarsManager.BuildPersistSpec();
        var snapshot = globalVarsManager.MakeSnapshot(spec);
        return JsonConvert.SerializeObject(snapshot, JsonSettings);
    }
}