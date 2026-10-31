using UnityEngine;
using UnityEngine.UI;

// Ползунки громкости. Громкость держит AudioHub (без AudioMixer: в WebGL он не работает).
public class AudioSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        musicSlider.SetValueWithoutNotify(AudioHub.MusicVolume);
        sfxSlider.SetValueWithoutNotify(AudioHub.SfxVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVol);
        sfxSlider.onValueChanged.AddListener(SetSFXVol);
    }

    public void SetMusicVol(float value)
    {
        AudioHub.SetMusicVolume(value);
    }

    public void SetSFXVol(float value)
    {
        AudioHub.SetSfxVolume(value);
        AudioHub.Play(SfxId.UiClick);
    }

    private void OnDestroy()
    {
        musicSlider.onValueChanged.RemoveListener(SetMusicVol);
        sfxSlider.onValueChanged.RemoveListener(SetSFXVol);
    }
}
