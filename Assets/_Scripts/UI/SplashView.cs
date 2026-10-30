using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Сплеш при старте: покадровая анимация логотипа (+ необязательная подпись), пока в фоне грузится меню.
// Клик / пробел / Escape пропускают.
public class SplashView : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 24f;
    [Tooltip("Необязательная подпись под логотипом. Проявляется и гаснет вместе с ним")]
    [SerializeField] private CanvasGroup caption;
    [SerializeField] private int captionShowFrame = 8;
    [SerializeField] private int captionHideFrame = 34;
    [SerializeField] private float captionFade = 0.3f;
    [SerializeField] private float holdAfter = 0.3f;

    private bool skip;

    private IEnumerator Start()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(Scenes.Menu);
        op.allowSceneActivation = false;

        if (caption != null)
            caption.alpha = 0f;
        float frameTime = 1f / Mathf.Max(1f, fps);

        for (int i = 0; i < frames.Length && !skip; i++)
        {
            image.sprite = frames[i];
            if (caption != null && i == captionShowFrame)
                caption.DOFade(1f, captionFade).SetLink(gameObject);
            if (caption != null && i == captionHideFrame)
                caption.DOFade(0f, captionFade).SetLink(gameObject);

            yield return new WaitForSecondsRealtime(frameTime);
        }

        if (!skip)
            yield return new WaitForSecondsRealtime(holdAfter);

        while (op.progress < 0.9f)
            yield return null;

        op.allowSceneActivation = true;
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        if ((kb != null && (kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
            || (mouse != null && mouse.leftButton.wasPressedThisFrame))
            skip = true;
    }
}
