using System;
using System.Collections.Generic;
using UnityEngine;

// Банк звуков: Resources/SoundBank.asset. Пустой слот (нет клипов) = процедурная заглушка из ProceduralAudio,
// так что игра звучит и без файлов. Чтобы заменить звук: перетащить клип(ы) в нужную строку.
[CreateAssetMenu(fileName = "SoundBank", menuName = "Audio/Sound Bank")]
public class SoundBank : ScriptableObject
{
    [Serializable]
    public class SfxEntry
    {
        public SfxId id;
        [Tooltip("Пусто = процедурная заглушка. Несколько = случайный")]
        public AudioClip[] clips;
        [Range(0f, 1.5f)] public float volume = 1f;
        [Tooltip("Случайный разброс высоты, ±")]
        [Range(0f, 0.5f)] public float pitchJitter = 0.06f;
        [Tooltip("Не чаще, чем раз в N секунд (против каши при 4 лентах)")]
        public float cooldown = 0.03f;
    }

    [Serializable]
    public class MusicEntry
    {
        public MusicId id;
        [Tooltip("Пусто = процедурная заглушка")]
        public AudioClip clip;
        [Tooltip("Ещё треки для этого места: при каждом запуске выбирается случайный из clip + этих")]
        public AudioClip[] alternates;
        [Range(0f, 1.5f)] public float volume = 1f;

        public bool HasClip => clip != null || (alternates != null && alternates.Length > 0);

        // Случайный трек, но не тот же, что в прошлый раз (если есть из чего выбрать)
        public AudioClip Pick(AudioClip previous)
        {
            var all = new List<AudioClip>();
            if (clip != null)
                all.Add(clip);
            if (alternates != null)
                foreach (AudioClip c in alternates)
                    if (c != null)
                        all.Add(c);
            if (all.Count == 0)
                return null;
            if (all.Count > 1 && previous != null)
                all.Remove(previous);
            return all[UnityEngine.Random.Range(0, all.Count)];
        }
    }

    public List<SfxEntry> sfx = new();
    public List<MusicEntry> music = new();
    [Tooltip("Секунды на смену трека")]
    public float musicFade = 1.2f;
    [Tooltip("Общая громкость процедурных заглушек относительно файлов")]
    [Range(0f, 1f)] public float proceduralSfxVolume = 0.8f;
    [Range(0f, 1f)] public float proceduralMusicVolume = 0.55f;

    private static SoundBank instance;
    public static SoundBank Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<SoundBank>("SoundBank");
                if (instance == null)
                    instance = CreateInstance<SoundBank>();
            }
            return instance;
        }
    }

    // Значения по умолчанию для строк, которых нет в ассете
    private static readonly Dictionary<SfxId, (float vol, float jitter, float cd)> Defaults = new()
    {
        { SfxId.UiClick, (0.5f, 0.05f, 0.05f) },
        { SfxId.Snatch, (0.8f, 0.04f, 0.02f) },
        { SfxId.WrongRemove, (0.8f, 0.03f, 0.08f) },
        { SfxId.Deliver, (0.3f, 0.08f, 0.09f) },
        { SfxId.Complaint, (0.75f, 0.03f, 0.25f) },
        { SfxId.GhostRelease, (0.7f, 0.08f, 0.1f) },
        { SfxId.Ecto, (0.6f, 0f, 0.1f) },
        { SfxId.FamiliarPounce, (0.45f, 0.12f, 0.06f) },
        { SfxId.NightStart, (0.7f, 0f, 0.5f) },
        { SfxId.ShiftEnd, (0.8f, 0f, 0.5f) },
        { SfxId.ClockTick, (0.5f, 0f, 0.2f) },
        { SfxId.ReportLine, (0.45f, 0.1f, 0.02f) },
        { SfxId.Stamp, (0.9f, 0.04f, 0.1f) },
        { SfxId.Buy, (0.7f, 0.02f, 0.05f) },
        { SfxId.PageTurn, (0.6f, 0.1f, 0.1f) },
        { SfxId.Splash, (0.7f, 0f, 1f) },
        { SfxId.BookOpen, (0.6f, 0.05f, 0.2f) },
        { SfxId.UiHover, (0.3f, 0.06f, 0.06f) },
        { SfxId.MonitorBreak, (0.6f, 0.05f, 0.2f) },
        { SfxId.MonitorHit, (0.8f, 0.08f, 0.04f) },
        { SfxId.MonitorFix, (0.6f, 0.02f, 0.1f) },
    };

    private Dictionary<SfxId, SfxEntry> sfxMap;

    public SfxEntry GetSfx(SfxId id)
    {
        if (sfxMap == null)
        {
            sfxMap = new Dictionary<SfxId, SfxEntry>();
            foreach (SfxEntry e in sfx)
                if (e != null)
                    sfxMap[e.id] = e;
        }

        if (!sfxMap.TryGetValue(id, out SfxEntry entry))
        {
            entry = new SfxEntry { id = id };
            if (Defaults.TryGetValue(id, out var d))
            {
                entry.volume = d.vol;
                entry.pitchJitter = d.jitter;
                entry.cooldown = d.cd;
            }
            sfxMap[id] = entry;
        }
        return entry;
    }

    public MusicEntry GetMusic(MusicId id)
    {
        foreach (MusicEntry e in music)
            if (e != null && e.id == id)
                return e;
        return null;
    }

    private void OnValidate()
    {
        sfxMap = null;
    }

#if UNITY_EDITOR
    // Заполнить список всеми id с громкостями по умолчанию (контекстное меню ассета)
    [ContextMenu("Добавить недостающие строки")]
    private void FillMissing()
    {
        foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
        {
            if (sfx.Exists(e => e.id == id))
                continue;
            var d = Defaults.TryGetValue(id, out var v) ? v : (1f, 0.06f, 0.03f);
            sfx.Add(new SfxEntry { id = id, volume = d.Item1, pitchJitter = d.Item2, cooldown = d.Item3 });
        }
        foreach (MusicId id in Enum.GetValues(typeof(MusicId)))
            if (id != MusicId.None && !music.Exists(e => e.id == id))
                music.Add(new MusicEntry { id = id });
        sfxMap = null;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
