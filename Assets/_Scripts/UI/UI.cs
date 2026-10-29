using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject finalPanel;

    [Header("Pause Panel Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button exitButton;

    [Header("Final Panel Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button finalExitButton;


    private void Awake()
    {
        G.ui = this;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (finalPanel != null)
            finalPanel.SetActive(false);
    }

    private void Start()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (finalExitButton != null)
            finalExitButton.onClick.AddListener(OnExitClicked);
    }

    private void OnContinueClicked()
    {
        if (G.main != null)
            G.main.ResumeGame();
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueClicked);

        if (exitButton != null)
            exitButton.onClick.RemoveListener(OnExitClicked);

        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);

        if (finalExitButton != null)
            finalExitButton.onClick.RemoveListener(OnExitClicked);
    }

    private void OnExitClicked()
    {
        Application.Quit();
    }

    private void OnRestartClicked()
    {
        Time.timeScale = 1f;
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    internal void SetPausePanel(bool active)
    {
        if (pausePanel != null)
            pausePanel.SetActive(active);
    }

    public void SetFinalPanel(bool active)
    {
        if (finalPanel != null)
            finalPanel.SetActive(active);

        if (active)
            Time.timeScale = 0f;
    }

    public bool IsFinalPanelOpen()
    {
        return finalPanel != null && finalPanel.activeInHierarchy;
    }
}