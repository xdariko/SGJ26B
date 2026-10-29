using UnityEngine;

namespace GridSkillTree
{
    [CreateAssetMenu(menuName = "Grid Skill Tree/Currency Definition")]
    public class CurrencyDefinition : ScriptableObject
    {
        [Tooltip("Stable id used for saving. Do not rename it after release.")]
        public string id = "currency";

        public string displayName = "Currency";
        public Sprite icon;
        public Color color = Color.white;
    }
}
