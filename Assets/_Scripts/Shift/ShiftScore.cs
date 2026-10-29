using System;
using System.Collections.Generic;
using UnityEngine;

// Счёт одной смены + комбо. Чистые данные, UI их только читает.
public class ShiftScore
{
    private readonly GameConfig cfg;
    private readonly GameStats stats;

    public int night;
    public int delivered;
    public int removedCorrect;
    public int removedWrong;
    public int complaints;
    public int unseenComplaints;
    public int ghosts;
    public int coinsEarned;
    public int coinsLost;
    public int ectoEarned;
    public int streak;
    public int bestStreak;
    public float bestMult = 1f;
    public readonly List<string> complaintLog = new();
    // Сколько жалоб каждого типа было из-за того, что камера не видела признак
    public readonly Dictionary<ParcelKind, int> unseenByKind = new();
    // Сколько снял каждый фамильяр
    public readonly Dictionary<FamiliarDefinition, int> familiarCatches = new();

    public int NetCoins => coinsEarned - coinsLost;

    public float ComboMult =>
        Mathf.Min(stats.comboMax, 1f + streak / Mathf.Max(1, cfg.comboStep) * cfg.comboStepBonus);

    public ShiftScore(int night, GameConfig cfg, GameStats stats)
    {
        this.night = night;
        this.cfg = cfg;
        this.stats = stats;
    }

    // Применяет комбо к монетам в исходе и записывает его в статистику.
    // Комбо растёт только от верных кликов игрока; доставка и работа фамильяров его не трогают, любая ошибка сбрасывает.
    public void Register(ref ParcelOutcome o)
    {
        bool playerAction = o.action != ParcelAction.Delivered && o.familiar == null;

        if (o.familiar != null)
        {
            familiarCatches.TryGetValue(o.familiar, out int caught);
            familiarCatches[o.familiar] = caught + 1;
        }

        if (!o.correct)
        {
            streak = 0;
        }
        else if (playerAction)
        {
            streak++;
            bestStreak = Math.Max(bestStreak, streak);
            if (cfg.ectoComboStreak > 0 && streak % cfg.ectoComboStreak == 0)
                o.ecto += 1;
        }

        o.comboMult = playerAction ? ComboMult : 1f;
        bestMult = Mathf.Max(bestMult, o.comboMult);
        o.coins = Mathf.RoundToInt(o.coins * o.comboMult);

        coinsEarned += o.coins;
        coinsLost += o.penalty;
        ectoEarned += o.ecto;

        switch (o.action)
        {
            case ParcelAction.GhostReleased:
                ghosts++;
                break;
            case ParcelAction.Removed:
                if (o.correct) removedCorrect++;
                else removedWrong++;
                break;
            case ParcelAction.Delivered:
                if (o.correct)
                {
                    delivered++;
                }
                else
                {
                    complaints++;
                    if (o.unseen)
                    {
                        unseenComplaints++;
                        unseenByKind.TryGetValue(o.kind, out int n);
                        unseenByKind[o.kind] = n + 1;
                    }
                    if (!string.IsNullOrEmpty(o.complaint)) complaintLog.Add(o.complaint);
                }
                break;
        }
    }
}
