using System;
using System.Collections.Generic;
using UnityEngine;

// Процедурные заглушки звуков и музыки: синтез в память, без файлов. Работает и в WebGL.
// Как только в SoundBank появляется настоящий клип, заглушка больше не используется.
public static class ProceduralAudio
{
    private const int SfxRate = 44100;
    private const int MusicRate = 22050;
    private const float TwoPi = Mathf.PI * 2f;

    private static System.Random rng = new(1031);

    // ============================== SFX ==============================

    public static AudioClip MakeSfx(SfxId id)
    {
        rng = new System.Random(1031 + (int)id);
        // Настроение — уютный хеллоуинский lo-fi: мягкие щипки (калимба/шкатулка), дерево, бумага, колокола.
        // Мелодические звуки в ля-минорной пентатонике, чтобы не спорить с музыкой. В конце всё «утепляем» фильтром.
        (float[] d, float warmth) = id switch
        {
            SfxId.UiClick => (Click(), 4500f),
            SfxId.UiHover => (Hover(), 6000f),
            SfxId.Snatch => (Snatch(), 7000f),
            SfxId.WrongRemove => (Bonk(), 2600f),
            SfxId.Deliver => (Ding(), 7000f),
            SfxId.Complaint => (Wah(), 1800f),
            SfxId.GhostRelease => (Ghost(), 4000f),
            SfxId.Ecto => (Sparkle(), 8000f),
            SfxId.FamiliarPounce => (Whoosh(0.22f, 500f, 2600f, 0.6f), 5000f),
            SfxId.NightStart => (Bell(220f, 2.2f, 1), 5000f),
            SfxId.ShiftEnd => (Bell(164.8f, 1.0f, 3), 5000f),
            SfxId.ClockTick => (Tick(), 5000f),
            SfxId.ReportLine => (Typewriter(), 6000f),
            SfxId.Stamp => (Stamp(), 3500f),
            SfxId.Buy => (Chime(), 8000f),
            SfxId.PageTurn => (Whoosh(0.3f, 400f, 2200f, 0.5f), 5000f),
            SfxId.BookOpen => (Whoosh(0.45f, 250f, 1500f, 0.45f), 4500f),
            SfxId.Splash => (SplashSting(), 6000f),
            _ => (Click(), 4500f)
        };
        Warm(d, SfxRate, warmth);
        Normalize(d, 0.9f);
        return ToClip("sfx_" + id, d, SfxRate);
    }

    // Ноты ля-минорной пентатоники
    private const float A4 = 440f, C5 = 523.25f, D5 = 587.33f, E5 = 659.25f, G5 = 783.99f,
        A5 = 880f, C6 = 1046.5f, D6 = 1174.66f, E6 = 1318.51f, A6 = 1760f;

