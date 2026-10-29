using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioSettingsUI : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private const string MusicPrefKey = "MusicVol";
    private const string SfxPrefKey = "SFXVol";

    private void Start()
    {
        InitializeSliders();
    }

    private void InitializeSliders()
    {
        musicSlider.onValueChanged.AddListener(SetMusicVol);
        sfxSlider.onValueChanged.AddListener(SetSFXVol);

        if (PlayerPrefs.HasKey("MusicVol"))
        {
            musicSlider.value = PlayerPrefs.GetFloat("MusicVol");
        }
        else
        {
            musicSlider.value = 1f;
        }

        if (PlayerPrefs.HasKey("SFXVol"))
        {
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVol");
        }
        else
        {
            sfxSlider.value = 1f;
        }

        float savedMusic = PlayerPrefs.GetFloat(MusicPrefKey, 1f);
        float savedSfx = PlayerPrefs.GetFloat(SfxPrefKey, 1f);
    }

    public void SetMusicVol(float value)
    {
        audioMixer.SetFloat("MusicVol", Mathf.Log10(musicSlider.value) * 20);
        PlayerPrefs.SetFloat("MusicVol", musicSlider.value);
    }

    public void SetSFXVol(float value)
    {
        audioMixer.SetFloat("SFXVol", Mathf.Log10(sfxSlider.value) * 20);
        PlayerPrefs.SetFloat("SFXVol", sfxSlider.value);
    }

    private void OnDestroy()
    {
        musicSlider.onValueChanged.RemoveListener(SetMusicVol);
        sfxSlider.onValueChanged.RemoveListener(SetSFXVol);
    }
}