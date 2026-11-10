using System;
using System.Collections.Generic;
using GridSkillTree;
using UnityEngine;

public static class G
{
    public static Main main;
    public static UI ui;

    public static GameConfig config;
    public static GameStats stats;
    public static SkillTreeRuntime tree;
    public static CurrencyWallet wallet;
    public static ShiftController shift;
    public static FamiliarManager familiars;
    public static CameraFaults faults;

    public static bool IsPaused;
    public static bool IsMenuOpen;
    // Идёт пошаговое обучение: ленты, таймер и фамильяры стоят, но Time.timeScale не трогаем
    public static bool IsTutorial;

    public static bool IsFrozen => IsPaused || IsTutorial;
}


public class ManagedBehaviour : MonoBehaviour
{
    void Update()
    {
        if (!G.IsFrozen)
            PausableUpdate();
    }

    void FixedUpdate()
    {
        if (!G.IsFrozen)
            PausableFixedUpdate();
    }

    protected virtual void PausableUpdate() { }
    protected virtual void PausableFixedUpdate() { }
}
