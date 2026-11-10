// Все звуки игры. Порядок не важен, в SoundBank ссылки по имени enum (новые добавлять в конец).
public enum SfxId
{
    UiClick,
    Snatch,          // игрок верно снял посылку
    WrongRemove,     // снял нормальную посылку
    Deliver,         // посылка доехала, всё хорошо
    Complaint,       // доехало то, что не должно
    GhostRelease,
    Ecto,            // получена эктоплазма
    FamiliarPounce,
    NightStart,      // колокол в начале смены
    ShiftEnd,        // полночь, конец смены
    ClockTick,       // последние секунды смены
    ReportLine,      // строка отчёта (машинка)
    Stamp,           // печать на отчёте
    Buy,             // покупка узла в книге
    PageTurn,        // книга, комикс
    Splash,          // логотип на старте
    BookOpen,
    UiHover,         // курсор навели на кнопку
    MonitorBreak,    // монитор потерял сигнал
    MonitorHit,      // удар кулаком по монитору
    MonitorFix       // монитор снова показывает
}

public enum MusicId
{
    None,
    Menu,
    Shift,
    Book,
    Final,
    Ending
}
