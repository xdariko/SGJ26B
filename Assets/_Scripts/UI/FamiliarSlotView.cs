using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Один фамильяр на подушке. Готов — дышит; прыгнул на посылку — спрыгивает с подушки и возвращается;
// отдыхает — сидит притихший (темнее) и постепенно светлеет к готовности.
public class FamiliarSlotView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color restingColor = new(0.45f, 0.42f, 0.55f);

    [Header("Анимация")]
    [SerializeField] private float breathe = 0.035f;
    [SerializeField] private float breatheTime = 1.1f;
    [SerializeField] private float leaveHeight = 70f;
    [SerializeField] private float leaveTime = 0.22f;
    [SerializeField] private float returnTime = 0.3f;

    public Familiar Familiar { get; private set; }

    private RectTransform body;
    private bool away;
    private Tween idle;

    public void Bind(Familiar familiar)
    {
        Familiar = familiar;
        body = icon.rectTransform;
        icon.sprite = familiar.def.sprite;
        icon.preserveAspect = true;
        away = false;
        ResetPose();
        StartIdle();
        Refresh();
    }

    public void Refresh()
    {
        if (Familiar == null)
            return;

        // Прыжок закончился — фамильяр возвращается на подушку
        if (away && !Familiar.IsBusy)
            PlayReturn();

        if (!away)
        {
            Color c = Familiar.IsReady ? readyColor : Color.Lerp(restingColor, readyColor, Familiar.Readiness01 * 0.6f);
            c.a = icon.color.a;
            icon.color = c;
        }
    }

    public void PlayPounce()
    {
        away = true;
        KillTweens();
        DOTween.Sequence().SetLink(gameObject).SetTarget(this)
            .Append(body.DOScale(new Vector3(1.15f, 0.8f, 1f), 0.07f))
            .Append(body.DOScale(new Vector3(0.85f, 1.2f, 1f), leaveTime))
            .Join(body.DOAnchorPosY(leaveHeight, leaveTime).SetEase(Ease.OutQuad))
            .Join(icon.DOFade(0f, leaveTime).SetEase(Ease.InQuad));
    }

    private void PlayReturn()
    {
        away = false;
        KillTweens();
        body.anchoredPosition = new Vector2(0f, leaveHeight);
        Color c = restingColor;
        c.a = 0f;
        icon.color = c;

        DOTween.Sequence().SetLink(gameObject).SetTarget(this)
            .Append(body.DOAnchorPosY(0f, returnTime).SetEase(Ease.InQuad))
            .Join(icon.DOFade(1f, returnTime * 0.6f))
            .Append(body.DOScale(new Vector3(1.2f, 0.8f, 1f), 0.07f))
            .Append(body.DOScale(Vector3.one, 0.18f).SetEase(Ease.OutBack))
            .OnComplete(StartIdle);
    }

    private void StartIdle()
    {
        idle?.Kill();
        body.localScale = Vector3.one;
        idle = body.DOScaleY(1f + breathe, breatheTime).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
            .SetDelay(Random.Range(0f, breatheTime)).SetLink(gameObject);
    }

    private void ResetPose()
    {
        KillTweens();
        body.anchoredPosition = Vector2.zero;
        body.localScale = Vector3.one;
        icon.color = readyColor;
    }

    private void KillTweens()
    {
        idle?.Kill();
        idle = null;
        DOTween.Kill(this);
        body.DOKill();
        icon.DOKill();
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }
}
