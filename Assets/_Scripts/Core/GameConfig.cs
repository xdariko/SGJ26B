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

    [Header("Смена")]
    public float shiftDuration = 90f;
    public List<NightNote> nightNotes = new();

    [Header("Финал")]
    [Tooltip("Во сколько раз чаще идут посылки в финальную смену (2 = поток x2)")]
    public float finalSpawnMult = 2f;
    public string finalTitle = "ПОЛНОЧЬ ХЕЛЛОУИНА";
    [TextArea] public string finalNote = "Король Тыкв ждёт свой заказ. Все линии открыты, поток удвоен. Не подведи!";

    [Header("Лента")]
    public float beltSpeed = 1.5f;
    public float spawnInterval = 1.6f;
    [Tooltip("Насколько реже поток на одну ленту за каждую дополнительную линию")]
    public float lineSpawnSlowdown = 0.35f;
    [Tooltip("Стартовый шанс мусора. Узел trash_cut снижает его")]
    [Range(0f, 1f)] public float trashChance = 0.45f;
    [Tooltip("Ниже этого шанс мусора не опустится, работа фамильярам останется")]
    [Range(0f, 1f)] public float minTrashChance = 0.15f;

    [Header("Комбо")]
    [Tooltip("Сколько верных решений подряд нужно на следующую ступень")]
    public int comboStep = 5;
    public float comboStepBonus = 0.5f;
    public float comboMax = 2f;
    [Tooltip("Каждые N верных кликов подряд дают 1 эктоплазму. Без этого до «Призрачной свечи» экто не заработать")]
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
    [Tooltip("Разрешение RenderTexture по уровню камеры (0..3)")]
    public Vector2Int[] cameraResolutions =
    {
        new(96, 54), new(192, 108), new(384, 216), new(384, 216)
    };

    public KindRule GetRule(ParcelKind kind)
    {
        foreach (KindRule rule in kindRules)
            if (rule.kind == kind)
                return rule;

        Debug.LogError($"GameConfig: нет правила для {kind}");
        return new KindRule { kind = kind };
    }

    public string GetNightNote(int night)
    {
        foreach (NightNote note in nightNotes)
            if (note.night == night)
                return note.text;
        return null;
    }

    public AddresseeInfo GetAddressee(Addressee id)
    {
        foreach (AddresseeInfo info in addressees)
            if (info.id == id)
                return info;
        return null;
    }
}
