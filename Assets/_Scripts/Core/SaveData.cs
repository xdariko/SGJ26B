using UnityEngine;

// Наш собственный прогресс. Дерево и кошелёк сохраняет GridSkillTree сам.
public static class SaveData
{
    private const string NightKey = "PumpkinPost.Night";

    public static int Night
    {
        get => PlayerPrefs.GetInt(NightKey, 1);
        set
        {
            PlayerPrefs.SetInt(NightKey, value);
            PlayerPrefs.Save();
        }
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(NightKey);

        if (G.tree != null)
            G.tree.ResetProgress();

        if (G.wallet != null)
            G.wallet.ResetToStartingBalances();

        PlayerPrefs.Save();
    }
}
