using System.Collections.Generic;
using UnityEngine;

namespace GridSkillTree
{
    public class CurrencyPanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private CurrencyAmountView itemPrefab;

        [Header("Currencies")]
        [Tooltip("Currencies to show in this panel. The wallet will register them automatically.")]
        [SerializeField] private List<CurrencyDefinition> currencies = new();
        [SerializeField] private bool hideZeroBalances;

        private readonly Dictionary<string, CurrencyAmountView> viewsByCurrencyId = new();

        private void Awake()
        {
            Build();
        }

        private void OnEnable()
        {
            if (wallet != null)
                wallet.OnCurrencyChanged += HandleCurrencyChanged;

            RefreshAll();
        }

        private void OnDisable()
        {
            if (wallet != null)
                wallet.OnCurrencyChanged -= HandleCurrencyChanged;
        }

        public void Build()
        {
            Clear();

            if (wallet == null || contentRoot == null || itemPrefab == null)
            {
                Debug.LogWarning("CurrencyPanelUI is missing references.", this);
                return;
            }

            foreach (CurrencyDefinition currency in currencies)
            {
                if (currency == null || string.IsNullOrWhiteSpace(currency.id))
                    continue;

                wallet.RegisterCurrency(currency);

                if (viewsByCurrencyId.ContainsKey(currency.id))
                    continue;

                CurrencyAmountView view = Instantiate(itemPrefab, contentRoot);
                view.Init(currency);
                viewsByCurrencyId.Add(currency.id, view);
            }

            RefreshAll();
        }

        public void RefreshAll()
        {
            if (wallet == null)
                return;

            foreach (CurrencyDefinition currency in currencies)
            {
                if (currency == null || string.IsNullOrWhiteSpace(currency.id))
                    continue;

                RefreshCurrency(currency, wallet.GetBalance(currency));
            }
        }

        private void HandleCurrencyChanged(CurrencyDefinition currency, int amount)
        {
            if (currency == null)
            {
                RefreshAll();
                return;
            }

            RefreshCurrency(currency, amount);
        }

        private void RefreshCurrency(CurrencyDefinition currency, int amount)
        {
            if (currency == null || string.IsNullOrWhiteSpace(currency.id))
                return;

            if (!viewsByCurrencyId.TryGetValue(currency.id, out CurrencyAmountView view) || view == null)
                return;

            view.Refresh(amount);
            view.gameObject.SetActive(!hideZeroBalances || amount > 0);
        }

        private void Clear()
        {
            viewsByCurrencyId.Clear();

            if (contentRoot == null)
                return;

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = contentRoot.GetChild(i).gameObject;

                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }
    }
}
