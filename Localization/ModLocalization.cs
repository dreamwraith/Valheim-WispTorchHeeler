using System;
using System.Collections.Generic;
using Jotunn.Managers;

namespace WispTorchHeeler.Localization
{
    /// <summary>
    /// Standalone localization module managing built-in tokens and custom translation files.
    /// Allows easy expansion for community language packs (.json / .yaml).
    /// </summary>
    public static class ModLocalization
    {
        // Custom Piece Tokens
        public const string TokenPieceTorchLg = "$piece_wisptorch_heeler_lg";
        public const string TokenPieceTorchLgDesc = "$piece_wisptorch_heeler_lg_description";
        public const string TokenPieceSmallLamp = "$piece_wisplamp_heeler";
        public const string TokenPieceSmallLampDesc = "$piece_wisplamp_heeler_description";
        public const string TokenPieceLampLg = "$piece_wisplamp_heeler_lg";
        public const string TokenPieceLampLgDesc = "$piece_wisplamp_heeler_lg_description";
        public const string TokenPieceLampGrand = "$piece_wisplamp_heeler_grand";
        public const string TokenPieceLampGrandDesc = "$piece_wisplamp_heeler_grand_description";

        // In-Game Notification & Feedback Tokens
        public const string TokenMsgPainted = "$msg_torch_painted";
        public const string TokenMsgReset = "$msg_torch_reset";
        public const string TokenMsgCantPaintWarded = "$msg_cantpaint_warded";
        public const string TokenMsgCantPaintDisabled = "$msg_cantpaint_disabled";

        // Hover Indicator Prompt Tokens
        public const string TokenPromptPaint = "$prompt_torch_paint";
        public const string TokenPromptReset = "$prompt_torch_reset";

        public static void Initialize()
        {
            var customLocalization = LocalizationManager.Instance.GetLocalization();

            // English Translations via standard Jotunn Dictionary
            customLocalization.AddTranslation("English", new Dictionary<string, string>
            {
                { TokenPieceTorchLg, "Large Wisp Torch" },
                { TokenPieceTorchLgDesc, "A large wisp torch that clears mist across a wide area." },
                { TokenPieceSmallLamp, "Dvergr Wisp Lamp" },
                { TokenPieceSmallLampDesc, "A sturdy dwarven demister lamp that clears mist across a wide area." },
                { TokenPieceLampLg, "Large Dvergr Wisp Lamp" },
                { TokenPieceLampLgDesc, "An advanced dwarven demister with an expanded core that clears mist across a wide radius." },
                { TokenPieceLampGrand, "Grand Dvergr Wisp Lamp" },
                { TokenPieceLampGrandDesc, "A colossal, reinforced dwarven demister monument that clears mist across a vast perimeter." },
                { TokenMsgPainted, "Wisp piece painted: {0}" },
                { TokenMsgReset, "Wisp piece color reset to default." },
                { TokenMsgCantPaintWarded, "Piece is protected by a ward." },
                { TokenMsgCantPaintDisabled, "Painting wisp pieces is disabled on this server." },
                { TokenPromptPaint, "Paint Light" },
                { TokenPromptReset, "Reset Color" }
            });

            Plugin.LogDebug("[ModLocalization] Default English localizations registered.");
        }

        /// <summary>
        /// Translates a given token key using Jotunn's CustomLocalization.
        /// </summary>
        public static string Localize(string token, params string[] arguments)
        {
            string translatedText = LocalizationManager.Instance.GetLocalization().TryTranslate(token);
            if (arguments != null && arguments.Length > 0)
            {
                translatedText = string.Format(translatedText, arguments);
            }
            return translatedText;
        }
    }
}
