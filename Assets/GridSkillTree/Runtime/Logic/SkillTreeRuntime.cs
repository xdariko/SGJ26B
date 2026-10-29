using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridSkillTree
{
    public class SkillTreeRuntime : MonoBehaviour
    {
        private const string SaveKeyPrefix = "GridSkillTree.Progress.";

        [SerializeField] private SkillTreeData treeData;
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private bool loadSavedProgress = true;
        [SerializeField] private bool saveProgressOnBuy = true;

        public SkillTreeData TreeData => treeData;
        public CurrencyWallet Wallet => wallet;
        public SkillTreeProgress Progress { get; private set; }

        public event Action OnTreeChanged;
        public event Action<SkillNodeData, int, IReadOnlyList<SkillEffect>> OnNodePurchased;

        private readonly Dictionary<string, float> additiveEffects = new();
        private readonly HashSet<string> unlockedEffects = new();

        private string SaveKey
        {
            get
            {
                string treeKey = treeData != null && !string.IsNullOrWhiteSpace(treeData.treeId)
                    ? treeData.treeId
                    : "default";

                return SaveKeyPrefix + treeKey;
            }
        }

        private void Awake()
        {
            Progress = new SkillTreeProgress();

            if (loadSavedProgress)
                LoadProgress();

            RebuildEffects();
        }

        public int GetLevel(string nodeId)
        {
            return Progress == null ? 0 : Progress.GetLevel(nodeId);
        }

        public List<CurrencyAmount> GetCosts(SkillNodeData node)
        {
            if (node == null)
                return new List<CurrencyAmount>();

            int currentLevel = GetLevel(node.id);

            if (wallet != null)
                return wallet.CreateCostSnapshot(node.costs, currentLevel);

            List<CurrencyAmount> result = new();

            if (node.costs == null)
                return result;

            foreach (SkillCost cost in node.costs)
            {
                if (cost == null || cost.currency == null)
                    continue;

                int amount = cost.GetAmount(currentLevel);
                if (amount > 0)
                    result.Add(new CurrencyAmount { currency = cost.currency, amount = amount });
            }

            return result;
        }

        public bool IsUnlocked(SkillNodeData node)
        {
            if (node == null)
                return false;

            if (node.previousNodeIds == null || node.previousNodeIds.Count == 0)
                return true;

            foreach (string previousNodeId in node.previousNodeIds)
            {
                if (GetLevel(previousNodeId) <= 0)
                    return false;
            }

            return true;
        }

        public bool ShouldShowNode(SkillNodeData node)
        {
            if (node == null)
                return false;

            if (node.previousNodeIds == null || node.previousNodeIds.Count == 0)
                return true;

            return GetLevel(node.id) > 0 || IsUnlocked(node);
        }

        public bool CanBuy(SkillNodeData node)
        {
            if (node == null)
                return false;

            if (GetLevel(node.id) >= node.maxLevel)
                return false;

            if (!IsUnlocked(node))
                return false;

            List<CurrencyAmount> costs = GetCosts(node);

            if (costs.Count == 0)
                return true;

            return wallet != null && wallet.CanAfford(costs);
        }

        public bool Buy(SkillNodeData node)
        {
            if (!CanBuy(node))
                return false;

            List<CurrencyAmount> costs = GetCosts(node);

            if (costs.Count > 0 && wallet != null && !wallet.Spend(costs))
                return false;

            int currentLevel = GetLevel(node.id);
            int newLevel = currentLevel + 1;
            Progress.SetLevel(node.id, newLevel);

            RebuildEffects();

            if (saveProgressOnBuy)
                SaveProgress();

            OnTreeChanged?.Invoke();
            OnNodePurchased?.Invoke(node, newLevel, node.effects);

            return true;
        }

        public SkillNodeVisualState GetVisualState(SkillNodeData node)
        {
            if (node == null)
                return SkillNodeVisualState.Locked;

            if (GetLevel(node.id) >= node.maxLevel)
                return SkillNodeVisualState.Maxed;

            return CanBuy(node) ? SkillNodeVisualState.Available : SkillNodeVisualState.Locked;
        }

        public string GetStatusText(SkillNodeData node)
        {
            return GetVisualState(node) switch
            {
                SkillNodeVisualState.Locked => "Locked",
                SkillNodeVisualState.Available => "Available",
                SkillNodeVisualState.Maxed => "Maxed",
                _ => string.Empty
            };
        }

        public float GetEffectValue(string effectId)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                return 0f;

            return additiveEffects.TryGetValue(effectId, out float value) ? value : 0f;
        }

        public bool HasUnlock(string effectId)
        {
            return !string.IsNullOrWhiteSpace(effectId) && unlockedEffects.Contains(effectId);
        }

        public IReadOnlyDictionary<string, float> GetAllEffectValues()
        {
            return additiveEffects;
        }

        public void SaveProgress()
        {
            if (Progress == null)
                return;

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Progress));
            PlayerPrefs.Save();
        }

        public void LoadProgress()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
                return;

            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
                return;

            SkillTreeProgress loaded = JsonUtility.FromJson<SkillTreeProgress>(json);
            if (loaded == null)
                return;

            if (loaded.nodeProgress == null)
                loaded.nodeProgress = new List<SkillNodeProgress>();

            Progress = loaded;
        }

        public void ResetProgress(bool deleteSavedProgress = true)
        {
            if (deleteSavedProgress)
            {
                PlayerPrefs.DeleteKey(SaveKey);
                PlayerPrefs.Save();
            }

            Progress = new SkillTreeProgress();
            RebuildEffects();
            OnTreeChanged?.Invoke();
        }

        private void RebuildEffects()
        {
            additiveEffects.Clear();
            unlockedEffects.Clear();

            if (treeData == null || treeData.nodes == null || Progress == null)
                return;

            foreach (SkillNodeData node in treeData.nodes)
            {
                if (node == null || node.effects == null)
                    continue;

                int level = Progress.GetLevel(node.id);
                if (level <= 0)
                    continue;

                foreach (SkillEffect effect in node.effects)
                    ApplyEffect(effect, level);
            }
        }

        private void ApplyEffect(SkillEffect effect, int level)
        {
            if (effect == null || string.IsNullOrWhiteSpace(effect.id))
                return;

            if (effect.valueMode == SkillValueMode.Unlock)
            {
                unlockedEffects.Add(effect.id);
                return;
            }

            float value = effect.GetValue(level);

            if (effect.valueMode == SkillValueMode.Percent)
                value *= 100f;

            if (effect.valueMode == SkillValueMode.Set)
            {
                additiveEffects[effect.id] = value;
                return;
            }

            additiveEffects.TryGetValue(effect.id, out float current);
            additiveEffects[effect.id] = current + value;
        }
    }
}
