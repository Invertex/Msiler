﻿using System.Windows.Controls;
using ICSharpCode.AvalonEdit;
using Msiler.DialogPages;
using Msiler.Lib;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using System.Collections.Generic;
using Msiler.AssemblyParser;
using Msiler.Helpers;
using System.Linq;
using System;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using System.Text;

namespace Msiler.UI
{
    public partial class MyControl : UserControl
    {
        const string RepoUrl = @"https://github.com/segrived/Msiler";

        AssemblyManager _assemblyManager = new AssemblyManager();

        AssemblyMethod _currentMethod;
        AssemblyMethod CurrentMethod {
            get { return this._currentMethod; }
            set
            {
                this._currentMethod = value;
                this.MethodsList.SelectedItem = value;
            }
        }

        List<AssemblyMethod> _assemblyMethods;
        Dictionary<AssemblyMethod, string> _listingCache =
            new Dictionary<AssemblyMethod, string>();


        public MyControl() {
            InitializeComponent();

            InitConfiguration();
            InitEventHandlers();
        }

        void OnMethodListChanged(object sender, MethodsListEventArgs e) {
            this._assemblyMethods = e.Methods;
            this.MethodsList.ItemsSource = new ObservableCollection<AssemblyMethod>(this._assemblyMethods);
            this._listingCache.Clear();

            if (this.CurrentMethod != null) {
                this.ProcessMethod(e.Methods.FirstOrDefault(m => m.Equals(this.CurrentMethod)));
            }
            var view = CollectionViewSource.GetDefaultView(this.MethodsList.ItemsSource);
            view.Filter = FilterMethodsList;
        }

        public void InitConfiguration() {
            this.BytecodeListing.Options = new TextEditorOptions {
                EnableEmailHyperlinks = false,
                EnableHyperlinks = false
            };
            UpdateDisplayOptions();
        }

        void UpdateDisplayOptions() {
            var displayOptions = Common.Instance.DisplayOptions;
            string fontFamily = "Consolas";
            if (FontHelpers.IsFontFamilyExist(displayOptions.FontName)) {
                fontFamily = displayOptions.FontName;
            }
            BytecodeListing.FontFamily = new FontFamily(fontFamily);
            BytecodeListing.FontSize = displayOptions.FontSize;
            BytecodeListing.ShowLineNumbers = displayOptions.LineNumbers;
            BytecodeListing.SyntaxHighlighting = ColorTheme.GetColorTheme(displayOptions.ColorTheme);
        }

        public void InitEventHandlers() {
            Common.Instance.DisplayOptions.Applied += (s, e)
                => UpdateDisplayOptions();
            Common.Instance.ListingGenerationOptions.Applied += (s, e) => {
                this._listingCache.Clear();
                this.ProcessMethod(this.CurrentMethod);
            };
            Common.Instance.ExcludeOptions.Applied += (s, e) => {
                if (MethodsList.ItemsSource != null) {
                    CollectionViewSource.GetDefaultView(MethodsList.ItemsSource).Refresh();
                }
            };
            VSColorTheme.ThemeChanged += (e) => UpdateDisplayOptions();

            FunctionFollower.MethodSelected += OnMethodSelected;
            _assemblyManager.MethodListChanged += OnMethodListChanged;
        }

        void OnMethodSelected(object sender, MethodSignatureEventArgs e) {
            if (this._assemblyMethods == null || this._assemblyMethods.Count == 0) {
                return;
            }
            // do not process if same method
            if (this.CurrentMethod != null && this.CurrentMethod.Signature.Equals(e.MethodSignature)) {
                return;
            }
            // find metyhod with same signature
            var selMethod = this._assemblyMethods.FirstOrDefault(m => m.Signature.Equals(e.MethodSignature));
            if (selMethod != null) {
                ProcessMethod(selMethod);
            }
        }


        bool FilterMethodsList(object o) {
            var method = (AssemblyMethod)o;

            // filter method types
            var excludeOptions = Common.Instance.ExcludeOptions;
            if (excludeOptions.ExcludeConstructors && method.IsConstructor)
                return false;
            if (excludeOptions.ExcludeProperties && method.IsProperty)
                return false;
            if (excludeOptions.ExcludeAnonymousMethods && method.IsAnonymous)
                return false;

            // filter methods by search
            var filterQuery = FilterMethodsTextBox.Text;
            if (String.IsNullOrEmpty(filterQuery))
                return true;

            return method.Signature.MethodName.Contains(filterQuery, StringComparison.OrdinalIgnoreCase);
        }

