using System;
using System.Collections.Generic;
using UnityEngine;

public static class G
{
    public static Main main;
    public static UI ui;

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
