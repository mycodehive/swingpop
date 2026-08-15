namespace SwingPop.Gameplay.Hole
{
    public static class ScoreCalculator
    {
        public static ScoreResult Calculate(int par, int strokes)
        {
            int safePar = System.Math.Max(1, par);
            int safeStrokes = System.Math.Max(0, strokes);
            int relative = safeStrokes - safePar;
            string label = relative switch
            {
                <= -3 => "Albatross",
                -2 => "Eagle",
                -1 => "Birdie",
                0 => "Par",
                1 => "Bogey",
                2 => "Double Bogey",
                _ => relative > 0 ? $"+{relative}" : relative.ToString()
            };
            return new ScoreResult(safeStrokes, safePar, relative, label);
        }
    }
}
