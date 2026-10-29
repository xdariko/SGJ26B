using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GridSkillTree
{
    /// <summary>
    /// Demo-only script that shows how to connect purchased skill effects
    /// to game-specific systems. Replace Debug.Log calls with your own logic.
    /// </summary>
    public class DemoSkillEffectHandler : MonoBehaviour
    {
        [SerializeField] private SkillTreeRuntime runtime;

        [Header("Logging")]
        [SerializeField] private bool logPurchaseInfo = true;
        [SerializeField] private bool logEffectMappingHints = true;

        private void Awake()
        {
            if (runtime == null)
                runtime = FindFirstObjectByType<SkillTreeRuntime>();
        }

        private void OnEnable()
        {
            if (runtime == null)
                runtime = FindFirstObjectByType<SkillTreeRuntime>();

            if (runtime != null)
                runtime.OnNodePurchased += HandleNodePurchased;
        }

        private void OnDisable()
        {
            if (runtime != null)
                runtime.OnNodePurchased -= HandleNodePurchased;
        }

        private void HandleNodePurchased(
            SkillNodeData node,
            int newLevel,
            IReadOnlyList<SkillEffect> effects)
        {
            if (node == null)
                return;

            if (logPurchaseInfo)
                Debug.Log(BuildPurchaseLog(node, newLevel, effects));

            if (logEffectMappingHints)
                LogEffectMappingHints(effects, newLevel);
        }

        private string BuildPurchaseLog(
            SkillNodeData node,
            int newLevel,
            IReadOnlyList<SkillEffect> effects)
        {
            StringBuilder builder = new();

            string nodeName = string.IsNullOrWhiteSpace(node.title)
                ? node.id
                : node.title;

            builder.AppendLine($"[GridSkillTree] Purchased node: {nodeName} | Level {newLevel}/{node.maxLevel}");

            if (effects == null || effects.Count == 0)
            {
                builder.AppendLine("[GridSkillTree] This node has no effects.");
                return builder.ToString();
            }

            builder.AppendLine("[GridSkillTree] Effects:");

            foreach (SkillEffect effect in effects)
            {
                if (effect == null || string.IsNullOrWhiteSpace(effect.id))
                    continue;

                builder.Append("- ");
                builder.Append(effect.id);
                builder.Append(" | Mode: ");
                builder.Append(effect.valueMode);

                if (effect.valueMode == SkillValueMode.Unlock)
                {
                    builder.Append(" | Value: unlocked");
                }
                else
                {
                    float value = effect.GetValue(newLevel);

                    if (effect.valueMode == SkillValueMode.Percent)
                        value *= 100f;

                    builder.Append(" | Value: ");
                    builder.Append(value.ToString("0.##"));
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private void LogEffectMappingHints(IReadOnlyList<SkillEffect> effects, int level)
        {
            if (effects == null)
                return;

            foreach (SkillEffect effect in effects)
            {
                if (effect == null || string.IsNullOrWhiteSpace(effect.id))
                    continue;

                LogEffectMappingHint(effect, level);
            }
        }

        private void LogEffectMappingHint(SkillEffect effect, int level)
        {
            float value = effect.valueMode == SkillValueMode.Unlock
                ? 1f
                : effect.GetValue(level);

            if (effect.valueMode == SkillValueMode.Percent)
                value *= 100f;

            switch (effect.id)
            {
                case "damage":
                    Debug.Log($"[GridSkillTree] Example mapping: increase player or weapon damage by {value:0.##}.");
                    break;

                case "move_speed":
                    Debug.Log($"[GridSkillTree] Example mapping: increase player movement speed by {value:0.##}.");
                    break;

                case "max_health":
                    Debug.Log($"[GridSkillTree] Example mapping: increase player max health by {value:0.##}.");
                    break;

                case "crit_chance":
                    Debug.Log($"[GridSkillTree] Example mapping: increase critical chance by {value:0.##}.");
                    break;

                case "unlock_dash":
                    Debug.Log("[GridSkillTree] Example mapping: unlock dash ability in your player controller.");
                    break;

                case "currency_gain":
                    Debug.Log($"[GridSkillTree] Example mapping: increase currency gain by {value:0.##}.");
                    break;

                default:
                    Debug.Log($"[GridSkillTree] Custom effect '{effect.id}' purchased. Add your own handling for this ID in your game code.");
                    break;
            }
        }
    }
}