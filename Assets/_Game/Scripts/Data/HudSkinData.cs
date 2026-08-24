using UnityEngine;

namespace SwingPop.Data
{
    public enum HudSkinTone
    {
        Cyan,
        Mint,
        Pink,
        Gold,
        Coral,
        Disabled,
        Fairway,
        Rough,
        Bunker,
        Green,
        PrimaryText,
        SecondaryText
    }

    [CreateAssetMenu(fileName = "HudSkin", menuName = "SwingPop/HUD Skin")]
    public sealed class HudSkinData : ScriptableObject
    {
        [Header("Panels")]
        [SerializeField] private Color panelColor = new(0.018f, 0.105f, 0.16f, 0.88f);
        [SerializeField] private Color raisedPanelColor = new(0.025f, 0.2f, 0.27f, 0.94f);
        [SerializeField] private Color borderColor = new(0.2f, 0.92f, 1f, 0.72f);
        [SerializeField] private Color shadowColor = new(0.005f, 0.025f, 0.055f, 0.5f);

        [Header("Accents")]
        [SerializeField] private Color cyan = new(0.18f, 0.92f, 1f, 1f);
        [SerializeField] private Color mint = new(0.33f, 1f, 0.72f, 1f);
        [SerializeField] private Color pink = new(1f, 0.27f, 0.62f, 1f);
        [SerializeField] private Color gold = new(1f, 0.79f, 0.16f, 1f);
        [SerializeField] private Color coral = new(1f, 0.34f, 0.26f, 1f);
        [SerializeField] private Color disabled = new(0.43f, 0.57f, 0.66f, 1f);

        [Header("Typography")]
        [SerializeField] private Color primaryText = new(0.96f, 0.99f, 1f, 1f);
        [SerializeField] private Color secondaryText = new(0.67f, 0.9f, 0.96f, 1f);

        [Header("Lie Accents")]
        [SerializeField] private Color fairway = new(0.33f, 0.95f, 0.57f, 1f);
        [SerializeField] private Color rough = new(0.18f, 0.65f, 0.39f, 1f);
        [SerializeField] private Color bunker = new(1f, 0.72f, 0.3f, 1f);
        [SerializeField] private Color green = new(0.42f, 1f, 0.78f, 1f);

        [Header("Shared UI Sprites")]
        [SerializeField] private Sprite roundedPanel;
        [SerializeField] private Sprite capsule;
        [SerializeField] private Sprite circle;
        [SerializeField] private Sprite diamond;
        [SerializeField] private Sprite triangle;
        [SerializeField] private Sprite playerIcon;
        [SerializeField] private Sprite windIcon;
        [SerializeField] private Sprite driverIcon;
        [SerializeField] private Sprite putterIcon;
        [SerializeField] private Sprite spinNoneIcon;
        [SerializeField] private Sprite spinTopIcon;
        [SerializeField] private Sprite spinBackIcon;
        [SerializeField] private Sprite spinLeftIcon;
        [SerializeField] private Sprite spinRightIcon;
        [SerializeField] private Sprite targetIcon;

        public Color PanelColor => panelColor;
        public Color RaisedPanelColor => raisedPanelColor;
        public Color BorderColor => borderColor;
        public Color ShadowColor => shadowColor;
        public Color Cyan => cyan;
        public Color Mint => mint;
        public Color Pink => pink;
        public Color Gold => gold;
        public Color Coral => coral;
        public Color Disabled => disabled;
        public Color PrimaryText => primaryText;
        public Color SecondaryText => secondaryText;
        public Color Fairway => fairway;
        public Color Rough => rough;
        public Color Bunker => bunker;
        public Color Green => green;
        public Sprite RoundedPanel => roundedPanel;
        public Sprite Capsule => capsule;
        public Sprite Circle => circle;
        public Sprite Diamond => diamond;
        public Sprite Triangle => triangle;
        public Sprite PlayerIcon => playerIcon;
        public Sprite WindIcon => windIcon;
        public Sprite DriverIcon => driverIcon;
        public Sprite PutterIcon => putterIcon;
        public Sprite SpinNoneIcon => spinNoneIcon;
        public Sprite SpinTopIcon => spinTopIcon;
        public Sprite SpinBackIcon => spinBackIcon;
        public Sprite SpinLeftIcon => spinLeftIcon;
        public Sprite SpinRightIcon => spinRightIcon;
        public Sprite TargetIcon => targetIcon;

        public Color Resolve(HudSkinTone tone)
        {
            return tone switch
            {
                HudSkinTone.Mint => mint,
                HudSkinTone.Pink => pink,
                HudSkinTone.Gold => gold,
                HudSkinTone.Coral => coral,
                HudSkinTone.Disabled => disabled,
                HudSkinTone.Fairway => fairway,
                HudSkinTone.Rough => rough,
                HudSkinTone.Bunker => bunker,
                HudSkinTone.Green => green,
                HudSkinTone.PrimaryText => primaryText,
                HudSkinTone.SecondaryText => secondaryText,
                _ => cyan
            };
        }
    }
}
