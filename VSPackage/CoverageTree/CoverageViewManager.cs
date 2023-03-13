// OpenCppCoverage is an open source code coverage for C++.
// Copyright (C) 2016 OpenCppCoverage
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;
using OpenCppCoverage.VSPackage.CoverageRateBuilder;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace OpenCppCoverage.VSPackage.CoverageTree
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [Export(typeof(ICoverageViewManager))]
    [ContentType("C/C++")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class CoverageViewManager : IWpfTextViewCreationListener, ICoverageViewManager
    {
        //---------------------------------------------------------------------
        static CoverageViewManager()
        {
            LoadBrushes();
        }

        //---------------------------------------------------------------------
        private static void LoadBrushes()
        {
            CoveredBrush = LoadBrush("CoveredLineColor");
            UncoveredBrush = LoadBrush("UnCoveredLineColor");
        }

        //---------------------------------------------------------------------
        private static SolidColorBrush LoadBrush(string keyName)
        {
            var key = new ThemeResourceKey(OpenCppCoverageCategory, keyName, ThemeResourceKeyType.BackgroundBrush);
            var color = VSColorTheme.GetThemedColor(key);
            return new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        //---------------------------------------------------------------------
        private static readonly Guid OpenCppCoverageCategory = new Guid("F50C7A34-815C-4FCB-BE28-7EDBB3185A04");
        public const string HighlightLinesAdornment = "HighlightLines";
        public static object CoverageTag = new object();
        public static Brush CoveredBrush;
        public static Brush UncoveredBrush;

        //---------------------------------------------------------------------
        private readonly List<IWpfTextView> _views = new List<IWpfTextView>();
        private readonly FileCoverageAggregator _fileCoverageAggregator = new FileCoverageAggregator();

        private Dictionary<string, FileCoverage> _coverageByFile = new Dictionary<string, FileCoverage>();

        private class Handler
        {
            public EventHandler<TextContentChangedEventArgs> OnTextChanged { get; set; }
            public ThemeChangedEventHandler OnThemeChanged { get; set; }
        }

        private readonly Dictionary<IWpfTextView, Handler> _handlers = new Dictionary<IWpfTextView, Handler>();
        private bool _showCoverage;

        //---------------------------------------------------------------------
        public void TextViewCreated(IWpfTextView textView)
        {
            this._views.Add(textView);
            textView.Closed += OnTextViewClosed;
            textView.LayoutChanged += OnLayoutChanged;

            void TextChanged(object sender, TextContentChangedEventArgs e) => OnTextChanged(textView, e);
            void OnThemeChanged(ThemeChangedEventArgs e) => OnThemeChangedEvent(textView);

            _handlers.Add(textView, new Handler
            {
                OnTextChanged = TextChanged,
                OnThemeChanged = OnThemeChanged
            });
            textView.TextBuffer.Changed += TextChanged;
            VSColorTheme.ThemeChanged += OnThemeChanged;
        }

        //---------------------------------------------------------------------
        // These lines declare new AdornmentLayer.
        [Export(typeof(AdornmentLayerDefinition))]
        [Name(HighlightLinesAdornment)]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
        [TextViewRole(PredefinedTextViewRoles.Document)]
        public AdornmentLayerDefinition EditorAdornmentLayer = null;

        //---------------------------------------------------------------------
        [Import] private ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        //---------------------------------------------------------------------
        public CoverageRate CoverageRate
        {
            set
            {
                this._coverageByFile = _fileCoverageAggregator.Aggregate(value, NormalizePath);
                this.RemoveHighlightForAllViews();
                AddHighlightCoverageForExistingViews();
            }
        }

        //---------------------------------------------------------------------
        public bool ShowCoverage
        {
            set
            {
                if (this._showCoverage == value) return;
                this._showCoverage = value;
                this.RemoveHighlightForAllViews();
                if (this._showCoverage)
                    AddHighlightCoverageForExistingViews();
            }
        }

        //---------------------------------------------------------------------
        private void AddHighlightCoverageForExistingViews()
        {
            foreach (var view in this._views) AddNewHighlightCoverage(view, view.TextViewLines);
        }

        //---------------------------------------------------------------------
        private string GetOptionalFilePath(ITextView textView)
        {
            this.TextDocumentFactoryService.TryGetTextDocument(textView.TextBuffer, out var textDocument);

            return textDocument == null ? null : NormalizePath(textDocument.FilePath);
        }

        //---------------------------------------------------------------------
        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (this._showCoverage) AddNewHighlightCoverage((IWpfTextView)sender, e.NewOrReformattedLines);
        }

        //---------------------------------------------------------------------
        private void OnTextViewClosed(object sender, EventArgs e)
        {
            if (!(sender is IWpfTextView textView)) return;
            this._views.Remove(textView);
            textView.Closed -= OnTextViewClosed;
            textView.LayoutChanged -= OnLayoutChanged;

            var handler = this._handlers[textView];
            this._handlers.Remove(textView);

            textView.TextBuffer.Changed -= handler.OnTextChanged;
            VSColorTheme.ThemeChanged -= handler.OnThemeChanged;
        }

        //---------------------------------------------------------------------
        private static string NormalizePath(string path)
        {
            return System.IO.Path.GetFullPath(path).ToLowerInvariant();
        }

        //---------------------------------------------------------------------
        private void AddNewHighlightCoverage(
            IWpfTextView textView,
            IEnumerable<ITextViewLine> textViewLines)
        {
            var viewLines = textViewLines as ITextViewLine[] ?? textViewLines.ToArray();
            if (!viewLines.Any()) return;

            var optionalFilePath = GetOptionalFilePath(textView);

            if (optionalFilePath == null ||
                !this._coverageByFile.TryGetValue(optionalFilePath, out var fileCoverage)) return;

            var coverage = fileCoverage.LineCoverages.ToDictionary(line => line.LineNumber);
            var adornmentLayer = textView.GetAdornmentLayer(HighlightLinesAdornment);

            foreach (var line in viewLines)
            {
                var lineNumber = textView.TextSnapshot.GetLineNumberFromPosition(line.Extent.Start) + 1;

                if (!coverage.TryGetValue(lineNumber, out var lineCoverage)) continue;
                var color = lineCoverage.HasBeenExecuted ? CoveredBrush : UncoveredBrush;

                AddAdornment(adornmentLayer, textView, line, color);
            }
        }

        //---------------------------------------------------------------------
        private void RemoveHighlightForAllViews()
        {
            foreach (var view in this._views)
                RemoveHighlight(view);
        }

        //---------------------------------------------------------------------
        private static void RemoveHighlight(IWpfTextView textView)
        {
            textView.GetAdornmentLayer(HighlightLinesAdornment).RemoveAllAdornments();
        }

        //---------------------------------------------------------------------
        private static void AddAdornment(IAdornmentLayer adornmentLayer, ITextView view, ITextViewLine line, Brush colorBrush)
        {
            var rect = new Rectangle()
            {
                Width = Math.Max(view.ViewportWidth, view.MaxTextRightCoordinate),
                Height = line.Height,
                Fill = colorBrush
            };

            Canvas.SetTop(rect, line.Top);
            Canvas.SetLeft(rect, 0);
            adornmentLayer.AddAdornment(line.Extent, CoverageTag, rect);
        }

        //---------------------------------------------------------------------
        private void OnTextChanged(IWpfTextView textView, TextContentChangedEventArgs e)
        {
            var lineChanged = e.Changes.Sum(c => c.LineCountDelta);
            if (lineChanged == 0) return;
            var optionalFilePath = GetOptionalFilePath(textView);
            if (optionalFilePath != null && this._coverageByFile.Remove(optionalFilePath)) RemoveHighlight(textView);
        }

        //---------------------------------------------------------------------
        private void OnThemeChangedEvent(IWpfTextView textView)
        {
            LoadBrushes();
            RemoveHighlight(textView);
            if (this._showCoverage) AddNewHighlightCoverage(textView, textView.TextViewLines);
        }
    }
}
