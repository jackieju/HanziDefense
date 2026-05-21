namespace HanziZombieDefense.Hanzi.Recognition
{
    /// <summary>
    /// Eight compass directions used for quantizing stroke segment headings.
    /// Indices map directly to the result of <c>round(angleDeg / 45) % 8</c>
    /// where <c>angleDeg</c> is the standard <c>atan2(dy, dx)</c> in degrees,
    /// i.e. 0° points East and angle grows counter-clockwise (Y-up).
    ///
    /// HanziWriter median data is delivered in Y-up coordinates, so a vertical
    /// stroke (竖) drawn top-to-bottom has dy &lt; 0 → angle ≈ 270° → <see cref="S"/>.
    /// </summary>
    public enum Dir8
    {
        E = 0,
        NE = 1,
        N = 2,
        NW = 3,
        W = 4,
        SW = 5,
        S = 6,
        SE = 7,
    }

    /// <summary>
    /// Canonical Chinese basic-stroke types used by the recognizer. The game
    /// classifies each player-drawn polyline into one of these and accepts the
    /// stroke when it falls in the same family as the expected (template) type.
    /// </summary>
    /// <remarks>
    /// Naming follows pinyin transliteration of the standard 笔画 names. See
    /// <see cref="StrokeClassifier"/> for the recognized direction patterns.
    /// </remarks>
    public enum StrokeType
    {
        /// <summary>Could not be classified into a known type.</summary>
        Unknown,

        /// <summary>横 — horizontal, left → right.</summary>
        Heng,

        /// <summary>竖 — vertical, top → bottom.</summary>
        Shu,

        /// <summary>撇 — falling left, upper-right → lower-left.</summary>
        Pie,

        /// <summary>捺 — falling right, upper-left → lower-right.</summary>
        Na,

        /// <summary>点 — dot. Very short, generally downward / down-right.</summary>
        Dian,

        /// <summary>提 — rising stroke, lower-left → upper-right.</summary>
        Ti,

        /// <summary>横折 — horizontal turning down (└ shape rotated).</summary>
        HengZhe,

        /// <summary>竖折 — vertical turning right.</summary>
        ShuZhe,

        /// <summary>横钩 — horizontal with a small downward hook at the right end.</summary>
        HengGou,

        /// <summary>竖钩 — vertical with a left/up hook at the bottom.</summary>
        ShuGou,

        /// <summary>弯钩 — curved vertical with a hook (used in 子, 手 etc).</summary>
        WanGou,

        /// <summary>斜钩 — diagonal (SE) stroke ending in an upward hook.</summary>
        XieGou,

        /// <summary>横折钩 — horizontal, then down, ending in a hook.</summary>
        HengZheGou,

        /// <summary>竖弯钩 — vertical curving right, ending in an upward hook (e.g. 儿).</summary>
        ShuWanGou,

        /// <summary>横撇 — horizontal turning into a falling-left.</summary>
        HengPie,

        /// <summary>撇折 — falling-left turning right (forms a small V).</summary>
        PieZhe,

        /// <summary>横折弯钩 — horizontal, down, right, then hook (e.g. 九).</summary>
        HengZheWanGou,
    }
}
