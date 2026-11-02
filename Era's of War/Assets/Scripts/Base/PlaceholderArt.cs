using System.Linq;
using ErasOfWar.Simulation.Base;
using UnityEngine;

namespace ErasOfWar.Unity.Base
{
    /// <summary>Placeholder visuals: flat colored squares and short text labels. No art assets.</summary>
    public static class PlaceholderArt
    {
        public static readonly Color Background = new Color(0.10f, 0.13f, 0.10f);
        public static readonly Color GroundA = new Color(0.33f, 0.55f, 0.28f);
        public static readonly Color GroundB = new Color(0.30f, 0.51f, 0.25f);
        public static readonly Color Perimeter = new Color(0.38f, 0.30f, 0.20f);
        public static readonly Color Selection = Color.white;
        public static readonly Color GhostValid = new Color(0.20f, 0.95f, 0.30f, 0.55f);
        public static readonly Color GhostInvalid = new Color(0.95f, 0.20f, 0.20f, 0.55f);

        private static Sprite _white;
        private static Font _font;

        /// <summary>A 1x1-unit white square with its pivot at the bottom-left corner.</summary>
        public static Sprite WhiteSquare
        {
            get
            {
                if (_white != null) return _white;
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "PlaceholderWhite" };
                tex.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
                tex.Apply();
                _white = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.zero, 4f);
                _white.name = "PlaceholderWhite";
                return _white;
            }
        }

        public static Font Font => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static Color ColorFor(BuildingDef def)
        {
            switch (def.Id)
            {
                case "command_center": return new Color(0.20f, 0.38f, 0.90f);
                case "gold_mine": return new Color(0.98f, 0.82f, 0.15f);
                case "gold_vault": return new Color(0.80f, 0.62f, 0.08f);
                case "oil_derrick": return new Color(0.22f, 0.22f, 0.26f);
                case "oil_tank": return new Color(0.42f, 0.30f, 0.52f);
                case "steel_mill": return new Color(0.66f, 0.70f, 0.76f);
                case "steel_depot": return new Color(0.46f, 0.52f, 0.60f);
            }

            switch (def.Category)
            {
                case "Training": return new Color(0.82f, 0.26f, 0.20f);
                case "Defense": return new Color(0.96f, 0.52f, 0.16f);
                case "Support": return new Color(0.18f, 0.66f, 0.66f);
                case "Alliance": return new Color(0.56f, 0.36f, 0.78f);
                case "Wall": return new Color(0.58f, 0.43f, 0.27f);
                default: return Color.magenta;
            }
        }

        /// <summary>"Gold Mine" -> "GM", "Command Center" -> "CC", "88mm Flak Tower" -> "8FT".</summary>
        public static string ShortName(BuildingDef def)
        {
            if (def.IsWall) return "";
            var letters = def.Name.Split(' ', '-')
                .Where(w => w.Length > 0)
                .Select(w => char.ToUpperInvariant(w[0]))
                .Take(3);
            return new string(letters.ToArray());
        }

        /// <summary>Readable text color on top of a fill color.</summary>
        public static Color TextOn(Color fill) =>
            (0.299f * fill.r + 0.587f * fill.g + 0.114f * fill.b) > 0.6f ? Color.black : Color.white;
    }
}
