using UnityEngine;

// Концовка после финальной смены: финальный комикс → главное меню.
// Игра пройдена, поэтому перед выходом в меню прогресс сбрасывается.
public class EndingView : MonoBehaviour
{
    [SerializeField] private ComicCutsceneUI finalComic;

    private bool finished;

    private void Start()
    {
        G.main.OnPhaseChanged += HandlePhase;
    }

    private void OnDestroy()
    {
        if (G.main != null)
            G.main.OnPhaseChanged -= HandlePhase;
    }

    private void HandlePhase(GamePhase phase)
    {
        if (phase != GamePhase.Ending)
            return;

        if (finalComic != null)
            finalComic.Play(Finish);
        else
            Finish();
    }

    private void Finish()
    {
        if (finished)
            return;

        finished = true;
        SaveData.ResetAll();
        Scenes.Load(Scenes.Menu);
    }
}