        public ListingGeneratorOptions GetGeneratorOptions()
            => Common.Instance.ListingGenerationOptions.ToListingGeneratorOptions();

        private void ProcessMethod(AssemblyMethod method) {
            if (method != null) {
                this.CurrentMethod = method;
                var listingText = (this._listingCache.ContainsKey(method))
                    ? _listingCache[method]
                    : this.CurrentMethod.GenerateListing(this.GetGeneratorOptions());
                this._listingCache[method] = listingText;
                this.BytecodeListing.Text = listingText;
            }
        }

        #region Instruction Hint Tooltip
        ToolTip toolTip = new ToolTip();

        void BytecodeListing_MouseHover(object sender, MouseEventArgs e) {
            var pos = BytecodeListing.GetPositionFromPoint(e.GetPosition(BytecodeListing));

            if (pos == null)
                return;

            int off = BytecodeListing.Document.GetOffset(pos.Value.Line, pos.Value.Column);
            var wordUnderCursor = AvalonEditHelpers.GetWordOnOffset(BytecodeListing.Document, off);

            var numberUnderCursor = StringHelpers.ParseNumber(wordUnderCursor);
            if (numberUnderCursor != null) {
                var v = numberUnderCursor.Value;
                var sb = new StringBuilder();
                sb.AppendLine($"Decimal: {v}");
                sb.AppendLine($"HEX: 0x{Convert.ToString(v, 16)}");
                sb.AppendLine($"Binary: 0b{Convert.ToString(v, 2).ToUpper()}");
                sb.AppendLine($"Octal: 0{Convert.ToString(v, 8)}");
                ShowToolTip(sb.ToString());
            }

            var info = AssemblyParser.Helpers.GetInstructionInformation(wordUnderCursor);
            if (info != null) {
                ShowToolTip($"{info.Name}: {info.Description}");
            }
            e.Handled = true;
        }

        public void ShowToolTip(string content) {
            toolTip.PlacementTarget = this;
            toolTip.Content = new TextEditor {
                Text = content,
                Opacity = 0.6,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden
            };
            toolTip.IsOpen = true;
        }

        void BytecodeListing_MouseHoverStopped(object sender, MouseEventArgs e) {
            toolTip.IsOpen = false;
        }
        #endregion

        #region UI handlers
        void MethodsList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            this.ProcessMethod((AssemblyMethod)this.MethodsList.SelectedItem);
        }

        void FilterMethodsTextBox_TextChanged(object sender, TextChangedEventArgs e) =>
            CollectionViewSource.GetDefaultView(MethodsList.ItemsSource).Refresh();

        void HyperlinkOptions_Click(object sender, System.Windows.RoutedEventArgs e) =>
            Common.Instance.Package.ShowOptionPage(typeof(ExtensionGeneralOptions));

        void HyperlinkGithub_Click(object sender, System.Windows.RoutedEventArgs e) =>
            Process.Start(RepoUrl);

        void HyperlinkAbout_Click(object sender, System.Windows.RoutedEventArgs e) =>
            new AboutWindow().ShowDialog();

        void IsFollowModeEnabled_CheckedChange(object sender, System.Windows.RoutedEventArgs e) {
            FunctionFollower.IsFollowingEnabled = ((CheckBox)sender).IsChecked.Value;
        }

        private void MenuItemGeneralOptions_Click(object sender, System.Windows.RoutedEventArgs e) {
            Common.Instance.Package.ShowOptionPage(typeof(ExtensionGeneralOptions));
        }

        private void MenuItemListingGenearationOptions_Click(object sender, System.Windows.RoutedEventArgs e) {
            Common.Instance.Package.ShowOptionPage(typeof(ExtensionListingGenerationOptions));
        }

        private void MenuItemMethodFilteringOptions_Click(object sender, System.Windows.RoutedEventArgs e) {
            Common.Instance.Package.ShowOptionPage(typeof(ExtensionExcludeOptions));
        }

        private void MenuItemDisplayOptions_Click(object sender, System.Windows.RoutedEventArgs e) {
            Common.Instance.Package.ShowOptionPage(typeof(ExtensionDisplayOptions));
        }

        private void OptionsLink_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                TextBlock block = sender as TextBlock;
                ContextMenu contextMenu = block.ContextMenu;
                contextMenu.PlacementTarget = block;
                contextMenu.IsOpen = true;
                e.Handled = true;
            }
        }
        #endregion UI handlers
    }
}