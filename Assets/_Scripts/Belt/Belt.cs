using System;
using System.Collections.Generic;
using UnityEngine;

// Одна лента: спавнит посылки в start, везёт к end. Доехала до конца = доставлена.
public class Belt : ManagedBehaviour
{
    [SerializeField] private Transform start;
    [SerializeField] private Transform end;
    [SerializeField] private Transform parcelRoot;
    [SerializeField] private Parcel parcelPrefab;
    [Tooltip("Камера-тыква, которая смотрит на эту ленту")]
    [SerializeField] private BeltCamera beltCamera;

    private readonly List<Parcel> parcels = new();
    private float spawnTimer;
    private int night;
    // Своё время ленты и сдвиг шума, чтобы волны на разных лентах не совпадали
    private float clock;
    private float waveSeed;
    private Parcel lastSpawned;

    public Addressee Addressee { get; private set; }
    public BeltCamera Cam => beltCamera;
    public bool IsRunning { get; private set; }
    public float SpeedMultiplier { get; set; } = 1f;
    public IReadOnlyList<Parcel> Parcels => parcels;
    public float Length => Vector3.Distance(start.position, end.position);

    public event Action<Parcel> OnParcelReachedEnd;

    public void Begin(Addressee addressee, int nightNumber)
    {
        Addressee = addressee;
        night = nightNumber;
        spawnTimer = 0f;
        clock = 0f;
        waveSeed = UnityEngine.Random.Range(0f, 1000f);
        lastSpawned = null;
        SpeedMultiplier = 1f;
        IsRunning = true;
    }

    public void Stop()
    {
        IsRunning = false;

        foreach (Parcel p in parcels)
            if (p != null && !p.IsResolved)
                p.Discard();

        parcels.Clear();
    }

    protected override void PausableUpdate()
    {
        Tick(Time.deltaTime);
    }

    // Шаг ленты. Отдельно от Update, чтобы можно было прогонять смену с фиксированным dt (тесты)
    public void Tick(float dt)
    {
        if (!IsRunning)
            return;

        clock += dt;
        // Замедленная (сломанный монитор) лента и подаёт посылки реже, иначе они слипнутся
        spawnTimer -= dt * SpeedMultiplier;
        // Таймер вышел, но прошлая посылка ещё у самого старта: ждём, пока отъедет, чтобы не слиплись
        if (spawnTimer <= 0f && HasRoomAtStart())
        {
            Spawn();
            spawnTimer = Mathf.Max(0f, spawnTimer) + NextInterval();
        }

        Vector3 from = start.position;
        Vector3 to = end.position;
        float length = Vector3.Distance(from, to);
        float step = G.stats.beltSpeed * SpeedMultiplier * dt;

        for (int i = parcels.Count - 1; i >= 0; i--)
        {
            Parcel p = parcels[i];
            if (p == null || p.IsResolved)
            {
                parcels.RemoveAt(i);
                continue;
            }

            p.Distance += step;
            p.transform.position = Vector3.Lerp(from, to, p.Distance / length) + p.LaneOffset;

            if (p.Distance >= length)
            {
                parcels.RemoveAt(i);
                OnParcelReachedEnd?.Invoke(p);
            }
        }
    }

    private void Spawn()
    {
        ParcelSpec spec = ParcelFactory.Roll(night, Addressee, G.config, G.stats);
        if (spec.shape == null)
            return;

        Parcel p = Instantiate(parcelPrefab, start.position, Quaternion.identity, parcelRoot != null ? parcelRoot : transform);
        p.Setup(spec, this, beltCamera.transform.rotation);
        // Пивот спрайта внизу: опускаем посылку, чтобы по середине полотна ехала её точка beltAnchor
        // (обычно центр, у высоких бутылок нижний край)
        Bounds b = p.Visual.ClickBounds;
        float anchor = spec.shape != null ? spec.shape.beltAnchor : 0.5f;
        p.LaneOffset = Vector3.down * (b.min.y + b.size.y * anchor - p.transform.position.y);
        p.transform.position += p.LaneOffset;
        parcels.Add(p);
        lastSpawned = p;
    }

    private bool HasRoomAtStart()
    {
        if (lastSpawned == null || lastSpawned.IsResolved)
            return true;

        return lastSpawned.Distance >= G.config.minParcelSpacing;
    }

    // Интервал до следующей посылки: медленные волны (то густо, то пусто) × случайный разброс,
    // иногда пачка вплотную. Делим на среднее, чтобы общий темп остался spawnInterval
    private float NextInterval()
    {
        GameConfig cfg = G.config;
        float wave = 1f + (Mathf.PerlinNoise(waveSeed, clock / cfg.spawnWavePeriod) * 2f - 1f) * cfg.spawnWaveStrength;
        float jitter = 1f + UnityEngine.Random.Range(-cfg.spawnJitter, cfg.spawnJitter);

        float clusterMean = (cfg.spawnClusterGap.x + cfg.spawnClusterGap.y) * 0.5f;
        float mean = 1f - cfg.spawnClusterChance + cfg.spawnClusterChance * clusterMean;
        float k = UnityEngine.Random.value < cfg.spawnClusterChance
            ? UnityEngine.Random.Range(cfg.spawnClusterGap.x, cfg.spawnClusterGap.y)
            : wave * jitter;

        return G.stats.spawnInterval * k / Mathf.Max(0.1f, mean);
    }
}
