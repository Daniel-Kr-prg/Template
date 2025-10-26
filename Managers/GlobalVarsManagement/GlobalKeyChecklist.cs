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
        [ReadOnly, HorizontalGroup("row", 0.65f)] public GlobalKey Key;
        [ToggleLeft, HorizontalGroup("row", 0.35f), LabelText("Сохранять")] public bool Save;
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
}