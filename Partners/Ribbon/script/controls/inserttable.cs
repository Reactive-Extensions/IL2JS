using System;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Ribbon.Controls
{
    [Import(MemberNameCasing = Casing.Exact)]
    public class InsertTableProperties : ControlProperties
    {
        extern public InsertTableProperties();
        extern public string Alt { get; }
        extern public string CommandPreview { get; }
        extern public string CommandRevert { get; }
        extern public string MenuSectionInitialTitle { get; }
        extern public string MenuSectionTitle { get; }
    }

    public static class InsertTableCommandProperties
    {
        public const string Rows = "Rows";
        public const string Columns = "Columns";
    }

    internal class InsertTable : Control
    {
        const int _maxIdx = 99;

        Div[] _innerDivs;
        Div[] _outerDivs;

        public InsertTable(Root root,
                             string id,
                             InsertTableProperties properties)
            : base(root, id, properties)
        {
            AddDisplayMode("Menu");
        }

        protected override ControlComponent CreateComponentForDisplayModeInternal(string displayMode)
        {
            if (this.Components.Count > 0)
                throw new ArgumentException("Only one ControlComponent can be created for each InsertTable Control");

            ControlComponent comp;
            comp = Root.CreateMenuItem(
                Id + "-" + displayMode + Root.GetUniqueNumber(),
                displayMode,
                this);
            return comp;
        }

        Table _elmDefault;
        TableBody _elmDefaultTbody;
        protected override HtmlElement CreateDOMElementForDisplayMode(string displayMode)
        {
            switch (displayMode)
            {
                case "Menu":
                    _elmDefault = new Table();
                    _elmDefault.SetAttribute("mscui:controltype", ControlType);

                    _elmDefaultTbody = new TableBody();
                    _elmDefaultTbody.ClassName = "ms-cui-it";
                    _elmDefault.SetAttribute("cellspacing", "0");
                    _elmDefault.SetAttribute("cellpadding", "0");
                    _elmDefaultTbody.SetAttribute("cellspacing", "0");
                    _elmDefaultTbody.SetAttribute("cellpadding", "0");

                    _elmDefault.MouseOut += OnControlMouseOut;

                    EnsureDivArrays();

                    TableRow elmRow;
                    TableCell elmCell;
                    Anchor elmCellA;
                    Div elmDiv;
                    Div elmDivOuter;
                    int idx = 0;
                    for (int i = 0; i < 10; i++)
                    {
                        elmRow = new TableRow();
                        _elmDefaultTbody.AppendChild(elmRow);
                        for (int j = 0; j < 10; j++)
                        {
                            elmCell = new TableCell();
                            elmCell.Style.Padding = "0px";
                            elmRow.AppendChild(elmCell);

                            elmCellA = new Anchor();

                            Utility.NoOpLink(elmCellA);
                            Utility.SetAriaTooltipProperties(Properties, elmCellA);

                            elmCellA.Focus += OnCellFocus;
                            elmDiv = new Div();
                            elmDiv.ClassName = "ms-cui-it-inactiveCell";

                            elmDivOuter = new Div();
                            elmDivOuter.Id = this.Id + "-" + idx;
                            elmDivOuter.ClassName = "ms-cui-it-inactiveCellOuter";

                            elmCell.MouseOver += OnCellHover;
                            elmCell.Click += OnCellClick;
                            elmCell.AppendChild(elmDivOuter);
                            elmDivOuter.AppendChild(elmDiv);
                            elmDiv.AppendChild(elmCellA);

                            _innerDivs[idx] = elmDiv;
                            _outerDivs[idx] = elmDivOuter;
                            idx++;
                        }
                    }

                    _elmDefault.AppendChild(_elmDefaultTbody);
                    return _elmDefault;
                default:
                    EnsureValidDisplayMode(displayMode);
                    break;
            }
            return null;
        }

        protected override void ReleaseEventHandlers()
        {
            _elmDefault.MouseOut -= OnControlMouseOut;
            for (int i = 0; i < 10; i++)
            {
                TableRow elmRow = (TableRow)_elmDefault.Rows[0];
                for (int j = 0; j < 10; j++)
                {
                    TableCell elmCell = (TableCell)elmRow.Cells[j];
                    elmCell.Click -= OnCellClick;
                    elmCell.MouseOver -= OnCellHover;

                    Div outerDiv = (Div)elmCell.FirstChild;
                    Div innerDiv = (Div)outerDiv.FirstChild;
                    Anchor elmCellA = (Anchor)innerDiv.FirstChild;
                    elmCellA.Focus -= OnCellFocus;
                }
            }
        }

        public override void OnEnabledChanged(bool enabled)
        {
            // TODO:  change the look of the picker if it is disabled?
        }

        internal override string ControlType
        {
            get
            {
                return "InsertTable";
            }
        }

        private void OnCellClick(HtmlEvent evt)
        {
            if (!CUIUtility.IsNullOrUndefined(typeof(PMetrics)))
                PMetrics.PerfMark(PMarker.perfCUIRibbonInsertTableOnClickStart);

            Utility.CancelEventUtility(evt, false, true);
            if (!Enabled)
                return;

            Div element = GetOuterDiv(evt.TargetElement);

            int idx = GetIndexFromElement(element);
            int column = GetColFromIndex(idx);
            int row = GetRowFromIndex(idx);

            // We don't want to send a preview revert command since the user has picked one
            // by clicking.
            CancelClickPreviewRevert();

            CommandProperties[InsertTableCommandProperties.Rows] = (row + 1).ToString();
            CommandProperties[InsertTableCommandProperties.Columns] = (column + 1).ToString();
            DisplayedComponent.RaiseCommandEvent(Properties.Command,
                                                 CommandType.General,
                                                 CommandProperties);

            if (!CUIUtility.IsNullOrUndefined(typeof(PMetrics)))
                PMetrics.PerfMark(PMarker.perfCUIRibbonInsertTableOnClickEnd);
        }

        /// <summary>
        /// We need this function because though we have the events on the <td> node,
        /// they can return a sub node (one of the divs for example) as the event.Target
        /// This function will find the outer element no mater what element in in 
        /// event.Target.
        /// </summary>
        /// <param name="elm"></param>
        /// <param name="goUp"></param>
        /// <param name="goDown"></param>
        /// <returns></returns>
        private Div GetOuterDiv(HtmlElement elm)
        {
            // Walk down until we get to the "A" tag.
            while (elm.HasChildNodes())
                elm = (HtmlElement)elm.ChildNodes[0];

            // Now "elm" has the "A" tag in it.  We can then get the outer div
            // from it.
            return (Div)elm.ParentNode.ParentNode;
        }

        private void SetTitleOnAnchor(Div elmOuterDiv)
        {
            Anchor elm = (Anchor)elmOuterDiv.ChildNodes[0].ChildNodes[0];
            int idx = GetIndexFromElement(elmOuterDiv);
            elm.Title = GetCellTitle(GetRowFromIndex(idx) + 1, GetColFromIndex(idx) + 1);
        }

        private string GetCellTitle(int row, int column)
        {
            string title = Properties.Alt;
            if (string.IsNullOrEmpty(title))
                title = CUIUtility.SafeString(Properties.MenuSectionTitle);
            title = String.Format(title, column.ToString(), row.ToString());
            return title;
        }

        private void OnCellHover(HtmlEvent args)
        {
            if (!Enabled)
                return;

            // Simulate a focus on the anchor of this cell to get the highlighting behavior
            // evt.Target.FirstChild.Focus();
            Div elmOuterDiv = GetOuterDiv(args.TargetElement);
            SetTitleOnAnchor(elmOuterDiv);
            HandleHighlightingTitleAndPreview(elmOuterDiv);
        }

        private void OnCellFocus(HtmlEvent args)
        {
            if (!Enabled)
                return;

            Div elmOuterDiv = GetOuterDiv(args.TargetElement);

            SetTitleOnAnchor(elmOuterDiv);
            HandleHighlightingTitleAndPreview(elmOuterDiv);
        }

        private void OnControlMouseOut(HtmlEvent evt)
        {
            // In this case, the mousecursor has been moved outside the insert table control
            // and we want to reset it to its initial state with nothing selected in the grid.
            Bounds bounds = Utility.GetElementPosition(_elmDefault);
            if (evt.ClientX <= bounds.X ||
                evt.ClientX >= bounds.X + bounds.Width ||
                evt.ClientY <= bounds.Y ||
                evt.ClientY >= bounds.Y + bounds.Height)
            {
                ResetAll();
            }
        }

        private void ResetAll()
        {
            UnselectAll();
            EnsureClickPreviewReverted();

            prevCol = -1;
            prevRow = -1;
            previewClickCommandSent = false;
        }

        private int GetIndexFromElement(Div element)
        {
            return Int32.Parse(element.Id.Substring(this.Id.Length + 1));
        }

        int _oldIdx = -1;
        bool previewClickCommandSent = false;
        int prevCol = -1;
        int prevRow = -1;
        private void HandleHighlightingTitleAndPreview(Div outerDiv)
        {
            int idx = GetIndexFromElement(outerDiv);

            // If the cell with the focus has not changed since the last time then there
            // nothing to change.
            if (_oldIdx == idx)
                return;

            HandleHighlightingTitleAndPreviewForIndex(idx);
        }

        private void HandleHighlightingTitleAndPreviewForIndex(int idx)
        {
            // Adjust the highliting
            AdjustHighlightingAndTitle(idx);

            // Save the new index
            _oldIdx = idx;

            // Make sure that we revert the old preview command
            EnsureClickPreviewReverted();
            SendClickPreviewCommand(idx);
        }

        private int GetRowFromIndex(int idx)
        {
            return Int32.Parse((Math.Floor(idx / 10)).ToString());
        }

        private int GetColFromIndex(int idx)
        {
            return idx % 10;
        }

        private void AdjustHighlightingAndTitle(int idx)
        {
            int column = GetColFromIndex(idx);
            int row = GetRowFromIndex(idx);
            int currRow = -1;
            int currCol = -1;
            if (_oldIdx != -1)
            {
                currRow = GetRowFromIndex(_oldIdx);
                currCol = GetColFromIndex(_oldIdx);
            }

            // Now "walk" from the old location to the new one
            while (currRow != row || currCol != column)
            {
                if (currRow < row)
                {
                    SetRowHighlighting(++currRow, currCol, true);
                }
                else if (currRow > row)
                {
                    SetRowHighlighting(currRow, currCol, false);
                    currRow--;
                }
                else if (currCol < column)
                {
                    SetColHighlighting(++currCol, currRow, true);
                }
                else if (currCol > column)
                {
                    SetColHighlighting(currCol, currRow, false);
                    currCol--;
                }
            }

            HostMenuSection.SetTitleImmediate(GetCellTitle(row + 1, column + 1));
        }

        private void SetRowHighlighting(int row, int column, bool on)
        {
            for (int i = 0; i <= column; i++)
            {
                SetCellHighlighting(row, i, on);
            }
        }

        private void SetColHighlighting(int column, int row, bool on)
        {
            for (int i = 0; i <= row; i++)
            {
                SetCellHighlighting(i, column, on);
            }
        }

        private void SetCellHighlighting(int row, int column, bool on)
        {
            int idx = row * 10 + column;
            Div inner = _innerDivs[idx];
            Div outer = _outerDivs[idx];

            if (on)
            {
                inner.ClassName = "ms-cui-it-activeCell";
                outer.ClassName = "ms-cui-it-activeCellOuter";
            }
            else
            {
                inner.ClassName = "ms-cui-it-inactiveCell";
                outer.ClassName = "ms-cui-it-inactiveCellOuter";
            }
        }

        private void UnselectAll()
        {
            for (int i = 0; i < 100; i++)
            {
                _innerDivs[i].ClassName = "ms-cui-it-inactiveCell";
                _outerDivs[i].ClassName = "ms-cui-it-inactiveCellOuter";
            }

            _oldIdx = -1;
            string title = CUIUtility.SafeString(Properties.MenuSectionInitialTitle);

            HostMenuSection.SetTitleImmediate(title);
        }

        private void EnsureDivArrays()
        {
            if (CUIUtility.IsNullOrUndefined(_innerDivs))
                _innerDivs = new Div[100];

            if (CUIUtility.IsNullOrUndefined(_outerDivs))
                _outerDivs = new Div[100];
        }

        public override void ReceiveFocus()
        {
            Div elmDivInner = _innerDivs[0];
            if (CUIUtility.IsNullOrUndefined(elmDivInner))
            {
                return;
            }

            // The parent node of this is an anchor tag and we want to focus on it
            ((HtmlElement)elmDivInner.FirstChild).PerformFocus();
        }

        private void CancelClickPreviewRevert()
        {
            prevCol = -1;
            prevRow = -1;
            previewClickCommandSent = false;
        }

        private void EnsureClickPreviewReverted()
        {
            if (previewClickCommandSent)
            {
                string revClkCmd = Properties.CommandRevert;
                if (!string.IsNullOrEmpty(revClkCmd))
                {
                    CommandProperties[InsertTableCommandProperties.Rows] = (prevRow + 1).ToString();
                    CommandProperties[InsertTableCommandProperties.Columns] = (prevCol + 1).ToString();
                    DisplayedComponent.RaiseCommandEvent(revClkCmd,
                                                         CommandType.PreviewRevert,
                                                         CommandProperties);
                }
                CancelClickPreviewRevert();
            }
        }

        private void SendClickPreviewCommand(int idx)
        {
            // Store the fact that we sent out a live preview command
            // and its coordinates
            prevCol = GetColFromIndex(idx);
            prevRow = GetRowFromIndex(idx);
            previewClickCommandSent = true;

            string prevClkCmd = Properties.CommandPreview;
            if (!string.IsNullOrEmpty(prevClkCmd))
            {
                CommandProperties[InsertTableCommandProperties.Rows] = (prevRow + 1).ToString();
                CommandProperties[InsertTableCommandProperties.Columns] = (prevCol + 1).ToString();
                DisplayedComponent.RaiseCommandEvent(prevClkCmd,
                                                     CommandType.Preview,
                                                     CommandProperties);
            }
        }

        private MenuSection HostMenuSection
        {
            get
            {
                Component parentComp = DisplayedComponent.Parent;
                if (!(parentComp is MenuSection))
                    throw new InvalidOperationException("InsertTable must live inside of a MenuSection.");
                return (MenuSection)parentComp;
            }
        }

        public override void OnMenuClosed()
        {
            ResetAll();
        }

        internal override bool FocusNext(HtmlEvent evt)
        {
            // If we are at the last square in the grid, then we can't focus within
            // the control any more so we return false
            if (_oldIdx == _maxIdx)
            {
                ResetAll();
                return false;
            }

            ((HtmlElement)_innerDivs[_oldIdx + 1].FirstChild).PerformFocus();
            return true;
        }

        internal override bool FocusPrevious(HtmlEvent evt)
        {
            // If we are currently at the first cell, then we can no longer go backwards
            if (_oldIdx == 0)
            {
                ResetAll();
                return false;
            }
            else if (_oldIdx == -1)
            {
                ((HtmlElement)_innerDivs[_maxIdx].FirstChild).PerformFocus();
            }
            else
            {
                ((HtmlElement)_innerDivs[_oldIdx - 1].FirstChild).PerformFocus();
            }

            return true;
        }

        public override void Dispose()
        {
            base.Dispose();
            _elmDefault = null;
            _elmDefaultTbody = null;
        }

        private InsertTableProperties Properties
        {
            get
            {
                return (InsertTableProperties)base.ControlProperties;
            }
        }
    }
}