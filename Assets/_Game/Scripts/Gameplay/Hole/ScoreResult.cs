using System;

namespace SwingPop.Gameplay.Hole
{
    [Serializable]
    public readonly struct ScoreResult
    {
        public ScoreResult(int strokes, int par, int relativeToPar, string label)
        {
            Strokes = strokes;
            Par = par;
            RelativeToPar = relativeToPar;
            Label = label;
        }

        public int Strokes { get; }
        public int Par { get; }
        public int RelativeToPar { get; }
        public string Label { get; }

        public override string ToString()
        {
            string relative = RelativeToPar > 0 ? $"+{RelativeToPar}" : RelativeToPar.ToString();
            return $"{Label} ({relative}) — {Strokes} strokes / Par {Par}";
        }
    }
}
