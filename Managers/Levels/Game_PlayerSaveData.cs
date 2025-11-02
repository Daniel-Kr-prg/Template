using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class Game_PlayerSaveData : MonoBehaviour
{
    // Количество валюты у игрока
    public int currency = 0;
    // Словарь бонусов: название бонуса -> количество
    public Dictionary<string, int> bonuses = new Dictionary<string, int>();

    private PlayerSaveDataSaveItem saveItem;

    private void Start()
    {
        saveItem = new PlayerSaveDataSaveItem("PlayerSaveData", this);
    }
}

/// <summary>
/// SaveItem для Game_PlayerSaveData. Сохраняет валюту и бонусы.
/// </summary>
public class PlayerSaveDataSaveItem : SaveItem
{
    private readonly Game_PlayerSaveData saveData;

    public PlayerSaveDataSaveItem(string id, Game_PlayerSaveData target)
        : base(id, target)
    {
        saveData = target;
    }

    public override string CreateSaveData(object sourceObject)
    {
        // Сохраняем всё как JSON
        return JsonConvert.SerializeObject(new SaveDataDTO
        {
            currency = saveData.currency,
            bonuses = saveData.bonuses
        });
    }

    protected override void LoadCallback()
    {
        var loaded = SaveManager.Load<SaveDataDTO>(id);
        if (loaded != null)
        {
            saveData.currency = loaded.currency;
            saveData.bonuses = loaded.bonuses ?? new Dictionary<string, int>();
        }
        else
        {
            saveData.currency = 0;
            saveData.bonuses.Clear();
        }
    }

    [Serializable]
    private class SaveDataDTO
    {
        public int currency;
        public Dictionary<string, int> bonuses;
    }
} 