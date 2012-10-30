using System;
using System.Collections.Generic;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Ribbon.Controls
{
    public sealed class ColorStyle
    {
        public string Title;
        public string Color;
        public string Style;
        public string DisplayColor;
    }

    /// <summary>
    /// Color picker properties
    /// </summary>
    [Import(MemberNameCasing = Casing.Exact)]
    public class ColorPickerProperties : ControlProperties
    {
        extern public ColorPickerProperties();
        extern public string CommandPreview { get; }
        extern public string CommandRevert { get; }
    }

    public static class ColorPickerCommandProperties
    {
        public const string Color = "Color";
        public const string Style = "Style";
    }


    /// <summary>
    /// Color picker result
    /// </summary>
    public sealed class ColorPickerResult
    {
        public string Color;
        public string Style;
    }

    /// <summary>
    /// Color picker drop down menu.
    /// </summary>
    internal class ColorPicker : Control, IMenuItem
    {
        private const string ColorInformation = "colorPickerCssClass";
        private const string NormalCellCssClassName = "ms-cui-colorpicker-cell";
        private const string CellAnchorCssClassName = "ms-cui-colorpicker-cell-a";
        private const string CellDivClassName = "ms-cui-colorpicker-celldiv";
        private const string CellInternalDivClassName = "ms-cui-colorpicker-cellinternaldiv";
        private const string SelectedCellCssClassName = "ms-cui-colorpicker-hoveredOver";
        private const int DefaultCellHeight = 13;

        private Table _colorTable;
        private ColorStyle[] _colors;

        public ColorPicker(Root root,
                           string id,
                           ColorPickerProperties properties,
                           ColorStyle[] colors)
            : base(root, id, properties)
        {
            AddDisplayMode("Menu");

            _colors = colors;
            _colorTable = new Table();
        }

        protected override ControlComponent CreateComponentForDisplayModeInternal(string displayMode)
        {
            if (this.Components.Count > 0)
                throw new InvalidOperationException("Only one ControlComponent can be created for each ColorPicker Control");
            ControlComponent comp;
            comp = Root.CreateMenuItem(
                Id + "-" + displayMode + Root.GetUniqueNumber(),
                displayMode,
                this);
            return comp;
        }

        internal override string ControlType
        {
            get
            {
                return "ColorPicker";
            }
        }

        private List<HtmlElement> _colorCells;

        private const int ColumnSize = 10;

        /// <summary>
        /// Create some rows or cells in _colorTable
        /// </summary>
        /// <param name="colorRules"></param>
        /// <param name="styleName"></param>
        private void AddColorCells(HtmlElement colorTableBody, ColorStyle[] colorRules)
        {
            int rowNumber = 0;
            TableRow row = new TableRow();
            int totalRows = colorRules.Length / ColumnSize;

            for (int i = 0; i < colorRules.Length; i++)
            {
                if ((i % ColumnSize) == 0)
                {
                    row = new TableRow();
                    colorTableBody.AppendChild(row);
                    rowNumber++;
                }

                TableCell cell = new TableCell();
                cell.ClassName = NormalCellCssClassName;
                cell.SetAttribute("arrayPosition", i.ToString());
                // Make the first row have spacing.
                if (rowNumber == 1)
                {
                    cell.Style.Padding = "2px";
                    cell.Style.Height = "16px";
                }

                row.AppendChild(cell);
                Anchor link = new Anchor();
                link.Href = "javascript:";
                string displayName = colorRules[i].Title;
                link.Title = displayName;
                link.ClassName = CellAnchorCssClassName;

                link.Focus += OnCellFocus;
                Div elmDiv = new Div();
                string color = colorRules[i].DisplayColor;
                elmDiv.Style.BackgroundColor = color;
                elmDiv.ClassName = CellDivClassName;
                int size = DefaultCellHeight;
                if (rowNumber == 1 || rowNumber == 2)
                {
                    elmDiv.Style.BorderTopWidth = "1px";
                    size--;
                }
                if (rowNumber == 1 || rowNumber == totalRows)
                {
                    elmDiv.Style.BorderBottomWidth = "1px";
                    size--;
                }
                if (size != DefaultCellHeight)
                {
                    elmDiv.Style.Height = size + "px";
                }

                Div internalelmDiv = new Div();
                internalelmDiv.ClassName = CellInternalDivClassName;

                link.MouseOver += OnCellHover;
                link.MouseOut += OnCellBlur;
                link.Click += OnCellClick;

                cell.AppendChild(link);
                link.AppendChild(elmDiv);
                elmDiv.AppendChild(internalelmDiv);

                cell.SetAttribute(ColorInformation + "Color", colorRules[i].Color);
                cell.SetAttribute(ColorInformation + "Style", colorRules[i].Style);

                // add the cell to _colorCells so that we could reset the highlight
                _colorCells.Add(cell);
            }
        }

        protected override void ReleaseEventHandlers()
        {
            TableBody tBody = (TableBody)_colorTable.TBodies[0];
            foreach (TableRow tRow in tBody.Rows)
            {
                foreach (TableCell tCell in tRow.Cells)
                {
                    Anchor link = (Anchor)tCell.FirstChild;
                    link.Focus -= OnCellFocus;
                    link.MouseOver -= OnCellHover;
                    link.MouseOut -= OnCellBlur;
                    link.Click -= OnCellClick;
                }
            }
        }

        protected override HtmlElement CreateDOMElementForDisplayMode(string displayMode)
        {
            switch (displayMode)
            {
                case "Menu":
                    if (Root.TextDirection == Direction.RTL)
                    {
                        _colorTable.Dir = "rtl";
                    }
                    else
                    {
                        _colorTable.Dir = "ltr";
                    }

                    _colorTable.ClassName = "ms-cui-smenu-inner";
                    _colorTable.SetAttribute("cellSpacing", "0");
                    _colorTable.SetAttribute("cellPadding", "0");
                    _colorTable.SetAttribute("mscui:controltype", ControlType);

                    TableBody colorTableBody = new TableBody();
                    AddColorCells(colorTableBody, _colors);
                    _colorTable.AppendChild(colorTableBody);

                    return _colorTable;
                default:
                    EnsureValidDisplayMode(displayMode);
                    break;
            }
            return null;
        }

        public override void OnEnabledChanged(bool enabled)
        {
        }

        private void OnCellClick(HtmlEvent args)
        {
            Utility.CancelEventUtility(args, false, true);
            if (!Enabled)
                return;

            HtmlElement element = args.TargetElement;

            HtmlElement cell = Utility.GetNearestContainingParentElementOfType(element, "td");

            ColorPickerResult result = GetColorPickerResultFromSelectedCell(cell);
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict[ColorPickerCommandProperties.Color] = result.Color;
            dict[ColorPickerCommandProperties.Style] = result.Style;

            // We don't want to send a preview revert command since the user has picked one
            // by clicking.
            CancelClickPreviewRevert();
            DisplayedComponent.RaiseCommandEvent(Properties.Command,
                                                 CommandType.General,
                                                 dict);
        }

        private ColorPickerResult GetColorPickerResultFromSelectedCell(HtmlElement cell)
        {
            ColorStyle style = (ColorStyle)((JSObject)(object)cell).GetField<object>(ColorInformation);

            ColorPickerResult result = new ColorPickerResult();
            result.Color = cell.GetAttribute(ColorInformation + "Color");
            result.Style = cell.GetAttribute(ColorInformation + "Style");
            return result;
        }

        private void OnCellHover(HtmlEvent args)
        {
            if (!Enabled)
                return;

            // Simulate a focus on the anchor of this cell to get the highlighting behavior
            HtmlElement cell = Utility.GetNearestContainingParentElementOfType(args.TargetElement, "td");
            HandleHighlightingAndPreview(cell);
        }

        private void OnCellFocus(HtmlEvent args)
        {
            if (!Enabled)
                return;
            
            HtmlElement cell = Utility.GetNearestContainingParentElementOfType(args.TargetElement, "td");
            HandleHighlightingAndPreview(cell);
        }

        private void OnCellBlur(HtmlEvent args)
        {
            if (!Enabled)
                return;

            RemoveHighlighting();
            _focusedIndex = -ColumnSize;
        }

        bool previewClickCommandSent = false;
        ColorPickerResult previewPickerResult = null;
        private void HandleHighlightingAndPreview(HtmlElement cell)
        {
            int newIndex = Int32.Parse(cell.GetAttribute("arrayPosition"));
            // If the cell with the focus has not changed since the last time then there
            // nothing to change.
            if (_focusedIndex == newIndex)
            {
                return;
            }

            // Save the new index
            _focusedIndex = newIndex;

            // We should not revert preview.
            SendClickPreviewCommand(cell);

            // Adjust the highliting
            AdjustHighlighting(cell);
        }

        private void AdjustHighlighting(HtmlElement cell)
        {
            RemoveHighlighting();
            Utility.EnsureCSSClassOnElement(cell, SelectedCellCssClassName);
            highlightedElement = cell;
            if (cell.FirstChild != null)
            {
                ((HtmlElement)cell.FirstChild).PerformFocus();
            }
        }


        private HtmlElement highlightedElement;
        private void RemoveHighlighting()
        {
            if (highlightedElement != null)
            {
                highlightedElement.ClassName = NormalCellCssClassName;
            }
        }

        public override void ReceiveFocus()
        {
            SetFocusOnCell(0);
        }

        private void SetFocusOnCell(int i)
        {
            if (_colorCells != null && _colorCells.Count > i)
            {
                HtmlElement cell = _colorCells[i];
                HandleHighlightingAndPreview(cell);
            }
        }

        private void CancelClickPreviewRevert()
        {
            previewClickCommandSent = false;
            previewPickerResult = null;
        }

        private void EnsureClickPreviewReverted()
        {
            if (previewClickCommandSent && !string.IsNullOrEmpty(Properties.CommandRevert))
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                dict[ColorPickerCommandProperties.Color] = previewPickerResult.Color;
                dict[ColorPickerCommandProperties.Style] = previewPickerResult.Style;

                DisplayedComponent.RaiseCommandEvent(Properties.CommandRevert,
                                                     CommandType.PreviewRevert,
                                                     dict);
            }
        }

        private void SendClickPreviewCommand(HtmlElement cell)
        {
            if (!string.IsNullOrEmpty(Properties.CommandPreview))
            {
                // Store the fact that we sent out a live preview command
                // and its coordinates
                previewClickCommandSent = true;

                ColorPickerResult result = GetColorPickerResultFromSelectedCell(cell);
                Dictionary<string, string> dict = new Dictionary<string, string>();
                dict[ColorPickerCommandProperties.Color] = result.Color;
                dict[ColorPickerCommandProperties.Style] = result.Style;

                previewPickerResult = result;

                DisplayedComponent.RaiseCommandEvent(Properties.CommandPreview,
                                                     CommandType.Preview,
                                                     dict);
            }
        }

        private ColorPickerProperties Properties
        {
            get
            {
                return (ColorPickerProperties)base.ControlProperties;
            }
        }

        public override void OnMenuClosed()
        {
            EnsureClickPreviewReverted();
        }

        private static int _focusedIndex = -ColumnSize;

        internal override bool FocusPrevious(HtmlEvent evt)
        {
            int moveCount = 1;
            if (!CUIUtility.IsNullOrUndefined(evt) && evt.KeyCode == (int)Key.Up)
            {
                moveCount = ColumnSize;
            }


            if (_focusedIndex < 0)
            {
                _focusedIndex += _colorCells.Count + moveCount;
            }

            if (_focusedIndex >= moveCount)
            {
                SetFocusOnCell(_focusedIndex - moveCount);
                return true;
            }

            RemoveHighlighting();
            _focusedIndex -= moveCount;
            return false;
        }


        internal override bool FocusNext(HtmlEvent evt)
        {
            int moveCount = 1;
            if (!CUIUtility.IsNullOrUndefined(evt) && evt.KeyCode == (int)Key.Down)
            {
                moveCount = ColumnSize;
            }

            if (_focusedIndex + moveCount < 0)
            {
                _focusedIndex = -1;
                moveCount = 1;
            }


            if (_focusedIndex + moveCount < _colorCells.Count)
            {
                SetFocusOnCell(_focusedIndex + moveCount);
                return true;
            }
            RemoveHighlighting();
            _focusedIndex -= _colorCells.Count;
            return false;
        }

    }
}
