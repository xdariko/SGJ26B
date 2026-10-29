using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Один фамильяр на полке: иконка, круг отдыха, имя.
public class FamiliarSlotView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Image cooldownFill;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color restingColor = new(0.5f, 0.5f, 0.55f);

    public Familiar Familiar { get; private set; }

    public void Bind(Familiar familiar)
    {
        Familiar = familiar;
        icon.sprite = familiar.def.sprite;
        nameText.text = familiar.def.displayName;
        Refresh();
    }

    public void Refresh()
    {
        if (Familiar == null)
            return;

        float ready = Familiar.Readiness01;
        cooldownFill.fillAmount = 1f - ready;
        icon.color = Familiar.IsReady || Familiar.IsBusy ? readyColor : restingColor;
    }

    public void PlayPounce()
    {
        icon.transform.DOKill(true);
        icon.transform.DOPunchScale(Vector3.one * 0.35f, 0.3f).SetLink(gameObject);
    }
}
