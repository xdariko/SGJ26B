using UnityEngine;
using UnityEngine.InputSystem;

public class Main : MonoBehaviour
{
    private float impTimer;

    private void Awake()
    {
        G.main = this;
    }

    private void Start()
    {

    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        SetPause(!G.IsPaused);
    }

    private void SetPause(bool paused)
    {
        G.IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (G.ui != null)
            G.ui.SetPausePanel(paused);
    }

    public void ResumeGame()
    {
        SetPause(false);
    }
}