using UnityEngine;
using System.Collections.Generic;

public static class AssetStorage
{
    private const string KEY = "AssetList";

    public static void SaveAll(AssetList data)
    {
        if (data == null)
        {
            Debug.LogWarning("⚠️ AssetList is null — nothing to save.");
            return;
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();

        Debug.Log($"✅ Saved AssetList with {data.Assets?.Count ?? 0} items.");
    }

    public static void SavePage(AssetData data)
    {
        if (data == null) return;

        AssetList list = LoadAll();
        if (list.Assets == null)
            list.Assets = new List<AssetData>();

        // اگر صفحه قبلاً وجود داشت، جایگزینش کن
        int existing = list.Assets.FindIndex(a => a.PageIndex == data.PageIndex);
        if (existing >= 0)
            list.Assets[existing] = data;
        else
            list.Assets.Add(data);

        string json = JsonUtility.ToJson(list);
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();

        Debug.Log($"✅ Saved/Updated page {data.PageIndex} in local list.");
    }
    
    public static AssetList LoadAll()
    {
        if (!PlayerPrefs.HasKey(KEY))
        {
            Debug.LogWarning("⚠️ No AssetList found in PlayerPrefs.");
            return new AssetList { Assets = new List<AssetData>() };
        }

        string json = PlayerPrefs.GetString(KEY);
        AssetList loaded = JsonUtility.FromJson<AssetList>(json);
        if (loaded.Assets == null)
            loaded.Assets = new List<AssetData>();

        return loaded;
    }

    public static AssetData LoadAt(int index)
    {
        AssetList list = LoadAll();
        if (list.Assets == null || index < 0 || index >= list.Assets.Count)
        {
            Debug.LogWarning($"⚠️ Invalid page index {index}. List count: {list.Assets?.Count ?? 0}");
            return null;
        }

        AssetData asset = list.Assets[index];
        Debug.Log($"✅ Loaded asset at index {index}: {asset.Name}");
        return asset;
    }

    public static bool IsOnLocal()
    {
        if (!PlayerPrefs.HasKey(KEY)) return false;
        return true;
    }
}
