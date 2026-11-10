using System.Collections.Generic;
using GridSkillTree;
using UnityEngine;

// Все стартовые цифры игры в одном месте. Лежит в Resources/GameConfig.
[CreateAssetMenu(menuName = "Pumpkin Post/Game Config")]
public class GameConfig : ScriptableObject
{
    private static GameConfig instance;

    public static GameConfig Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<GameConfig>("GameConfig");
            return instance;
        }
    }

    [Header("Валюты")]
    public CurrencyDefinition coin;
    public CurrencyDefinition ecto;

    [Header("Книга")]
    [Tooltip("Стартовый узел дерева, покупается автоматически")]
    public string rootNodeId = "night_shift";

    [Header("Посылки")]
    [Tooltip("Общий множитель размера всех посылок на ленте (поверх scale у каждой)")]
    public float parcelScale = 1.3f;

    [Header("Смена")]
    public float shiftDuration = 90f;
    public List<NightNote> nightNotes = new();
    [Tooltip("Новости о покупках в книге. Порядок = приоритет, за ночь показывается не больше maxUnlockNotes")]
    public List<UnlockNote> unlockNotes = new();
    [Min(1)] public int maxUnlockNotes = 2;
    [Tooltip("Обычные записки, когда ничего нового не случилось. Идут по кругу по номеру ночи")]
    [TextArea] public List<string> idleNotes = new();

    [Header("Финал")]
    [Tooltip("Во сколько раз чаще идут посылки в финальную смену (2 = поток x2)")]
    public float finalSpawnMult = 2f;
    public string finalTitle = "ПОЛНОЧЬ ХЕЛЛОУИНА";
    [TextArea] public string finalNote = "Испытательный срок истекает. Все линии открыты, поток удвоен. Не подведи!";

    [Header("Лента")]
    public float beltSpeed = 1.5f;
    public float spawnInterval = 1.6f;
    [Tooltip("Насколько реже поток на одну ленту за каждую дополнительную линию")]
    public float lineSpawnSlowdown = 0.35f;

    [Header("Ритм потока (средний темп остаётся spawnInterval)")]
    [Tooltip("Разброс каждого интервала: 0.5 = от x0.5 до x1.5")]
    [Range(0f, 0.9f)] public float spawnJitter = 0.5f;
    [Tooltip("Волны: то густо, то пусто. 0.6 = поток плавно ходит от x0.4 до x1.6")]
    [Range(0f, 0.9f)] public float spawnWaveStrength = 0.6f;
    [Tooltip("Примерная длина одной волны, секунды")]
    [Min(1f)] public float spawnWavePeriod = 10f;
    [Tooltip("Шанс, что следующая посылка поедет вплотную (пачка из 2-3 штук)")]
    [Range(0f, 0.6f)] public float spawnClusterChance = 0.2f;
    [Tooltip("Интервал внутри пачки, доля от обычного")]
    public Vector2 spawnClusterGap = new(0.3f, 0.45f);
    [Tooltip("Посылки не наезжают друг на друга: минимальное расстояние между ними, мировые единицы")]
    [Min(0f)] public float minParcelSpacing = 1.7f;

    [Header("Поломки мониторов")]
    [Tooltip("С какой ночи мониторы начинают ломаться")]
    [Min(1)] public int faultFromNight = 2;
    [Tooltip("Среднее время между поломками (на все мониторы вместе), секунды. Узел fault_rate растягивает")]
    [Min(1f)] public float faultInterval = 35f;
    [Tooltip("Разброс интервала: 0.4 = от 60% до 140% среднего")]
    [Range(0f, 0.9f)] public float faultIntervalJitter = 0.4f;
    [Tooltip("Первые секунды смены ничего не ломается")]
    [Min(0f)] public float faultGrace = 12f;
    [Tooltip("Починенный монитор не ломается снова столько секунд")]
    [Min(0f)] public float faultCooldown = 10f;
    [Tooltip("Сколько ударов нужно, чтобы починить. Узел fault_hits снимает по одному")]
    [Min(1)] public int faultHits = 3;
    [Tooltip("Скорость ленты, пока её монитор сломан. Узел fault_stop («Песочные часы») снимает по доле за уровень, до остановки")]
    [Range(0f, 1f)] public float faultBeltSpeed = 1f;
    [Tooltip("Стартовый шанс мусора. Узел trash_cut снижает его")]
    [Range(0f, 1f)] public float trashChance = 0.45f;
    [Tooltip("На сколько мусора меньше с каждой ночью: дальше снимать приходится чужие адреса и проклятия")]
    [Range(0f, 0.2f)] public float trashDropPerNight = 0.07f;
    [Tooltip("Ниже этого шанс мусора не падает от ночей (узел trash_cut режет дальше)")]
    [Range(0f, 1f)] public float trashNightFloor = 0.3f;
    [Tooltip("Ниже этого шанс мусора не опустится, работа фамильярам останется")]
    [Range(0f, 1f)] public float minTrashChance = 0.15f;

    [Header("Комбо")]
    [Tooltip("Сколько верных решений подряд нужно на следующую ступень")]
    public int comboStep = 5;
    public float comboStepBonus = 0.5f;
    public float comboMax = 2f;
    [Tooltip("Каждые N верных кликов подряд дают 1 эктоплазму. Без этого до первых призраков экто не заработать")]
    public int ectoComboStreak = 10;

    [Header("Посылки")]
    public List<ParcelDefinition> shapes = new();
    public ParcelDefinition goldenShape;
    public List<AddresseeInfo> addressees = new();
    public List<KindRule> kindRules = new();
    public List<TraitChance> traitChances = new();
    public int ghostEcto = 1;
    [Tooltip("Множитель штрафа за жалобу, которую камера не могла увидеть (нужное улучшение не куплено)")]
    [Range(0f, 1f)] public float unseenPenaltyMult = 0.25f;

    [Header("Камера")]
    [Tooltip("Разрешение RenderTexture по уровню «Протереть глаза» (0..3). Ниже 192×108 посылки сливаются в кашу")]
    public Vector2Int[] cameraResolutions =
    {
        new(192, 108), new(256, 144), new(320, 180), new(400, 225)
    };

    [Header("Свечение проклятий, мимиков и призраков: у каждого свой узел (Глаз ведьмы, Сердечная руна, Призрачная свеча)")]
    [Tooltip("Прозрачность свечения на пике без улучшений (слабое)")]
    [Range(0f, 1f)] public float glowBaseStrength = 0.35f;
    [Tooltip("+ к прозрачности за уровень узла")]
    public float glowStrengthPerLevel = 0.22f;
    [Tooltip("Сколько секунд свечение горит без улучшений")]
    public float glowOnTime = 0.8f;
    [Tooltip("+ секунд горения за уровень узла")]
    public float glowOnPerLevel = 0.35f;
    [Tooltip("Сколько секунд свечение погашено без улучшений")]
    public float glowOffTime = 2.8f;
    [Tooltip("- секунд паузы за уровень узла")]
    public float glowOffPerLevel = 0.8f;
    public float glowMinOffTime = 0.3f;
    [Tooltip("Плавное появление и угасание внутри вспышки, секунды")]
    public float glowFade = 0.25f;
    [Tooltip("Ночное зрение совы: на сколько уровней «Глаза ведьмы» ярче проклятия, пока сова отдыхает")]
    public float curseSightLevels = 2f;

    public KindRule GetRule(ParcelKind kind)
    {
        foreach (KindRule rule in kindRules)
            if (rule.kind == kind)
                return rule;

        Debug.LogError($"GameConfig: нет правила для {kind}");
        return new KindRule { kind = kind };
    }

    public string GetNightNote(int night, GameStats stats)
    {
        foreach (NightNote note in nightNotes)
            if (note.night == night && (string.IsNullOrEmpty(note.hideIfUnlocked) || !stats.HasUnlock(note.hideIfUnlocked)))
                return note.text;
        return null;
    }

    public string GetIdleNote(int night)
    {
        return idleNotes.Count == 0 ? null : idleNotes[(night - 1) % idleNotes.Count];
    }

    public AddresseeInfo GetAddressee(Addressee id)
    {
        foreach (AddresseeInfo info in addressees)
            if (info.id == id)
                return info;
        return null;
    }
}
