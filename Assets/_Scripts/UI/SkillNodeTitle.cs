using GridSkillTree;
using TMPro;
using UnityEngine;

// Подпись с названием под узлом книги. Висит на нашей копии префаба узла, саму тулзу не трогает.
[RequireComponent(typeof(SkillNodeButton))]
public class SkillNodeTitle : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private SkillNodeButton button;
    private SkillNodeData shown;

    private void Awake()
    {
        button = GetComponent<SkillNodeButton>();
    }

    private void LateUpdate()
    {
        // Init у кнопки вызывается после Instantiate, поэтому подхватываем узел, как только он появился
        if (button.Node == shown)
            return;

        shown = button.Node;
        label.text = shown != null ? shown.title : string.Empty;
    }
}
