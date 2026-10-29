using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridSkillTree
{
    public class CurrencyWallet : MonoBehaviour
    {
        private const string SaveKeyPrefix = "GridSkillTree.Wallet.";

        [SerializeField] private string walletId = "default";
        [SerializeField] private bool loadSavedBalances = true;
        [SerializeField] private bool saveOnChange = true;
        [SerializeField] private List<CurrencyAmount> startingBalances = new();

        private readonly Dictionary<string, int> balances = new();
        private readonly Dictionary<string, CurrencyDefinition> definitionsById = new();

        public event Action<CurrencyDefinition, int> OnCurrencyChanged;

        private string SaveKey => SaveKeyPrefix + walletId;

        private void Awake()
        {
            RegisterStartingCurrencies();
            ResetToStartingBalances();

            if (loadSavedBalances)
                LoadBalances();
        }

        public void RegisterCurrency(CurrencyDefinition currency)
        {
            if (currency == null || string.IsNullOrWhiteSpace(currency.id))
                return;

            definitionsById[currency.id] = currency;

            if (!balances.ContainsKey(currency.id))
                balances[currency.id] = 0;
        }

        public int GetBalance(CurrencyDefinition currency)
        {
            if (!IsValid(currency))
                return 0;

            return balances.TryGetValue(currency.id, out int value) ? value : 0;
        }

        public bool CanAfford(IReadOnlyList<CurrencyAmount> costs)
        {
            if (costs == null)
                return true;

            foreach (CurrencyAmount cost in costs)
            {
                if (cost == null || cost.currency == null || cost.amount <= 0)
                    continue;

                if (GetBalance(cost.currency) < cost.amount)
                    return false;
            }

            return true;
        }

        public bool Spend(IReadOnlyList<CurrencyAmount> costs)
        {
            if (!CanAfford(costs))
                return false;

            foreach (CurrencyAmount cost in costs)
            {
                if (cost == null || cost.currency == null || cost.amount <= 0)
                    continue;

                Add(cost.currency, -cost.amount);
            }

            return true;
        }

        public void Add(CurrencyDefinition currency, int amount)
        {
            if (!IsValid(currency) || amount == 0)
                return;

            RegisterCurrency(currency);

            balances[currency.id] = Mathf.Max(0, GetBalance(currency) + amount);
            OnCurrencyChanged?.Invoke(currency, balances[currency.id]);

            if (saveOnChange)
                SaveBalances();
        }

        public void ResetToStartingBalances()
        {
            balances.Clear();
            RegisterStartingCurrencies();

            foreach (CurrencyAmount entry in startingBalances)
            {
                if (entry == null || entry.currency == null)
                    continue;

                RegisterCurrency(entry.currency);
                balances[entry.currency.id] = Mathf.Max(0, entry.amount);
                OnCurrencyChanged?.Invoke(entry.currency, balances[entry.currency.id]);
            }

            if (saveOnChange)
                SaveBalances();
        }

        public List<CurrencyAmount> CreateCostSnapshot(IReadOnlyList<SkillCost> skillCosts, int currentLevel)
        {
            List<CurrencyAmount> result = new();

            if (skillCosts == null)
                return result;

            foreach (SkillCost cost in skillCosts)
            {
                if (cost == null || cost.currency == null)
                    continue;

                RegisterCurrency(cost.currency);

                int amount = cost.GetAmount(currentLevel);
                if (amount <= 0)
                    continue;

                result.Add(new CurrencyAmount
                {
                    currency = cost.currency,
                    amount = amount
                });
            }

            return result;
        }

        public void SaveBalances()
        {
            WalletSaveData data = new();

            foreach (KeyValuePair<string, int> pair in balances)
            {
                data.items.Add(new WalletSaveItem
                {
                    currencyId = pair.Key,
                    amount = pair.Value
                });
            }

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public void LoadBalances()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
                return;

            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
                return;

            WalletSaveData data = JsonUtility.FromJson<WalletSaveData>(json);
            if (data == null || data.items == null)
                return;

            foreach (WalletSaveItem item in data.items)
            {
                if (string.IsNullOrWhiteSpace(item.currencyId))
                    continue;

                balances[item.currencyId] = Mathf.Max(0, item.amount);

                definitionsById.TryGetValue(item.currencyId, out CurrencyDefinition definition);
                OnCurrencyChanged?.Invoke(definition, balances[item.currencyId]);
            }
        }

        public void DeleteSavedBalances()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        private void RegisterStartingCurrencies()
        {
            foreach (CurrencyAmount entry in startingBalances)
            {
                if (entry != null)
                    RegisterCurrency(entry.currency);
            }
        }

        private bool IsValid(CurrencyDefinition currency)
        {
            return currency != null && !string.IsNullOrWhiteSpace(currency.id);
        }

        [Serializable]
        private class WalletSaveItem
        {
            public string currencyId;
            public int amount;
        }

        [Serializable]
        private class WalletSaveData
        {
            public List<WalletSaveItem> items = new();
        }
    }
}
