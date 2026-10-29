using System;
using UnityEngine;

// Посылка на ленте: данные и состояние. Как она выглядит, решает ParcelVisual.
public class Parcel : MonoBehaviour
{
    [SerializeField] private ParcelVisual visual;

    public ParcelSpec Spec { get; private set; }
    public ParcelKind Kind => Spec.kind;
    public bool HasGhost { get; private set; }
    public bool IsResolved { get; private set; }
    public Belt Belt { get; private set; }
    public ParcelVisual Visual => visual;

    // Пройденный путь по ленте, в мировых единицах
    public float Distance { get; set; }

    // facing: поворот камеры ленты, чтобы спрайты стояли лицом к ней
    public void Setup(ParcelSpec spec, Belt belt, Quaternion facing)
    {
        Spec = spec;
        Belt = belt;
        HasGhost = spec.hasGhost;
        IsResolved = false;
        Distance = 0f;

        visual.Show(spec, G.config, facing);
    }

    public void Remove(Action onDone = null)
    {
        IsResolved = true;
        visual.PlayRemove(() => Finish(onDone));
    }

    public void Deliver(Action onDone = null)
    {
        IsResolved = true;
        visual.PlayDeliver(() => Finish(onDone));
    }

    public void ReleaseGhost()
    {
        HasGhost = false;
        visual.PlayGhostRelease();
    }

    // Убрать без анимации (конец смены)
    public void Discard()
    {
        IsResolved = true;
        Destroy(gameObject);
    }

    private void Finish(Action onDone)
    {
        onDone?.Invoke();
        Destroy(gameObject);
    }
}
