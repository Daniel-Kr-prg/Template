using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class GlobalKeyChecklist
{
    [Serializable]
    public class Item
    {
        [ReadOnly, HorizontalGroup("row", 0.35f)] public GlobalKey Key;
        [ToggleLeft, HorizontalGroup("row", 0.2f), LabelText("Сохранять")] public bool Save;

        [ShowInInspector, ReadOnly, HorizontalGroup("row", 0.45f), LabelText("Значение")]
        public string CurrentValue => GlobalVarsManager.TryGetRaw(Key, out var value)
            ? GlobalVarsManager.FormatValueForInspector(value)
            : string.Empty;
    }

    [TableList(IsReadOnly = false, AlwaysExpanded = true, ShowIndexLabels = false)]
    [SerializeField] private List<Item> _items = new();

    /// <summary>
    /// Обновить список по текущему enum GlobalKey, сохранив отмеченные галочки.
    /// Новые значения добавятся (Save=false), удалённые исчезнут.
    /// </summary>
    [Button("Обновить список ключей", ButtonSizes.Medium)]
    public void RefreshFromEnum()
    {
        // 1) запомним текущее состояние
        var previous = new Dictionary<GlobalKey, bool>();
        foreach (var it in _items)
        {
            if (!previous.ContainsKey(it.Key))
                previous.Add(it.Key, it.Save);
        }

        // 2) соберём новые элементы строго по enum
        _items.Clear();
        foreach (GlobalKey key in Enum.GetValues(typeof(GlobalKey)))
        {
            bool wasSaved = previous.TryGetValue(key, out var saved) && saved;
            _items.Add(new Item { Key = key, Save = wasSaved });
        }
    }

    /// <summary>Хелпер: вернуть отмеченные ключи как множество.</summary>
    public HashSet<GlobalKey> ToSet()
    {
        var hs = new HashSet<GlobalKey>();
        foreach (var it in _items) if (it.Save) hs.Add(it.Key);
        return hs;
    }

    public void SetSaveable(GlobalKey key, bool saveable)
    {
        EnsureKey(key).Save = saveable;
    }

    public void EnsureKeyExists(GlobalKey key)
    {
        EnsureKey(key);
    }

    /// <summary>Опционально: выставить отметки из множества (если нужно программно).</summary>
    public void FromSet(HashSet<GlobalKey> set)
    {
        if (set == null) return;
        RefreshFromEnum(); // чтобы точно были все ключи
        foreach (var it in _items) it.Save = set.Contains(it.Key);
    }

    // Удобно автообновлять при открытии инспектора/перекомпиляции:
    [OnInspectorInit]
    private void OnInspectorInit() => RefreshFromEnum();

#if UNITY_EDITOR
    // и при изменениях в редакторе (без потери галочек)
    private void OnValidate() => RefreshFromEnum();
#endif

    private Item EnsureKey(GlobalKey key)
    {
        foreach (var it in _items)
        {
            if (it.Key == key)
                return it;
        }

        var item = new Item { Key = key };
        _items.Add(item);
        return item;
    }
}

[Serializable]
public class GlobalStringKeyChecklist
{
    [Serializable]
    public class Item
    {
        [HorizontalGroup("row", 0.45f), LabelText("Ключ")]
        public string Key;

        [ToggleLeft, HorizontalGroup("row", 0.2f), LabelText("Сохранять")]
        public bool Save;

        [ShowInInspector, ReadOnly, HorizontalGroup("row", 0.35f), LabelText("Значение")]
        public string CurrentValue => GlobalVarsManager.TryGetRaw(Key, out var value)
            ? GlobalVarsManager.FormatValueForInspector(value)
            : string.Empty;
    }

    [TableList(IsReadOnly = false, AlwaysExpanded = true, ShowIndexLabels = false)]
    [SerializeField] private List<Item> _items = new();

    [Button("Обновить список ключей", ButtonSizes.Medium)]
    public void Refresh()
    {
        var cleaned = new List<Item>();
        foreach (var it in _items)
        {
            if (it == null || string.IsNullOrWhiteSpace(it.Key) || ContainsKey(cleaned, it.Key))
                continue;

            cleaned.Add(it);
        }

        _items = cleaned;
    }

    public void AddOrUpdateKey(string key, object value, bool saveable)
    {
        var item = EnsureKey(key);
        if (item == null)
            return;

        if (saveable)
            item.Save = true;
    }

    public void SetSaveable(string key, bool saveable)
    {
        var item = EnsureKey(key);
        if (item != null)
            item.Save = saveable;
    }

    public HashSet<string> ToSet()
    {
        var hs = new HashSet<string>();
        foreach (var it in _items)
        {
            if (it != null && it.Save && !string.IsNullOrWhiteSpace(it.Key))
                hs.Add(it.Key);
        }

        return hs;
    }

    public void AddSavedKeys(IEnumerable<string> keys)
    {
        if (keys == null)
            return;

        foreach (var key in keys)
            SetSaveable(key, true);
    }

    [OnInspectorInit]
    private void OnInspectorInit() => Refresh();

#if UNITY_EDITOR
    private void OnValidate() => Refresh();
#endif

    private Item EnsureKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        foreach (var it in _items)
        {
            if (it != null && it.Key == key)
                return it;
        }

        var item = new Item { Key = key };
        _items.Add(item);
        return item;
    }

    private static bool ContainsKey(List<Item> items, string key)
    {
        foreach (var it in items)
        {
            if (it != null && it.Key == key)
                return true;
        }

        return false;
    }
}
