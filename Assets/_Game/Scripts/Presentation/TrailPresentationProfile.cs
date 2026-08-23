namespace SwingPop.Presentation
{
    public readonly struct TrailPresentationProfile
    {
        public TrailPresentationProfile(float lifetime, float width)
        {
            Lifetime = lifetime;
            Width = width;
        }

        public float Lifetime { get; }
        public float Width { get; }
    }
}
