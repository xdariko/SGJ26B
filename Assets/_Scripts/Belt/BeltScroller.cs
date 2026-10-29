using UnityEngine;

// Визуально крутит полотно ленты. Спрайт ленты в режиме Tiled, сдвигаем его на шаг тайла по кругу.
public class BeltScroller : ManagedBehaviour
{
    [SerializeField] private Belt belt;
    [SerializeField] private SpriteRenderer surface;
    [Tooltip("Ширина одного тайла спрайта в мировых единицах")]
    [SerializeField] private float tileWidth = 2f;

    private Vector3 basePos;
    private float offset;

    private void Awake()
    {
        basePos = surface.transform.localPosition;
    }

    protected override void PausableUpdate()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float dt)
    {
        if (belt == null || !belt.IsRunning || G.stats == null)
            return;

        offset = Mathf.Repeat(offset + G.stats.beltSpeed * belt.SpeedMultiplier * dt, tileWidth);
        surface.transform.localPosition = basePos + Vector3.right * offset;
    }
}
