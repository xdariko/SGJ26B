using System;
using UnityEngine;

public enum ParcelKind
{
    Normal,
    Trash,
    WrongAddress,
    Cursed,
    Mimic,
    Golden
}

public enum Addressee
{
    Any,
    Vampires,
    Witches,
    Ghosts,
    Pumpkinheads
}

// Что выпадает при спавне. Одна посылка = один «особый» признак.
public enum ParcelTrait
{
    WrongAddress,
    Cursed,
    Mimic,
    Ghost,
    Golden
}

// Правило типа посылки: что с ней надо сделать и сколько это стоит.
[Serializable]
public class KindRule
{
    public ParcelKind kind;
    [Tooltip("true = посылку нужно снять с ленты")]
    public bool mustRemove;
    [Tooltip("Монеты за верное снятие. За доставку платит сам предмет (ParcelDefinition.value)")]
    public int reward;
    [Tooltip("Штраф за доставку того, что надо было снять")]
    public int penalty;
    [Tooltip("С какого уровня камеры игрок видит этот тип")]
    public int requiredCamLevel;
    [Tooltip("Строка жалобы в отчёте")]
    public string complaint;
    [Tooltip("Подсказка в отчёте, если камера этого не видела: какое улучшение купить")]
    public string upgradeHint;
}

// Записка в начале ночи: что нового в почте
[Serializable]
public class NightNote
{
    [Min(1)] public int night = 1;
    [TextArea] public string text;
}

[Serializable]
public class TraitChance
{
    public ParcelTrait trait;
    [Min(1)] public int minNight = 1;
    [Range(0f, 1f)] public float chance = 0.1f;
    [Tooltip("Минимальный уровень камеры, чтобы признак начал выпадать. -1 = выпадает даже невидимым")]
    public int minCamLevel = -1;
}

[Serializable]
public class AddresseeInfo
{
    public Addressee id;
    public string displayName;
    public Sprite stamp;
    public Color color = Color.white;
}

// Результат броска спавнера: из чего собрать посылку.
public struct ParcelSpec
{
    public ParcelDefinition shape;
    public ParcelKind kind;
    public Addressee address;
    public bool hasGhost;
}
