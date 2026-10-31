using GridSkillTree;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ведьмина книга между сменами: дерево на весь экран и тонкая панель снизу (валюта + «Продолжить»).
public class BookView : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startLabel;

    [Header("Нижняя панель: валюта")]
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text ectoText;

    private void Start()
    {
        G.main.OnPhaseChanged += HandlePhase;
        if (G.wallet != null)
            G.wallet.OnCurrencyChanged += HandleCurrencyChanged;
        startButton.onClick.AddListener(OnStartClicked);
        HandlePhase(G.main.Phase);
    }

    private void OnDestroy()
    {
        if (G.main != null)
            G.main.OnPhaseChanged -= HandlePhase;
        if (G.wallet != null)
            G.wallet.OnCurrencyChanged -= HandleCurrencyChanged;
        startButton.onClick.RemoveListener(OnStartClicked);
    }

    private void HandlePhase(GamePhase phase)
    {
        bool open = phase == GamePhase.Book;
        root.SetActive(open);
        if (!open)
            return;

        startLabel.text = "Продолжить";
        RefreshCurrency();
    }

    private void HandleCurrencyChanged(CurrencyDefinition currency, int balance)
    {
        if (root.activeSelf)
            RefreshCurrency();
    }

    private void RefreshCurrency()
    {
        if (G.wallet == null)
            return;
        if (coinsText != null)
            coinsText.text = G.wallet.GetBalance(G.config.coin).ToString();
        if (ectoText != null)
            ectoText.text = G.wallet.GetBalance(G.config.ecto).ToString();
    }

    private void OnStartClicked()
    {
        G.main.BeginNight();
    }
}
