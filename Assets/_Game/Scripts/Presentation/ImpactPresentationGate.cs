namespace SwingPop.Presentation
{
    public sealed class ImpactPresentationGate
    {
        public bool IsArmed { get; private set; }

        public void Arm()
        {
            IsArmed = true;
        }

        public bool TryConsume()
        {
            if (!IsArmed)
            {
                return false;
            }

            IsArmed = false;
            return true;
        }

        public void Reset()
        {
            IsArmed = false;
        }
    }
}
