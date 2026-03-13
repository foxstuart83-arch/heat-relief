using UnityEngine;

namespace TNG.UI
{
    /// <summary>
    /// Static colour palette for the LCARS interface system.
    /// All UI scripts source colours from here to ensure visual consistency.
    /// </summary>
    public static class LCARSColors
    {
        // ── Primary palette ──────────────────────────────────────────────────

        /// <summary>LCARS orange — primary interactive elements, headers.</summary>
        public static readonly Color Orange = HexToColor("FF9900");

        /// <summary>LCARS purple — secondary panels, decorative strips.</summary>
        public static readonly Color Purple = HexToColor("CC88FF");

        /// <summary>LCARS blue — tertiary elements, status indicators.</summary>
        public static readonly Color Blue = HexToColor("4488FF");

        // ── Alert colours ────────────────────────────────────────────────────

        /// <summary>Red Alert tint (full red).</summary>
        public static readonly Color RedAlert = HexToColor("FF0000");

        /// <summary>Yellow Alert colour.</summary>
        public static readonly Color YellowAlert = HexToColor("FFCC00");

        // ── Status colours ───────────────────────────────────────────────────

        /// <summary>Warp core nominal — bright green.</summary>
        public static readonly Color StatusGreen = HexToColor("44FF88");

        /// <summary>Warning state — amber.</summary>
        public static readonly Color StatusAmber = HexToColor("FFAA00");

        /// <summary>Critical state — red.</summary>
        public static readonly Color StatusRed = HexToColor("FF2222");

        // ── Background colours ───────────────────────────────────────────────

        /// <summary>Base panel background, 85% opacity applied in code.</summary>
        public static readonly Color PanelBackground = new Color(0f, 0f, 0f, 0.85f);

        /// <summary>Darker stripe used inside panels.</summary>
        public static readonly Color PanelStripe = HexToColor("111111");

        // ── Text colours ─────────────────────────────────────────────────────

        /// <summary>Primary text — bright white.</summary>
        public static readonly Color TextPrimary = Color.white;

        /// <summary>Secondary text — muted orange-cream.</summary>
        public static readonly Color TextSecondary = HexToColor("FFDDAA");

        /// <summary>Dimmed text for inactive elements.</summary>
        public static readonly Color TextDimmed = HexToColor("886644");

        // ── Division uniform colours ─────────────────────────────────────────

        /// <summary>Command division uniform — deep red.</summary>
        public static readonly Color UniformCommand = HexToColor("CC0000");

        /// <summary>Operations/Engineering division — gold.</summary>
        public static readonly Color UniformOperations = HexToColor("CCAA00");

        /// <summary>Sciences/Medical division — teal.</summary>
        public static readonly Color UniformSciences = HexToColor("006666");

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Converts a 6-character hex colour string to a Unity Color (alpha = 1).
        /// </summary>
        public static Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString("#" + hex, out Color c))
                return c;
            Debug.LogWarning($"[LCARSColors] Could not parse hex: {hex}");
            return Color.magenta;
        }

        /// <summary>
        /// Returns the uniform colour for a named division string.
        /// Accepts "Command", "Operations", "Sciences", "Medical", "Engineering".
        /// </summary>
        public static Color DivisionColor(string division)
        {
            return division?.ToLower() switch
            {
                "command"     => UniformCommand,
                "operations"  => UniformOperations,
                "engineering" => UniformOperations,
                "sciences"    => UniformSciences,
                "medical"     => UniformSciences,
                _             => Color.white
            };
        }
    }
}
