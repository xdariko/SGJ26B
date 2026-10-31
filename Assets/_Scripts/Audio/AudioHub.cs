using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Весь звук игры. Создаётся сам до первой сцены и живёт между сценами.
// Громкость считаем сами, без AudioMixer: в WebGL микшер не работает.
// Звуки:  AudioHub.Play(SfxId.Snatch);   Музыка: AudioHub.PlayMusic(MusicId.Shift);
public class AudioHub : MonoBehaviour
{
    public const string MusicPrefKey = "MusicVol";
    public const string SfxPrefKey = "SFXVol";

    private const int PoolSize = 20;

    private static AudioHub instance;

    private readonly List<AudioSource> pool = new();
    private readonly Dictionary<SfxId, float> lastPlayed = new();
    private readonly Dictionary<SfxId, AudioClip> proceduralSfx = new();
    private readonly Dictionary<MusicId, AudioClip> proceduralMusic = new();
    private readonly List<RaycastResult> uiHits = new();

    private AudioSource musicA, musicB;
    private AudioSource musicCurrent;
    private float musicCurrentBase = 1f;
    private float musicFadeT = 1f;
    private float musicFromStartVol;
    private AudioSource musicFrom;
    private MusicId currentMusic = MusicId.None;
    private int poolCursor;

    public static float MusicVolume { get; private set; } = 1f;
    public static float SfxVolume { get; private set; } = 1f;

    private static SoundBank Bank => SoundBank.Instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        var go = new GameObject("[AudioHub]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AudioHub>();
    }

    private void Awake()
    {
        MusicVolume = PlayerPrefs.GetFloat(MusicPrefKey, 1f);
        SfxVolume = PlayerPrefs.GetFloat(SfxPrefKey, 1f);

        for (int i = 0; i < PoolSize; i++)
            pool.Add(CreateSource("sfx " + i, false));

        // Свой слушатель: в Splash/Menu его нет, а все звуки у нас 2D
        gameObject.AddComponent<AudioListener>();

        musicA = CreateSource("music A", true);
        musicB = CreateSource("music B", true);

        SceneManager.sceneLoaded += HandleSceneLoaded;
        StartCoroutine(WarmUp());
    }

    // Заранее синтезировать заглушки по одной за кадр, чтобы не было рывка в момент первого звука
    private System.Collections.IEnumerator WarmUp()
    {
        yield return null;
        foreach (MusicId id in new[] { MusicId.Menu, MusicId.Shift, MusicId.Book, MusicId.Final, MusicId.Ending })
        {
            SoundBank.MusicEntry e = Bank.GetMusic(id);
            if ((e == null || e.clip == null) && !proceduralMusic.ContainsKey(id))
                proceduralMusic[id] = ProceduralAudio.MakeMusic(id);
            yield return null;
        }
        foreach (SfxId id in (SfxId[])System.Enum.GetValues(typeof(SfxId)))
        {
            SoundBank.SfxEntry e = Bank.GetSfx(id);
            if (e.clips == null || e.clips.Length == 0)
                GetProceduralSfx(id);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private AudioSource CreateSource(string sourceName, bool loop)
    {
        var go = new GameObject(sourceName);
        go.transform.SetParent(transform, false);
        AudioSource s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f;
        // Пауза ставит timeScale = 0, звук от этого не зависит
        s.ignoreListenerPause = true;
        return s;
    }

    // Музыка по сцене. В Game музыку дальше ведёт GameAudio по фазам.
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Два слушателя = предупреждение каждый кадр: оставляем только свой
        foreach (AudioListener l in FindObjectsByType<AudioListener>(FindObjectsInactive.Include))
            if (l.gameObject != gameObject)
                l.enabled = false;

        if (scene.name == Scenes.Menu)
            PlayMusic(MusicId.Menu);
        else if (scene.name == Scenes.Splash)
            PlayMusic(MusicId.None);

        if (FindAnyObjectByType<ShiftController>() != null && FindAnyObjectByType<GameAudio>() == null)
            new GameObject("[GameAudio]").AddComponent<GameAudio>();
    }

    // ---------- API ----------

    public static void Play(SfxId id, float pitch = 1f, float volume = 1f)
    {
        if (instance != null)
            instance.PlayInternal(id, null, pitch, volume);
    }

    // Свой клип поверх банка (например, у фамильяра свой звук). Пусто = звук банка.
    public static void Play(SfxId id, AudioClip[] overrideClips, float pitch = 1f, float volume = 1f)
    {
        if (instance != null)
            instance.PlayInternal(id, overrideClips, pitch, volume);
    }

    public static void PlayMusic(MusicId id)
    {
        if (instance != null)
            instance.PlayMusicInternal(id);
    }

    public static void SetMusicVolume(float v)
    {
        MusicVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(MusicPrefKey, MusicVolume);
        if (instance != null && instance.musicCurrent != null && instance.musicFadeT >= 1f)
            instance.musicCurrent.volume = instance.musicCurrentBase * MusicVolume;
    }

    public static void SetSfxVolume(float v)
    {
        SfxVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(SfxPrefKey, SfxVolume);
    }

    // ---------- SFX ----------

    private void PlayInternal(SfxId id, AudioClip[] overrideClips, float pitch, float volume)
    {
        if (SfxVolume <= 0.001f)
            return;

        SoundBank.SfxEntry e = Bank.GetSfx(id);
        float now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(id, out float last) && now - last < e.cooldown)
            return;
        lastPlayed[id] = now;

        AudioClip clip;
        float baseVol = e.volume;
        if (overrideClips != null && overrideClips.Length > 0)
            clip = overrideClips[Random.Range(0, overrideClips.Length)];
        else if (e.clips != null && e.clips.Length > 0)
            clip = e.clips[Random.Range(0, e.clips.Length)];
        else
        {
            clip = GetProceduralSfx(id);
            baseVol *= Bank.proceduralSfxVolume;
        }

        if (clip == null)
            return;

        AudioSource s = NextSource();
        s.clip = clip;
        s.volume = baseVol * volume * SfxVolume;
        s.pitch = pitch * (1f + Random.Range(-e.pitchJitter, e.pitchJitter));
        s.Play();
    }

