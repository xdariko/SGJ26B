using UnityEngine;

namespace GridSkillTree
{
    public class DemoCurrencyControls : MonoBehaviour
    {
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private CurrencyDefinition gold;
        [SerializeField] private CurrencyDefinition crystal;
        [SerializeField] private int goldAmount = 25;
        [SerializeField] private int crystalAmount = 5;

        public void AddGold()
        {
            if (wallet != null && gold != null)
                wallet.Add(gold, goldAmount);
        }

        public void AddCrystal()
        {
            if (wallet != null && crystal != null)
                wallet.Add(crystal, crystalAmount);
        }

        public void ResetWallet()
        {
            if (wallet != null)
                wallet.ResetToStartingBalances();
        }
    }
}
