using System.Collections.Generic;
using UnityEngine;

// Особое умение фамильяра, открывается узлом книги (specialEffect)
public enum FamiliarSpecial
{
    None,
    // Иногда приносит монету вместе со снятой посылкой
    FindCoin,
    // За снятую посылку с чужим адресом платят больше
    Redirect,
    // Пока фамильяр отдыхает, проклятия на лентах светятся ярче
    CurseSight,
    // Выпущенный призрак даёт больше эктоплазмы
    MoreEcto
}

// Строка отчёта «с характером». {0} = сколько поймал, {1} = слово из reportNoun в нужной форме
[System.Serializable]
public class FamiliarReportLine
{
    [TextArea] public string text;
    [Tooltip("Строку можно выбрать, только если поймано не меньше")]
    [Min(1)] public int minCount = 1;
}

// Фамильяр: что снимает, как часто, чем открывается. Видит только то, что видит камера.
[CreateAssetMenu(menuName = "Pumpkin Post/Familiar")]
public class FamiliarDefinition : ScriptableObject
{
    public string displayName = "Чёрный кот";
    [Tooltip("Женский род: в отчёте «Сова помогла», а не «помог»")]
    public bool feminine;
    public Sprite sprite;
    [Tooltip("Звук прыжка (мяу, карр...). Пусто = общий звук из SoundBank")]
    public AudioClip[] pounceClips;

    [Header("Открытие и скорость")]
    [Tooltip("Unlock-эффект из книги, который даёт фамильяра")]
    public string unlockEffect = Effects.FamCat;
    [Tooltip("Эффект книги, ускоряющий именно этого фамильяра (доля, 0.2 = +20%)")]
    public string rateEffect;
    [Tooltip("Секунды отдыха после работы")]
    public float cooldown = 4f;
    [Tooltip("Сколько летит до посылки, прежде чем снять её")]
    public float pounceTime = 0.35f;

    [Header("Цели")]
    public List<ParcelKind> targets = new() { ParcelKind.Trash };
    [Tooltip("Выпускает призраков из посылок")]
    public bool releasesGhosts;

    [Header("Где ловит")]
    [Tooltip("Ловит посылки только в этой части ленты (доля длины), чтобы было видно на мониторе")]
    [Range(0f, 1f)] public float zoneStart = 0.3f;
    [Range(0f, 1f)] public float zoneEnd = 0.92f;
    [Tooltip("Эффект книги: на столько (доля ленты) раньше начинает ловить")]
    public string zoneEffect;
    [Tooltip("Ближе к началу ленты не ловит даже с прокачкой: посылку должно быть видно")]
    [Range(0f, 1f)] public float minZoneStart = 0.08f;

    [Header("Двойной прыжок")]
    [Tooltip("Эффект книги: шанс (доля) сразу прыгнуть ещё раз, без отдыха")]
    public string doubleEffect;

    [Header("Особое умение")]
    public FamiliarSpecial special;
    [Tooltip("Unlock-эффект книги, открывающий умение")]
    public string specialEffect;
    [Range(0f, 1f)] public float specialChance = 1f;
    [Tooltip("Сколько монет / эктоплазмы добавляет умение")]
    public int specialAmount = 1;

    [Header("Отчёт")]
    [Tooltip("Цвет строки фамильяра в отчёте (тёмный, читаемый на светлой бумаге)")]
    public Color reportColor = new(0.42f, 0.29f, 0.55f);
    [Tooltip("Формы слова для {1}: «штуку|штуки|штук»")]
    public string reportNoun = "штуку|штуки|штук";
    public List<FamiliarReportLine> reportLines = new();

    // Случайная строка отчёта, подходящая к числу пойманных
    public string GetReportLine(int count)
    {
        List<FamiliarReportLine> fit = reportLines.FindAll(l => l.minCount <= count && !string.IsNullOrEmpty(l.text));
        if (fit.Count == 0)
            return $"{displayName} {(feminine ? "помогла" : "помог")}: {count}";

        string line = fit[Random.Range(0, fit.Count)].text;
        return string.Format(line, count, RuText.Plural(count, reportNoun));
    }
}