    private AudioSource NextSource()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            AudioSource s = pool[(poolCursor + i) % pool.Count];
            if (!s.isPlaying)
            {
                poolCursor = (poolCursor + i + 1) % pool.Count;
                return s;
            }
        }
        // Все заняты: перебиваем самый старый по кругу
        AudioSource oldest = pool[poolCursor];
        poolCursor = (poolCursor + 1) % pool.Count;
        return oldest;
    }

    private AudioClip GetProceduralSfx(SfxId id)
    {
        if (!proceduralSfx.TryGetValue(id, out AudioClip clip))
        {
            clip = ProceduralAudio.MakeSfx(id);
            proceduralSfx[id] = clip;
        }
        return clip;
    }

    // ---------- Музыка ----------

    private void PlayMusicInternal(MusicId id)
    {
        if (id == currentMusic)
            return;
        currentMusic = id;

        AudioClip clip = null;
        float vol = 1f;
        if (id != MusicId.None)
        {
            SoundBank.MusicEntry e = Bank.GetMusic(id);
            if (e != null && e.clip != null)
            {
                clip = e.clip;
                vol = e.volume;
            }
            else
            {
                if (!proceduralMusic.TryGetValue(id, out clip))
                {
                    clip = ProceduralAudio.MakeMusic(id);
                    proceduralMusic[id] = clip;
                }
                vol = Bank.proceduralMusicVolume;
            }
        }

        // Старый трек затухает, новый нарастает
        musicFrom = musicCurrent;
        musicFromStartVol = musicFrom != null ? musicFrom.volume : 0f;

        AudioSource next = musicCurrent == musicA ? musicB : musicA;
        next.Stop();
        musicCurrent = null;
        if (clip != null)
        {
            next.clip = clip;
            next.volume = 0f;
            next.Play();
            musicCurrent = next;
        }
        musicCurrentBase = vol;
        musicFadeT = 0f;
    }

    private void Update()
    {
        UpdateMusicFade();
        DetectUiClick();
    }

    private void UpdateMusicFade()
    {
        if (musicFadeT >= 1f)
            return;

        float fade = Mathf.Max(0.01f, Bank.musicFade);
        musicFadeT = Mathf.Min(1f, musicFadeT + Time.unscaledDeltaTime / fade);

        if (musicCurrent != null)
            musicCurrent.volume = musicCurrentBase * MusicVolume * musicFadeT;

        if (musicFrom != null)
        {
            musicFrom.volume = musicFromStartVol * (1f - musicFadeT);
            if (musicFadeT >= 1f)
            {
                musicFrom.Stop();
                musicFrom = null;
            }
        }
    }

    // ---------- Клик по кнопкам ----------
    // Один глобальный щелчок для любых Selectable (в том числе созданных в рантайме узлов книги).
    private void DetectUiClick()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame || EventSystem.current == null)
            return;

        var data = new PointerEventData(EventSystem.current) { position = mouse.position.ReadValue() };
        uiHits.Clear();
        EventSystem.current.RaycastAll(data, uiHits);
        if (uiHits.Count == 0)
            return;

        Selectable sel = uiHits[0].gameObject.GetComponentInParent<Selectable>();
        if (sel != null && sel.IsInteractable() && sel.enabled)
            Play(SfxId.UiClick);
    }
}
