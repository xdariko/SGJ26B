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

    public static bool IsPaused;
    public static bool IsMenuOpen;
}


public class ManagedBehaviour : MonoBehaviour
{
    void Update()
    {
        if (!G.IsPaused)
            PausableUpdate();
    }

    void FixedUpdate()
    {
        if (!G.IsPaused)
            PausableFixedUpdate();
    }

    protected virtual void PausableUpdate() { }
    protected virtual void PausableFixedUpdate() { }
}