    // Однополюсный фильтр низких частот + мягкое насыщение: убирает «цифровой» визг, звук теплее
    private static void Warm(float[] d, int rate, float cutoff)
    {
        float a = 1f - Mathf.Exp(-TwoPi * cutoff / rate);
        float lp = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            lp += a * (d[i] - lp);
            d[i] = (float)Math.Tanh(lp * 1.4f) / 1.4f;
        }
    }

    // Щипок калимбы/шкатулки: основной тон + негармонический обертон, который гаснет быстрее, + тихий «тик» удара
    private static void Pluck(float[] d, float start, float f, float amp, float decay = 9f)
    {
        float dur = Mathf.Min(1.2f, 6f / decay + 0.05f);
        AddTone(d, SfxRate, start, dur, f, f, amp, 0, 0.002f, decay);
        AddTone(d, SfxRate, start, dur * 0.5f, f * 2.76f, f * 2.76f, amp * 0.25f, 0, 0.001f, decay * 3f);
        AddNoise(d, SfxRate, start, 0.008f, 4000f, 2500f, amp * 0.12f, 0.0005f, 300f);
    }

    private static float[] Buf(float seconds) => new float[Mathf.CeilToInt(seconds * SfxRate)];

    private static float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);

    // Синус/треугольник/пила/квадрат по фазе 0..1
    private static float Wave(int type, float phase)
    {
        phase -= Mathf.Floor(phase);
        return type switch
        {
            0 => Mathf.Sin(phase * TwoPi),
            1 => 1f - 4f * Mathf.Abs(phase - 0.5f),
            2 => 2f * phase - 1f,
            _ => phase < 0.5f ? 1f : -1f
        };
    }

    // Тон со скольжением частоты и экспоненциальным затуханием
    private static void AddTone(float[] d, int rate, float start, float dur, float f0, float f1, float amp,
        int wave = 0, float attack = 0.003f, float decay = 6f, float vibHz = 0f, float vibDepth = 0f)
    {
        int s0 = (int)(start * rate);
        int n = (int)(dur * rate);
        double phase = 0;
        for (int i = 0; i < n && s0 + i < d.Length; i++)
        {
            float t = i / (float)rate;
            float k = t / dur;
            float f = Mathf.Lerp(f0, f1, k);
            if (vibHz > 0f)
                f *= 1f + vibDepth * Mathf.Sin(TwoPi * vibHz * t);
            phase += f / rate;
            float env = Mathf.Min(1f, t / attack) * Mathf.Exp(-decay * t);
            // Короткий хвост, чтобы не щёлкало в конце
            env *= Mathf.Clamp01((dur - t) / 0.01f);
            if (s0 + i >= 0)
                d[s0 + i] += Wave(wave, (float)phase) * env * amp;
        }
    }

    // Шум через однополюсный фильтр с плавающей частотой среза
    private static void AddNoise(float[] d, int rate, float start, float dur, float cut0, float cut1, float amp,
        float attack = 0.002f, float decay = 10f, bool highpass = false)
    {
        int s0 = (int)(start * rate);
        int n = (int)(dur * rate);
        float lp = 0f;
        for (int i = 0; i < n && s0 + i < d.Length; i++)
        {
            float t = i / (float)rate;
            float cut = Mathf.Lerp(cut0, cut1, t / dur);
            float a = 1f - Mathf.Exp(-TwoPi * cut / rate);
            float x = Noise();
            lp += a * (x - lp);
            float y = highpass ? x - lp : lp;
            float env = Mathf.Min(1f, t / attack) * Mathf.Exp(-decay * t) * Mathf.Clamp01((dur - t) / 0.01f);
            d[s0 + i] += y * env * amp;
        }
    }

    // Простое эхо
    private static void Echo(float[] d, int rate, float delay, float feedback, int taps)
    {
        int ds = (int)(delay * rate);
        for (int tap = 0; tap < taps; tap++)
            for (int i = d.Length - 1; i >= ds; i--)
                d[i] += d[i - ds] * feedback;
    }

    // Деревянный «ток» — как кнопка старого радиоприёмника
    private static float[] Click()
    {
        float[] d = Buf(0.08f);
        AddTone(d, SfxRate, 0, 0.06f, 720f, 520f, 0.6f, 1, 0.001f, 70f);
        AddTone(d, SfxRate, 0, 0.03f, 1450f, 1300f, 0.15f, 0, 0.001f, 120f);
        AddNoise(d, SfxRate, 0, 0.012f, 2500f, 1500f, 0.2f, 0.0005f, 250f);
        return d;
    }

    // Наведение: еле слышная стеклянная капля, не утомляет при частом повторе
    private static float[] Hover()
    {
        float[] d = Buf(0.16f);
        AddTone(d, SfxRate, 0, 0.14f, E6, E6 * 1.01f, 0.4f, 0, 0.003f, 32f);
        AddTone(d, SfxRate, 0, 0.06f, E6 * 2f, E6 * 2f, 0.06f, 0, 0.002f, 60f);
        return d;
    }

    // Верно сняла посылку: два щипка калимбы вверх (+ тон растёт с комбо)
    private static float[] Snatch()
    {
        float[] d = Buf(0.5f);
        AddTone(d, SfxRate, 0, 0.05f, 300f, 700f, 0.25f, 0, 0.001f, 40f); // мягкий «поп»
        Pluck(d, 0.0f, A5, 0.5f, 14f);
        Pluck(d, 0.06f, E6, 0.4f, 14f);
        return d;
    }

    // Сняла не то: глухие два шага вниз, без резкости
    private static float[] Bonk()
    {
        float[] d = Buf(0.45f);
        AddTone(d, SfxRate, 0, 0.18f, 330f, 315f, 0.55f, 1, 0.003f, 11f);
        AddTone(d, SfxRate, 0.14f, 0.28f, 262f, 240f, 0.55f, 1, 0.003f, 9f);
        AddNoise(d, SfxRate, 0, 0.02f, 900f, 500f, 0.2f, 0.001f, 120f);
        return d;
    }

    // Доставлено: одна тихая нота шкатулки (звучит часто — должна быть ненавязчивой)
    private static float[] Ding()
    {
        float[] d = Buf(0.6f);
        Pluck(d, 0, C6, 0.5f, 8f);
        return d;
    }

    // Жалоба: «уа-уа» приглушённой трубы вниз + лёгкие помехи монитора
    private static float[] Wah()
    {
        float[] d = Buf(0.8f);
        AddTone(d, SfxRate, 0, 0.32f, 233f, 220f, 0.5f, 2, 0.02f, 3f, 6f, 0.02f);
        AddTone(d, SfxRate, 0, 0.32f, 234.5f, 221f, 0.3f, 1, 0.02f, 3f);
        AddTone(d, SfxRate, 0.3f, 0.48f, 196f, 165f, 0.5f, 2, 0.02f, 3.5f, 6f, 0.03f);
        AddTone(d, SfxRate, 0.3f, 0.48f, 197f, 166f, 0.3f, 1, 0.02f, 3.5f);
        AddNoise(d, SfxRate, 0, 0.7f, 2200f, 1200f, 0.06f, 0.01f, 3f);
        return d;
    }

    // Призрак улетел: воздушное «у-у» вверх с эхом
    private static float[] Ghost()
    {
        float[] d = Buf(1.5f);
        AddTone(d, SfxRate, 0, 1.0f, A4, A5, 0.4f, 0, 0.15f, 2.2f, 5f, 0.025f);
        AddTone(d, SfxRate, 0.05f, 0.9f, E5, E6, 0.12f, 0, 0.18f, 2.8f, 5.5f, 0.025f);
        AddNoise(d, SfxRate, 0, 0.9f, 600f, 2400f, 0.1f, 0.25f, 2.5f);
        Echo(d, SfxRate, 0.16f, 0.35f, 2);
        return d;
    }

    // Эктоплазма: короткое волшебное арпеджио челесты
    private static float[] Sparkle()
    {
        float[] d = Buf(0.9f);
        float[] notes = { A5, C6, E6, A6 };
        for (int i = 0; i < notes.Length; i++)
            Pluck(d, i * 0.055f, notes[i], 0.35f, 10f);
        AddNoise(d, SfxRate, 0, 0.35f, 6000f, 8000f, 0.03f, 0.02f, 7f, true);
        Echo(d, SfxRate, 0.12f, 0.3f, 2);
        return d;
    }

    private static float[] Whoosh(float dur, float c0, float c1, float amp = 0.8f)
    {
        float[] d = Buf(dur + 0.02f);
        int n = (int)(dur * SfxRate);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            // Вверх и обратно вниз
            float cut = Mathf.Lerp(c0, c1, Mathf.Sin(t * Mathf.PI));
            float a = 1f - Mathf.Exp(-TwoPi * cut / SfxRate);
            lp += a * (Noise() - lp);
            float env = Mathf.Sin(t * Mathf.PI);
            d[i] = lp * env * env * amp;
        }
        return d;
    }

    // Колокол: негармонические обертоны с долгим затуханием, strikes = сколько ударов
    private static float[] Bell(float f, float spacing, int strikes)
    {
        float ring = 2.6f;
        float[] d = Buf(spacing * (strikes - 1) + ring);
        float[] ratios = { 0.5f, 1f, 1.19f, 1.56f, 2f, 2.74f, 3.76f };
        float[] amps = { 0.35f, 0.5f, 0.25f, 0.2f, 0.25f, 0.12f, 0.08f };
        float[] decays = { 1.2f, 1.6f, 2.2f, 2.6f, 3f, 4f, 5f };
        for (int s = 0; s < strikes; s++)
            for (int i = 0; i < ratios.Length; i++)
                AddTone(d, SfxRate, s * spacing, ring, f * ratios[i], f * ratios[i] * 0.999f, amps[i], 0, 0.002f, decays[i]);
        return d;
    }

    // Тиканье старых часов: деревянное, без писка
    private static float[] Tick()
    {
        float[] d = Buf(0.1f);
        AddTone(d, SfxRate, 0, 0.07f, 1150f, 980f, 0.45f, 1, 0.0005f, 75f);
        AddTone(d, SfxRate, 0, 0.05f, 380f, 300f, 0.25f, 0, 0.0005f, 80f);
        AddNoise(d, SfxRate, 0, 0.015f, 3000f, 1500f, 0.2f, 0.0005f, 200f);
        return d;
    }

    // Клавиша печатной машинки — мягче, ниже
    private static float[] Typewriter()
    {
        float[] d = Buf(0.08f);
        AddNoise(d, SfxRate, 0, 0.035f, 2200f, 1200f, 0.5f, 0.0005f, 110f, true);
        AddTone(d, SfxRate, 0, 0.04f, 260f, 180f, 0.45f, 0, 0.0005f, 90f);
        return d;
    }

    private static float[] Stamp()
    {
        float[] d = Buf(0.3f);
        AddTone(d, SfxRate, 0, 0.25f, 110f, 45f, 0.9f, 0, 0.001f, 14f);
        AddNoise(d, SfxRate, 0, 0.08f, 1500f, 400f, 0.6f, 0.0005f, 40f);
        return d;
    }

    // Покупка в книге: заклинание — пентатоника вверх на калимбе + мерцание
    private static float[] Chime()
    {
        float[] d = Buf(1.4f);
        float[] notes = { E5, G5, A5, C6, E6 };
        for (int i = 0; i < notes.Length; i++)
            Pluck(d, i * 0.07f, notes[i], 0.4f, 6f);
        AddTone(d, SfxRate, 0.3f, 0.9f, A4, A4, 0.15f, 1, 0.05f, 3f); // тёплая подложка снизу
        AddNoise(d, SfxRate, 0.15f, 0.7f, 6000f, 8000f, 0.03f, 0.08f, 4f, true);
        Echo(d, SfxRate, 0.14f, 0.3f, 2);
        return d;
    }

    private static float[] SplashSting()
    {
        float[] d = Buf(2.5f);
        // Низкий гул + диссонансный колокольчик (тритон)
        AddTone(d, SfxRate, 0, 2.4f, 55f, 52f, 0.5f, 2, 0.3f, 1.2f);
        AddTone(d, SfxRate, 0, 2.4f, 82.4f, 80f, 0.25f, 1, 0.4f, 1.2f);
        AddTone(d, SfxRate, 0.2f, 2f, 880f, 880f, 0.2f, 0, 0.002f, 2f);
        AddTone(d, SfxRate, 0.45f, 1.8f, 1244.5f, 1244.5f, 0.15f, 0, 0.002f, 2.2f);
        Echo(d, SfxRate, 0.2f, 0.3f, 2);
        return d;
    }

    // ============================== Музыка ==============================
    // Маленький секвенсор: ноты (midi, доля, длительность, инструмент) → петля без шва.

    private enum Inst { Bass, MusicBox, Pad, Pluck, Kick, Hat, Snare }

    private struct Note
    {
        public int midi;
        public float beat, len, vel;
        public Inst inst;
    }

    private class Song
    {
        public float bpm;
        public int beatsPerBar = 4;
        public int bars;
        public readonly List<Note> notes = new();

        public void Add(Inst inst, int midi, float beat, float len, float vel = 1f) =>
            notes.Add(new Note { inst = inst, midi = midi, beat = beat, len = len, vel = vel });
    }

    // Аккорды: корень (midi) + минор/мажор
    private struct Chord
    {
        public int root;
        public bool minor;
        public int Third => root + (minor ? 3 : 4);
        public int Fifth => root + 7;
        public Chord(int root, bool minor) { this.root = root; this.minor = minor; }
    }

    // ре минор: Dm Bb Gm A / Dm F Gm A
    private static readonly Chord[] SpookyProg =
    {
        new(50, true), new(46, false), new(43, true), new(45, false),
        new(50, true), new(41, false), new(43, true), new(45, false)
    };

    // Натуральный минор + повышенная VII (гармонический) для мелодий в ре
    private static readonly int[] DMinorScale = { 0, 2, 3, 5, 7, 8, 10, 11 };

    public static AudioClip MakeMusic(MusicId id)
    {
        rng = new System.Random(777 + (int)id * 31);
        Song song = id switch
        {
            MusicId.Menu => SongMenu(),
            MusicId.Shift => SongShift(132f, 0, false),
            MusicId.Final => SongShift(150f, 2, true),
            MusicId.Book => SongBook(),
            MusicId.Ending => SongEnding(),
            _ => SongMenu()
        };
        float[] d = Render(song);
        Normalize(d, 0.85f);
        return ToClip("music_" + id, d, MusicRate);
    }

    private static Song SongMenu()
    {
        var s = new Song { bpm = 84f, bars = 8 };
        for (int bar = 0; bar < s.bars; bar++)
        {
            Chord c = SpookyProg[bar];
            float b = bar * 4f;
            AddPad(s, c, b, 4f, 0.45f);
            s.Add(Inst.Bass, c.root - 12, b, 3.5f, 0.6f);
            // Музыкальная шкатулка: медленное арпеджио + мелодия в верхнем регистре
            int[] arp = { c.root + 12, c.Third + 12, c.Fifth + 12, c.Third + 12 };
            for (int i = 0; i < 4; i++)
                s.Add(Inst.MusicBox, arp[i], b + i, 1f, 0.35f);
        }
        AddMelody(s, 24, 1f, 0.55f, 0.6f, 62);
        return s;
    }

    private static Song SongShift(float bpm, int transpose, bool intense)
    {
        var s = new Song { bpm = bpm, bars = 8 };
        for (int bar = 0; bar < s.bars; bar++)
        {
            Chord c = SpookyProg[bar];
            float b = bar * 4f;
            // Шагающий бас восьмыми: корень-квинта-октава-квинта
            int[] bass = { c.root - 12, c.Fifth - 12, c.root, c.Fifth - 12 };
            for (int i = 0; i < 8; i++)
                s.Add(Inst.Bass, bass[i % 4], b + i * 0.5f, 0.45f, i % 2 == 0 ? 0.9f : 0.65f);

            // Щипковое арпеджио, на финале шестнадцатыми
            int step = intense ? 16 : 8;
            int[] arp = { c.root + 12, c.Third + 12, c.Fifth + 12, c.root + 24, c.Fifth + 12, c.Third + 12 };
            for (int i = 0; i < step; i++)
                s.Add(Inst.Pluck, arp[i % arp.Length], b + i * (4f / step), 4f / step, i % 4 == 0 ? 0.45f : 0.3f);

            // Ударные
            for (int beat = 0; beat < 4; beat++)
            {
                if (intense || beat % 2 == 0)
                    s.Add(Inst.Kick, 0, b + beat, 0.3f, 0.9f);
                if (beat % 2 == 1)
                    s.Add(Inst.Snare, 0, b + beat, 0.2f, 0.5f);
            }
            for (int i = 0; i < 8; i++)
                s.Add(Inst.Hat, 0, b + i * 0.5f, 0.05f, i % 2 == 1 ? 0.5f : 0.3f);

            if (bar % 2 == 1)
                AddPad(s, c, b, 4f, 0.2f);
        }
        AddMelody(s, 0, 0.5f, 0.5f, intense ? 0.55f : 0.45f, 62);

        if (transpose != 0)
            for (int i = 0; i < s.notes.Count; i++)
            {
                Note n = s.notes[i];
                if (n.inst != Inst.Kick && n.inst != Inst.Hat && n.inst != Inst.Snare)
                    n.midi += transpose;
                s.notes[i] = n;
            }
        return s;
    }

    private static Song SongBook()
    {
        var s = new Song { bpm = 72f, bars = 8 };
        for (int bar = 0; bar < s.bars; bar++)
        {
            Chord c = SpookyProg[bar];
            float b = bar * 4f;
            AddPad(s, c, b, 4f, 0.35f);
            s.Add(Inst.Bass, c.root - 12, b, 2f, 0.4f);
            s.Add(Inst.Bass, c.Fifth - 12, b + 2f, 2f, 0.3f);
            int[] arp = { c.root + 12, c.Fifth + 12, c.Third + 24, c.Fifth + 12 };
            for (int i = 0; i < 8; i++)
                s.Add(Inst.MusicBox, arp[i % 4], b + i * 0.5f, 0.5f, i % 2 == 0 ? 0.3f : 0.2f);
        }
        return s;
    }

    private static Song SongEnding()
    {
        // Вальс в фа мажоре: F C Dm Bb / F C Bb C
        var s = new Song { bpm = 108f, bars = 8, beatsPerBar = 3 };
        Chord[] prog =
        {
            new(41, false), new(48, false), new(50, true), new(46, false),
            new(41, false), new(48, false), new(46, false), new(48, false)
        };
        for (int bar = 0; bar < s.bars; bar++)
        {
            Chord c = prog[bar];
            float b = bar * 3f;
            s.Add(Inst.Bass, c.root - 12, b, 1f, 0.8f);
            for (int k = 1; k < 3; k++)
            {
                s.Add(Inst.Pluck, c.Third + 12, b + k, 0.8f, 0.3f);
                s.Add(Inst.Pluck, c.Fifth + 12, b + k, 0.8f, 0.3f);
            }
            AddPad(s, c, b, 3f, 0.2f);
        }
        // Мелодия по тонам аккорда, четвертями
        int[] scale = { 0, 2, 4, 5, 7, 9, 11 };
        int prev = 72;
        for (int beat = 0; beat < 24; beat++)
        {
            Chord c = prog[beat / 3];
            int target = beat % 3 == 0 ? NearestChordTone(c, prev) : NearestScaleStep(prev, scale, 65, rng.Next(-2, 3));
            if (beat % 6 == 5) continue;
            s.Add(Inst.MusicBox, target, beat, beat % 3 == 0 ? 1.5f : 1f, 0.5f);
            prev = target;
        }
        return s;
    }

    private static void AddPad(Song s, Chord c, float beat, float len, float vel)
    {
        s.Add(Inst.Pad, c.root, beat, len, vel);
        s.Add(Inst.Pad, c.Third, beat, len, vel * 0.8f);
        s.Add(Inst.Pad, c.Fifth, beat, len, vel * 0.8f);
    }

    // Мелодия: на сильных долях тон аккорда, между ними шаги по гамме. Паузы, чтобы дышала.
    private static void AddMelody(Song s, int transpose, float stepBeats, float restChance, float vel, int around)
    {
        int prev = around + 12;
        int steps = Mathf.RoundToInt(s.bars * s.beatsPerBar / stepBeats);
        for (int i = 0; i < steps; i++)
        {
            float beat = i * stepBeats;
            int bar = Mathf.FloorToInt(beat / s.beatsPerBar);
            Chord c = SpookyProg[bar % SpookyProg.Length];
            bool strong = Mathf.Abs(beat % 2f) < 0.01f;
            if (!strong && rng.NextDouble() < restChance)
                continue;

            int note = strong
                ? NearestChordTone(c, prev)
                : NearestScaleStep(prev, DMinorScale, 62, rng.Next(-2, 3));
            note = Mathf.Clamp(note, around, around + 19);
            float len = strong ? stepBeats * 1.5f : stepBeats;
            s.Add(Inst.MusicBox, note + transpose, beat, len, vel);
            prev = note;
        }
    }

    private static int NearestChordTone(Chord c, int near)
    {
        int best = near, bestDist = 99;
        foreach (int tone in new[] { c.root, c.Third, c.Fifth })
            for (int oct = -2; oct <= 3; oct++)
            {
                int m = tone + oct * 12;
                int dist = Math.Abs(m - near);
                if (dist < bestDist) { bestDist = dist; best = m; }
            }
        return best;
    }

    // Сдвинуться на steps ступеней гаммы (тоника = tonicMidi % 12)
    private static int NearestScaleStep(int from, int[] scale, int tonicMidi, int steps)
    {
        int tonic = tonicMidi % 12;
        var tones = new List<int>();
        for (int m = from - 14; m <= from + 14; m++)
            if (Array.IndexOf(scale, ((m - tonic) % 12 + 12) % 12) >= 0)
                tones.Add(m);
        int idx = 0, bestDist = 99;
        for (int i = 0; i < tones.Count; i++)
        {
            int dist = Math.Abs(tones[i] - from);
            if (dist < bestDist) { bestDist = dist; idx = i; }
        }
        return tones[Mathf.Clamp(idx + steps, 0, tones.Count - 1)];
    }

    private static float Mtof(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

    private static float[] Render(Song s)
    {
        float spb = 60f / s.bpm;
        int total = Mathf.RoundToInt(s.bars * s.beatsPerBar * spb * MusicRate);
        var d = new float[total];
        var noise = new System.Random(4242);

        foreach (Note n in s.notes)
        {
            int start = Mathf.RoundToInt(n.beat * spb * MusicRate);
            float dur = n.len * spb;
            float f = Mtof(n.midi);
            // Хвост после отпускания: шкатулка и щипок звенят дольше ноты
            float tail = n.inst switch
            {
                Inst.MusicBox => 1.2f,
                Inst.Pluck => 0.4f,
                Inst.Pad => 0.5f,
                Inst.Bass => 0.08f,
                _ => 0f
            };
            int len = Mathf.RoundToInt((dur + tail) * MusicRate);
            double phase = 0, phase2 = 0;
            float lp = 0f;

            for (int i = 0; i < len; i++)
            {
                float t = i / (float)MusicRate;
                float release = t < dur ? 1f : Mathf.Clamp01(1f - (t - dur) / Mathf.Max(0.001f, tail));
                float y;
                switch (n.inst)
                {
                    case Inst.Bass:
                    {
                        phase += f / MusicRate;
                        float raw = Wave(2, (float)phase) * 0.6f + Wave(1, (float)phase) * 0.6f;
                        float cut = 300f + 1400f * Mathf.Exp(-t * 14f);
                        lp += (1f - Mathf.Exp(-TwoPi * cut / MusicRate)) * (raw - lp);
                        y = lp * Mathf.Min(1f, t / 0.004f) * Mathf.Exp(-t * 2.5f) * release * 0.55f;
                        break;
                    }
                    case Inst.MusicBox:
                    {
                        phase += f / MusicRate;
                        phase2 += f * 4.02f / MusicRate;
                        y = (Mathf.Sin((float)phase * TwoPi) + 0.25f * Mathf.Sin((float)phase2 * TwoPi) * Mathf.Exp(-t * 12f))
                            * Mathf.Min(1f, t / 0.002f) * Mathf.Exp(-t * 3.2f) * release * 0.32f;
                        break;
                    }
                    case Inst.Pluck:
                    {
                        phase += f / MusicRate;
                        float raw = Wave(3, (float)phase) * 0.5f + Wave(1, (float)phase);
                        float cut = 800f + 3500f * Mathf.Exp(-t * 20f);
                        lp += (1f - Mathf.Exp(-TwoPi * cut / MusicRate)) * (raw - lp);
                        y = lp * Mathf.Min(1f, t / 0.002f) * Mathf.Exp(-t * 7f) * release * 0.22f;
                        break;
                    }
                    case Inst.Pad:
                    {
                        phase += f * 0.997f / MusicRate;
                        phase2 += f * 1.003f / MusicRate;
                        float raw = Wave(2, (float)phase) + Wave(2, (float)phase2);
                        lp += (1f - Mathf.Exp(-TwoPi * 900f / MusicRate)) * (raw - lp);
                        float att = Mathf.Min(1f, t / 0.35f);
                        y = lp * att * release * 0.09f;
                        break;
                    }
                    case Inst.Kick:
                    {
                        float kf = 45f + 110f * Mathf.Exp(-t * 30f);
                        phase += kf / MusicRate;
                        y = Mathf.Sin((float)phase * TwoPi) * Mathf.Exp(-t * 14f) * 0.8f;
                        break;
                    }
                    case Inst.Snare:
                    {
                        float x = (float)(noise.NextDouble() * 2 - 1);
                        lp += 0.5f * (x - lp);
                        phase += 190f / MusicRate;
                        y = ((x - lp) * 0.6f + Mathf.Sin((float)phase * TwoPi) * 0.3f) * Mathf.Exp(-t * 22f) * 0.5f;
                        break;
                    }
                    default: // Hat
                    {
                        float x = (float)(noise.NextDouble() * 2 - 1);
                        lp += 0.6f * (x - lp);
                        y = (x - lp) * Mathf.Exp(-t * 60f) * 0.35f;
                        break;
                    }
                }
                // Петля без шва: хвост с конца заходит в начало
                d[(start + i) % total] += y * n.vel;
            }
        }

        // Лёгкая «комната» для атмосферы
        Echo(d, MusicRate, spb * 0.75f, 0.18f, 1);
        return d;
    }

    // ============================== Общее ==============================

    private static void Normalize(float[] d, float peak)
    {
        float max = 0f;
        for (int i = 0; i < d.Length; i++)
            max = Mathf.Max(max, Mathf.Abs(d[i]));
        if (max < 1e-5f)
            return;
        float k = peak / max;
        for (int i = 0; i < d.Length; i++)
            d[i] *= k;
    }

    private static AudioClip ToClip(string name, float[] data, int rate)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
