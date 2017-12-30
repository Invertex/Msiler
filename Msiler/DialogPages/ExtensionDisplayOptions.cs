﻿using Msiler.Lib;
using System.ComponentModel;

namespace Msiler.DialogPages
{
    public class ExtensionDisplayOptions : MsilerDialogPage
    {
        private const string CategoryTitle = "Display";

        [Category(CategoryTitle)]
        [DisplayName("Listing font name")]
        [Description("")]
        public string FontName { get; set; } = "Consolas";

        [Category(CategoryTitle)]
        [DisplayName("Listing font size")]
        [Description("")]
        public int FontSize { get; set; } = 12;

        [Category(CategoryTitle)]
        [DisplayName("Show line numbers")]
        [Description("")]
        public bool LineNumbers { get; set; } = false;

        [Category(CategoryTitle)]
        [DisplayName("Tooltip transparency")]
        [Description("(In percent)")]
        public int TooltipTransparency { get; set; } = 0;

        [Category(CategoryTitle)]
        [DisplayName("Color scheme")]
        [Description("")]
        public MsilerColorTheme ColorScheme { get; set; } = MsilerColorTheme.Auto;
    }
}
