using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Sirenix.OdinInspector;
using ColorMix.Gameplay.Model;

public class Statistics_PlayerProfile : MonoBehaviour
{
    #region Constants

    private const string CURRENCY_KEY = "player_currency";
    private const string LEVEL_KEY = "player_level";
    private const string EXPERIENCE_KEY = "player_experience";
    private const string HP_KEY = "player_hp";
    private const string MAX_HP_KEY = "player_max_hp";

    #endregion

    #region Currency

    [BoxGroup("Currency")]
    [ShowInInspector, ReadOnly]
    public int currency = 0;

    #endregion

    #region Items

    [BoxGroup("Items")]
    [ShowInInspector, ReadOnly]
    public Dictionary<string, int> items = new Dictionary<string, int>();

    #endregion

    #region HP

    [BoxGroup("HP")]
    [ShowInInspector, ReadOnly]
    public int hp = 5;

    [BoxGroup("HP")]
    [ShowInInspector, ReadOnly]
    public int maxHp = 5;

    #endregion

    #region Profile Data

    [BoxGroup("Profile")]
    [ShowInInspector, ReadOnly]
    public string playerName = "";

    [BoxGroup("Profile")]
    [ShowInInspector, ReadOnly]
    public int level = 1;

    [BoxGroup("Profile")]
    [ShowInInspector, ReadOnly]
    public int experience = 0;

    #endregion

    #region Save System

    private PlayerProfileSaveItem saveItem;

    #endregion

    #region SaveManager toggle

    /// <summary>
    /// Allows disabling SaveManager.Save() calls from this class (useful for tests / isolated runs).
    /// Default: true (enabled).
    /// </summary>
    public static bool SaveManagerAutoSaveEnabled { get; set; } = true;

    private static void TryAutoSave()
    {
        if (!SaveManagerAutoSaveEnabled)
            return;

        SaveManager.Save();
    }

    #endregion

    #region Singleton Instance

