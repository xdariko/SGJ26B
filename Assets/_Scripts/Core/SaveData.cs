using UnityEngine;

// Наш собственный прогресс. Дерево и кошелёк сохраняет GridSkillTree сам.
public static class SaveData
{
    private const string NightKey = "PumpkinPost.Night";
    private const string ParcelsKey = "PumpkinPost.TotalParcels";
    private const string ComplaintsKey = "PumpkinPost.TotalComplaints";
    private const string GhostsKey = "PumpkinPost.TotalGhosts";
    private const string CoinsKey = "PumpkinPost.TotalCoins";
    private const string IntroKey = "PumpkinPost.IntroSeen";
    private const string HintsKey = "PumpkinPost.HintsSeen";

    // Меню попросило начать заново. Сбросить прогресс можно только в Game: там живут дерево и кошелёк
    public static bool NewGameRequested;

    // Есть что продолжать: отыграна хотя бы одна смена или уже показано вступление
    // (иначе после выхода посреди первой смены нет ни «Новой игры», ни комикса)
    public static bool HasProgress => PlayerPrefs.GetInt(NightKey, 1) > 1 || IntroSeen;

    public static bool IntroSeen
    {
        get => PlayerPrefs.GetInt(IntroKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(IntroKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static int Night
    {
        get => PlayerPrefs.GetInt(NightKey, 1);
        set
        {
            PlayerPrefs.SetInt(NightKey, value);
            PlayerPrefs.Save();
        }
    }

    // Подсказки обучения, которые уже показаны. Храним одной строкой: «|trash|book|»
    public static bool IsHintSeen(string id) => PlayerPrefs.GetString(HintsKey, "|").Contains("|" + id + "|");

    public static void MarkHintSeen(string id)
    {
        if (IsHintSeen(id))
            return;

        PlayerPrefs.SetString(HintsKey, PlayerPrefs.GetString(HintsKey, "|") + id + "|");
        PlayerPrefs.Save();
    }

    // Итоги за всю игру, для титров
    public static int TotalParcels => PlayerPrefs.GetInt(ParcelsKey, 0);
    public static int TotalComplaints => PlayerPrefs.GetInt(ComplaintsKey, 0);
    public static int TotalGhosts => PlayerPrefs.GetInt(GhostsKey, 0);
    public static int TotalCoins => PlayerPrefs.GetInt(CoinsKey, 0);

    public static void AddShift(ShiftScore s)
    {
        Add(ParcelsKey, s.delivered + s.removedCorrect + s.removedWrong + s.complaints + s.ghosts);
        Add(ComplaintsKey, s.complaints);
        Add(GhostsKey, s.ghosts);
        Add(CoinsKey, s.coinsEarned);
        PlayerPrefs.Save();
    }

    private static void Add(string key, int value)
    {
        PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) + value);
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(NightKey);
        PlayerPrefs.DeleteKey(ParcelsKey);
        PlayerPrefs.DeleteKey(ComplaintsKey);
        PlayerPrefs.DeleteKey(GhostsKey);
        PlayerPrefs.DeleteKey(CoinsKey);
        PlayerPrefs.DeleteKey(IntroKey);
        PlayerPrefs.DeleteKey(HintsKey);

        if (G.tree != null)
            G.tree.ResetProgress();

        if (G.wallet != null)
            G.wallet.ResetToStartingBalances();

        PlayerPrefs.Save();
    }
}
