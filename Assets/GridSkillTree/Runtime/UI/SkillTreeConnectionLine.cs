using UnityEngine;
using UnityEngine.UI;

namespace GridSkillTree
{
    [RequireComponent(typeof(Image))]
    public class SkillTreeConnectionLine : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;

        private void Reset()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void SetPoints(Vector2 from, Vector2 to, float thickness)
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            Vector2 direction = to - from;
            float distance = direction.magnitude;

            rectTransform.anchoredPosition = from + direction * 0.5f;
            rectTransform.sizeDelta = new Vector2(distance, thickness);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }
    }
}
