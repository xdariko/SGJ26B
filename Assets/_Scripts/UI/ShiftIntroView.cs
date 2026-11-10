using DG.Tweening;
using TMPro;
using UnityEngine;

// Заставка в начале смены: «НОЧЬ 3» + записка (что нового, см. NightNotes). Клики не перехватывает.
public class ShiftIntroView : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text noteText;
    [SerializeField] private float fadeIn = 0.3f;
    [SerializeField] private float hold = 1.4f;
    [SerializeField] private float holdWithNote = 2.6f;
    [Tooltip("Добавка к показу за каждую строку записки после первой")]
    [SerializeField] private float holdPerExtraLine = 0.8f;
    [SerializeField] private float fadeOut = 0.6f;

    private void Start()
    {
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        G.shift.OnShiftStarted += Play;
        if (G.shift.Current == ShiftController.State.Running)
            Play();
    }

    private void OnDestroy()
    {
        if (G.shift != null)
            G.shift.OnShiftStarted -= Play;
    }

    private void Play()
    {
        bool final = G.stats.finalShift;
        string note = final ? G.config.finalNote : NightNotes.Take(G.config, G.stats, G.shift.Night);
        titleText.text = final ? G.config.finalTitle : $"НОЧЬ {G.shift.Night}";
        noteText.text = note ?? string.Empty;

        group.DOKill();
        group.alpha = 0f;
        DOTween.Sequence().SetTarget(group).SetLink(gameObject)
            .Append(group.DOFade(1f, fadeIn))
            .AppendInterval(string.IsNullOrEmpty(note) ? hold : holdWithNote + holdPerExtraLine * (note.Split('\n').Length - 1))
            .Append(group.DOFade(0f, fadeOut));
    }
}
