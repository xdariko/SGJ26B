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
    [SerializeField, Range(0f, 0.5f)] private float spawnJitter = 0.2f;

    private readonly List<Parcel> parcels = new();
    private float spawnTimer;
    private int night;

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

        spawnTimer -= dt;
        if (spawnTimer <= 0f)
        {
            Spawn();
            spawnTimer += G.stats.spawnInterval * (1f + UnityEngine.Random.Range(-spawnJitter, spawnJitter));
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
            p.transform.position = Vector3.Lerp(from, to, p.Distance / length);

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
        parcels.Add(p);
    }
}
