using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GridSkillTree
{
    public class CurrencyAmountView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private bool tintIconWithCurrencyColor = true;

        private CurrencyDefinition currency;
        private int amount;

        public CurrencyDefinition Currency => currency;
        public int Amount => amount;

        public void Init(CurrencyDefinition currencyDefinition)
        {
            SetData(currencyDefinition, 0);
        }

        public void SetData(CurrencyDefinition currencyDefinition, int amountValue)
        {
            currency = currencyDefinition;
            amount = amountValue;

            RefreshView();
        }

        public void Refresh(int amountValue)
        {
            amount = amountValue;

            RefreshView();
        }

        private void RefreshView()
        {
            if (currency == null)
            {
                if (nameText != null)
                    nameText.text = "Currency";

                if (amountText != null)
                    amountText.text = amount.ToString();

                if (iconImage != null)
                {
                    iconImage.enabled = false;
                    iconImage.sprite = null;
                }

                return;
            }

            if (nameText != null)
                nameText.text = string.IsNullOrWhiteSpace(currency.displayName)
                    ? currency.id
                    : currency.displayName;

            if (amountText != null)
                amountText.text = amount.ToString();

            if (iconImage != null)
            {
                iconImage.enabled = currency.icon != null;
                iconImage.sprite = currency.icon;

                if (currency.icon != null)
                    iconImage.color = tintIconWithCurrencyColor ? currency.color : Color.white;
            }
        }
    }
}