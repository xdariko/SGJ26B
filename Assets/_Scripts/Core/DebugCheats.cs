using UnityEngine;
using UnityEngine.InputSystem;

// Читы для тестов. В релизном билде не работают.
// F1 +100 монет, F2 +5 эктоплазмы, F3 марки вкл/выкл, F4 закончить смену, F5 новая игра,
// F6 финальная смена сразу (без покупки final_order), F7 сломать случайный монитор
public class DebugCheats : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null)
            return;

        if (kb.f1Key.wasPressedThisFrame)
            G.wallet.Add(G.config.coin, 100);

        if (kb.f2Key.wasPressedThisFrame)
            G.wallet.Add(G.config.ecto, 5);

        if (kb.f3Key.wasPressedThisFrame)
        {
            G.stats.camLevel = (G.stats.camLevel + 1) % 2;
            G.shift.RefreshVision();
            Debug.Log($"[Cheat] Уровень камеры: {G.stats.camLevel}");
        }

        if (kb.f4Key.wasPressedThisFrame)
            G.shift.EndShift();

        if (kb.f5Key.wasPressedThisFrame)
            G.main.NewGame();

        if (kb.f6Key.wasPressedThisFrame)
        {
            G.shift.EndShift();
            G.main.BeginNight(true);
        }

        if (kb.f7Key.wasPressedThisFrame && G.faults != null)
        {
            var belts = G.shift.Belts;
            for (int tries = 0; tries < 8; tries++)
            {
                Belt b = belts[Random.Range(0, belts.Count)];
                if (b.IsRunning && !G.faults.IsBroken(b))
                {
                    G.faults.Break(b);
                    break;
                }
            }
        }
    }
#endif
}
