using DG.Tweening;
using UnityEngine;

// Фамильяр выскакивает на посылку прямо в мире ленты, поэтому его видно в мониторе.
public class FamiliarPounceView : MonoBehaviour
{
    [SerializeField] private float size = 0.9f;
    [SerializeField] private Vector2 startOffset = new(-1.2f, 1.2f);
    [SerializeField] private float landHeight = 0.25f;
    [SerializeField] private float fadeOut = 0.3f;
    [SerializeField] private int sortingOrder = 15;

    private void Start()
    {
        G.familiars.OnPounce += Play;
    }

    private void OnDestroy()
    {
        if (G.familiars != null)
            G.familiars.OnPounce -= Play;
    }

    private void Play(Familiar familiar, Parcel target)
    {
        Quaternion facing = target.Belt != null ? target.Belt.Cam.transform.rotation : Quaternion.identity;

        GameObject go = new($"Pounce_{familiar.def.name}");
        // Камеры лент видят только свои слои, без этого прыжок не попадёт в монитор
        go.layer = LayerMask.NameToLayer(CamVision.ParcelsLayer);
        // Дочерний к посылке: едет вместе с ней и исчезает вместе с ней
        go.transform.SetParent(target.transform, false);
        go.transform.rotation = facing;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = familiar.def.sprite;
        sr.sortingOrder = sortingOrder;

        Vector3 up = go.transform.up;
        Vector3 right = go.transform.right;
        Vector3 land = up * landHeight;
        go.transform.localPosition = right * startOffset.x + up * startOffset.y;
        go.transform.localScale = Vector3.one * size * 0.4f;

        float t = familiar.def.pounceTime;
        DOTween.Sequence().SetLink(go)
            .Append(go.transform.DOLocalMove(land, t).SetEase(Ease.OutQuad))
            .Join(go.transform.DOScale(size, t).SetEase(Ease.OutBack))
            .Append(sr.DOFade(0f, fadeOut))
            .OnComplete(() => Destroy(go));
    }
}
