using System.Collections.Generic;

// Что написать в записке под «НОЧЬ N». По приоритету:
// 1) новости о покупках в книге (лента, новые посылки, фамильяр), о каждой один раз;
// 2) записка этой ночи из GameConfig.nightNotes;
// 3) обычная записка из idleNotes, чтобы панель не была пустой.
public static class NightNotes
{
    public static string Take(GameConfig cfg, GameStats stats, int night)
    {
        // Сохранение старше этой системы: всё купленное раньше считаем уже объявленным, иначе вывалится всё разом
        if (!SaveData.HasAnnouncedList && night > 1)
        {
            SaveData.MarkAnnounced(stats.Unlocks);
            return Fallback(cfg, stats, night);
        }

        List<string> lines = new();
        List<string> announced = new();
        foreach (UnlockNote n in cfg.unlockNotes)
        {
            if (!stats.HasUnlock(n.effectId) || SaveData.IsAnnounced(n.effectId))
                continue;

            announced.Add(n.effectId);
            if (lines.Count < cfg.maxUnlockNotes)
                lines.Add(n.text);
        }

        // Объявленными помечаем все новые, даже не влезшие: завтра они уже не новость
        SaveData.MarkAnnounced(announced);
        return lines.Count > 0 ? string.Join("\n", lines) : Fallback(cfg, stats, night);
    }

    private static string Fallback(GameConfig cfg, GameStats stats, int night)
    {
        return cfg.GetNightNote(night, stats) ?? cfg.GetIdleNote(night);
    }
}
