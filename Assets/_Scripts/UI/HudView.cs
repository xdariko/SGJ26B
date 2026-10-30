using DG.Tweening;
using GridSkillTree;
using TMPro;
using UnityEngine;

// Верхняя плашка смены: ночь, таймкод CCTV, валюты, комбо. Только показывает, логику не трогает.
public class HudView : MonoBehaviour
{
    [SerializeField] private TMP_Text nightText;
    [SerializeField] private TMP_Text clockText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text ectoText;
    [SerializeField] private TMP_Text comboText;

    [Header("Таймкод: 23:00 → 00:00")]
    [SerializeField] private int startHour = 23;
    [SerializeField] private int clockMinutes = 60;

    [Header("Цвета")]
    [SerializeField] private Color comboIdleColor = new(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color comboActiveColor = new(1f, 0.6f, 0.1f);

    private void Start()
    {
        G.wallet.OnCurrencyChanged += HandleCurrencyChanged;
        G.shift.OnShiftStarted += HandleShiftStarted;
        G.shift.OnParcelResolved += HandleParcelResolved;

        RefreshCurrencies();
        HandleShiftStarted();
    }

    private void OnDestroy()
    {
        if (G.wallet != null) G.wallet.OnCurrencyChanged -= HandleCurrencyChanged;
        if (G.shift != null)
        {
            G.shift.OnShiftStarted -= HandleShiftStarted;
            G.shift.OnParcelResolved -= HandleParcelResolved;
        }
    }

    private void Update()
    {
        if (G.shift == null || clockText == null)
            return;

        int totalMinutes = Mathf.FloorToInt(G.shift.Progress01 * clockMinutes);
        int hour = (startHour + totalMinutes / 60) % 24;
        clockText.text = $"{hour:00}:{totalMinutes % 60:00}";
    }

    private void HandleShiftStarted()
    {
        if (nightText != null && G.shift.Score != null)
            nightText.text = G.stats.finalShift ? "ХЕЛЛОУИН" : $"НОЧЬ {G.shift.Night}";

        SetCombo(1f, 0);
    }

    private void HandleCurrencyChanged(CurrencyDefinition currency, int value)
    {
        RefreshCurrencies();
    }

    private void HandleParcelResolved(ParcelOutcome o)
    {
        ShiftScore score = G.shift.Score;
        SetCombo(score.ComboMult, score.streak);

        if (o.correct && o.comboMult > 1f && comboText != null)
            comboText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f).SetLink(comboText.gameObject);
    }

    private void SetCombo(float mult, int streak)
    {
        if (comboText == null)
            return;

        comboText.text = $"x{mult:0.#}  <size=60%>{streak}</size>";
        comboText.color = mult > 1f ? comboActiveColor : comboIdleColor;
    }

    private void RefreshCurrencies()
    {
        if (coinsText != null) coinsText.text = G.wallet.GetBalance(G.config.coin).ToString();
        if (ectoText != null) ectoText.text = G.wallet.GetBalance(G.config.ecto).ToString();
    }
}