    public static Statistics_PlayerProfile Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Multiple Statistics_PlayerProfile instances detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        saveItem = new PlayerProfileSaveItem("PlayerProfile", this);
        SyncToGlobalVars();
    }

    #endregion

    #region GlobalVars Sync

    public void SyncToGlobalVars()
    {
        GlobalVarsManager.Set(CURRENCY_KEY, currency);
        GlobalVarsManager.Set(LEVEL_KEY, level);
        GlobalVarsManager.Set(EXPERIENCE_KEY, experience);
        GlobalVarsManager.Set(HP_KEY, hp);
        GlobalVarsManager.Set(MAX_HP_KEY, maxHp);

        foreach (var kvp in items)
        {
            GlobalVarsManager.Set(kvp.Key, kvp.Value);
        }
    }

    private void SyncCurrency() => GlobalVarsManager.Set(CURRENCY_KEY, currency);
    private void SyncLevel() => GlobalVarsManager.Set(LEVEL_KEY, level);
    private void SyncExperience() => GlobalVarsManager.Set(EXPERIENCE_KEY, experience);
    private void SyncHp() => GlobalVarsManager.Set(HP_KEY, hp);
    private void SyncMaxHp() => GlobalVarsManager.Set(MAX_HP_KEY, maxHp);

    private void SyncItem(string itemID, int count) => GlobalVarsManager.Set(itemID, count);
    private void RemoveItemFromGlobalVars(string itemID) => GlobalVarsManager.Remove(itemID);

    private void SyncExperience()
    {
        GlobalVarsManager.Set(EXPERIENCE_KEY, experience);
    }

    private void SyncItem(string itemID, int count)
    {
        GlobalVarsManager.Set(itemID, count);
    }

    private void RemoveItemFromGlobalVars(string itemID)
    {
        GlobalVarsManager.Remove(itemID);
    }

    #endregion

    #region Currency Management

    public void AddCurrency(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"Attempting to add negative currency: {amount}");
            return;
        }

        currency += amount;
        SyncCurrency();
        TryAutoSave();
    }

    public bool SpendCurrency(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"Attempting to spend negative currency: {amount}");
            return false;
        }

        if (currency >= amount)
        {
            currency -= amount;
            SyncCurrency();
            TryAutoSave();
            return true;
        }

        return false;
    }

    public bool HasEnoughCurrency(int amount) => currency >= amount;

    public void SetCurrency(int amount)
    {
        currency = Mathf.Max(0, amount);
        SyncCurrency();
        TryAutoSave();
    }

    #endregion

    #region HP Management

    public void SetMaxHp(int value, bool clampCurrent = true)
    {
        maxHp = Mathf.Max(1, value);
        if (clampCurrent)
            hp = Mathf.Clamp(hp, 0, maxHp);

        SyncMaxHp();
        SyncHp();
        TryAutoSave();
    }

    public void SetHp(int value)
    {
        hp = Mathf.Clamp(value, 0, maxHp);
        SyncHp();
        TryAutoSave();
    }

    public void AddHp(int amount)
    {
        if (amount <= 0) return;
        hp = Mathf.Clamp(hp + amount, 0, maxHp);
        SyncHp();
        TryAutoSave();
    }

    public bool SpendHp(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"Invalid HP spend amount: {amount}");
            return false;
        }

        if (hp < amount)
            return false;

        hp -= amount;
        SyncHp();
        TryAutoSave();
        return true;
    }

    public bool HasHp(int amount = 1) => hp >= amount;

    #endregion

    #region Items Management

    public void AddItem(string itemID, int count = 1)
    {
        if (string.IsNullOrEmpty(itemID))
        {
            Debug.LogWarning("ItemID is null or empty");
            return;
        }

        if (count <= 0)
        {
            Debug.LogWarning($"Invalid item count: {count}");
            return;
        }

        if (items.ContainsKey(itemID))
        {
            items[itemID] += count;
        }
        else
        {
            items[itemID] = count;
        }

        SyncItem(itemID, items[itemID]);
        TryAutoSave();
    }

    public bool RemoveItem(string itemID, int count = 1)
    {
        if (string.IsNullOrEmpty(itemID))
            return false;

        if (count <= 0)
        {
            Debug.LogWarning($"Invalid item remove count: {count}");
            return false;
        }

        if (!items.ContainsKey(itemID))
            return false;

        if (items[itemID] < count)
            return false;

        items[itemID] -= count;

        if (items[itemID] <= 0)
        {
            items.Remove(itemID);
            RemoveItemFromGlobalVars(itemID);
        }
        else
        {
            SyncItem(itemID, items[itemID]);
        }

        TryAutoSave();
        return true;
    }

        return false;
    }

    public bool HasItem(string itemID, int count = 1)
    {
        if (string.IsNullOrEmpty(itemID)) return false;
        if (count <= 0) return true;
        return items.ContainsKey(itemID) && items[itemID] >= count;
    }

    public int GetItemCount(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return 0;
        return items.ContainsKey(itemID) ? items[itemID] : 0;
    }

    public void ClearItems()
    {
        foreach (var key in new List<string>(items.Keys))
        {
            RemoveItemFromGlobalVars(key);
        }

        items.Clear();
        TryAutoSave();
    }

    #endregion

    #region Profile Management

    public void SetPlayerName(string name)
    {
        playerName = name ?? "";
        TryAutoSave();
    }

    public void AddExperience(int exp)
    {
        if (exp <= 0) return;

        experience += exp;
        SyncExperience();

        while (experience >= GetExperienceForNextLevel())
        {
            LevelUp();
        }

        TryAutoSave();
    }

    private void LevelUp()
    {
        int required = GetExperienceForNextLevel();
        level++;
        experience -= required;

        SyncLevel();
        SyncExperience();
        Debug.Log($"Level up! New level: {level}");
    }

    private int GetExperienceForNextLevel()
    {
        return Mathf.Max(1, level) * 100;
    }

    public void ResetProfile()
    {
        currency = 0;
        ClearItems();

        playerName = "";
        level = 1;
        experience = 0;

        maxHp = 5;
        hp = maxHp;

        SyncToGlobalVars();
        TryAutoSave();
    }

    #endregion

    #region Bonus Helpers

    public static string GetBonusID(BonusType bonusType)
    {
        return $"bonus_{bonusType.ToString().ToLower()}";
    }

    public int GetBonusCount(BonusType bonusType)
    {
        return GetItemCount(GetBonusID(bonusType));
    }

    public void AddBonus(BonusType bonusType, int count = 1)
    {
        AddItem(GetBonusID(bonusType), count);
    }

    public bool SpendBonus(BonusType bonusType, int count = 1)
    {
        return RemoveItem(GetBonusID(bonusType), count);
    }

    public bool HasBonus(BonusType bonusType, int count = 1)
    {
        return HasItem(GetBonusID(bonusType), count);
    }

    #endregion

    #region Debug

    [Button("Add 100 Currency"), BoxGroup("Debug")]
    private void Debug_AddCurrency()
    {
        AddCurrency(100);
    }

    [Button("Add Test Item"), BoxGroup("Debug")]
    private void Debug_AddItem()
    {
        AddItem("test_item", 1);
    }

    [Button("Spend 1 HP"), BoxGroup("Debug")]
    private void Debug_SpendHp()
    {
        SpendHp(1);
    }

    [Button("Add 1 Rainbow Bonus"), BoxGroup("Debug")]
    private void Debug_AddRainbow()
    {
        AddBonus(BonusType.Rainbow, 1);
    }

    [Button("Reset Profile"), BoxGroup("Debug")]
    private void Debug_ResetProfile()
    {
        ResetProfile();
    }

    #endregion
}

public class PlayerProfileSaveItem : SaveItem
{
    #region Fields

    private readonly Statistics_PlayerProfile profile;

    #endregion

    #region Constructor

    public PlayerProfileSaveItem(string id, Statistics_PlayerProfile target)
        : base(id, target)
    {
        profile = target;
    }

    #endregion

    #region Save/Load

    public override string CreateSaveData(object sourceObject)
    {
        return JsonConvert.SerializeObject(new ProfileDTO
        {
            currency = profile.currency,
            items = profile.items,
            playerName = profile.playerName,
            level = profile.level,
            experience = profile.experience,
            hp = profile.hp,
            maxHp = profile.maxHp
        });
    }

    protected override void LoadCallback()
    {
        var loaded = SaveManager.Load<ProfileDTO>(id);
        if (loaded != null)
        {
            profile.currency = loaded.currency;
            profile.items = loaded.items ?? new Dictionary<string, int>();
            profile.playerName = loaded.playerName ?? "";
            profile.level = Mathf.Max(1, loaded.level);
            profile.experience = Mathf.Max(0, loaded.experience);

            profile.maxHp = Mathf.Max(1, loaded.maxHp);
            profile.hp = Mathf.Clamp(loaded.hp, 0, profile.maxHp);
        }
        else
        {
            profile.currency = 0;
            profile.items = new Dictionary<string, int>();
            profile.playerName = "";
            profile.level = 1;
            profile.experience = 0;

            profile.maxHp = 5;
            profile.hp = profile.maxHp;
        }

        profile.SyncToGlobalVars();
    }

    #endregion

    #region DTO

    [Serializable]
    private class ProfileDTO
    {
        public int currency;
        public Dictionary<string, int> items;
        public string playerName;
        public int level;
        public int experience;
        public int hp;
        public int maxHp;
    }

    #endregion
}

