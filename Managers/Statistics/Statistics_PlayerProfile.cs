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

        foreach (var kvp in items)
        {
            GlobalVarsManager.Set(kvp.Key, kvp.Value);
        }
    }

    private void SyncCurrency()
    {
        GlobalVarsManager.Set(CURRENCY_KEY, currency);
    }

    private void SyncLevel()
    {
        GlobalVarsManager.Set(LEVEL_KEY, level);
    }

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
        SaveManager.Save();
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
            SaveManager.Save();
            return true;
        }

        return false;
    }

    public bool HasEnoughCurrency(int amount)
    {
        return currency >= amount;
    }

    public void SetCurrency(int amount)
    {
        currency = Mathf.Max(0, amount);
        SyncCurrency();
        SaveManager.Save();
    }

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
        SaveManager.Save();
    }

    public bool RemoveItem(string itemID, int count = 1)
    {
        if (!items.ContainsKey(itemID))
            return false;

        if (items[itemID] >= count)
        {
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

            SaveManager.Save();
            return true;
        }

        return false;
    }

    public bool HasItem(string itemID, int count = 1)
    {
        return items.ContainsKey(itemID) && items[itemID] >= count;
    }

    public int GetItemCount(string itemID)
    {
        return items.ContainsKey(itemID) ? items[itemID] : 0;
    }

    public void ClearItems()
    {
        foreach (var key in new List<string>(items.Keys))
        {
            RemoveItemFromGlobalVars(key);
        }
        items.Clear();
        SaveManager.Save();
    }

    #endregion

    #region Profile Management

    public void SetPlayerName(string name)
    {
        playerName = name;
        SaveManager.Save();
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

        SaveManager.Save();
    }

    private void LevelUp()
    {
        level++;
        experience -= GetExperienceForNextLevel();
        SyncLevel();
        SyncExperience();
        Debug.Log($"Level up! New level: {level}");
    }

    private int GetExperienceForNextLevel()
    {
        return level * 100;
    }

    public void ResetProfile()
    {
        currency = 0;
        ClearItems();
        playerName = "";
        level = 1;
        experience = 0;
        SyncToGlobalVars();
        SaveManager.Save();
    }

    #endregion

    #region Bonus Helpers

    public static string GetBonusID(BonusType bonusType)
    {
        return $"bonus_{bonusType.ToString().ToLower()}";
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
            experience = profile.experience
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
            profile.level = loaded.level;
            profile.experience = loaded.experience;
        }
        else
        {
            profile.currency = 0;
            profile.items.Clear();
            profile.playerName = "";
            profile.level = 1;
            profile.experience = 0;
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
    }

    #endregion
}

