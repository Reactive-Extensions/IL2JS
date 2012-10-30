using System;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;
using Ribbon.Controls;

namespace Ribbon
{
    /// <summary>
    /// The dimensions of the rectangular elements in a Gallery.
    /// </summary>
    public enum GalleryElementDimensions
    {
        /// <summary>
        /// A 16x16 gallery element
        /// </summary>
        Size16by16 = 1,
        /// <summary>
        /// A 32x32 gallery element
        /// </summary>
        Size32by32 = 2,
        /// <summary>
        /// A 48x48 gallery element
        /// </summary>
        Size48by48 = 3,
        /// <summary>
        /// A 64x48 gallery element
        /// </summary>
        Size64by48 = 4,
        /// <summary>
        /// A 72x96 gallery element
        /// </summary>
        Size72by96 = 5,
        /// <summary>
        /// A 96x72 gallery element
        /// </summary>
        Size96by72 = 6,
        /// <summary>
        /// A 96x96 gallery element
        /// </summary>
        Size96by96 = 7,
        /// <summary>
        /// A 128x128 gallery element
        /// </summary>
        Size128by128 = 8,
        /// <summary>
        /// A 190x30 gallery element
        /// </summary>
        Size190by30 = 9,
        /// <summary>
        /// A 190x40 gallery element
        /// </summary>
        Size190by40 = 10,
        /// <summary>
        /// A 190x50 gallery element
        /// </summary>
        Size190by50 = 11,
        /// <summary>
        /// A 190x60 gallery element
        /// </summary>
        Size190by60 = 12

        // NOTE: If you are adding a value here, you must define it in
        // GalleryElementDimensionsToSizeString in Utility.cs as well
    }

    /// <summary>
    /// A class that represents the properties of a Gallery
    /// </summary>
    [Import(MemberNameCasing = Casing.Exact)]
    public class GalleryProperties : ControlProperties
    {
        extern public GalleryProperties();
        extern public string Width { get; }
        extern public string ElementDimensions { get; }
        extern public string CommandPreview { get; }
        extern public string CommandRevert { get; }
    }

    /// <summary>
    /// A Component that is a n by m grid of rectangles that can be placed in menus.
    /// </summary>
    internal class Gallery : Component
    {
        GalleryProperties _prop;
        GalleryElementDimensions _elmDim;
        int _width;

        // Focus system variables
        int _focusedIndex = -1;
        ISelectableControl _selectedItem = null;

        /// <summary>
        /// Creates a Gallery
        /// </summary>
        /// <param name="root">The Ribbon that this Gallery was created by and is a part of.</param>
        /// <param name="id">The Component id of this Gallery.</param>
        /// <param name="title">The Title of this Gallery.</param>
        /// <param name="description">The Description of this Gallery</param>
        /// <param name="properties">The properties of this Gallery</param>
        public Gallery(Root root, string id, string title, string description, GalleryProperties properties)
            : base(root, id, title, description)
        {
            Properties = properties;
            Width = Int32.Parse(Properties.Width);
            ElementDimensions = Gallery.ConvertStringToGalleryElementDimensions(Properties.ElementDimensions);
        }

        #region Component Overrides
        protected override string DOMElementTagName
        {
            get
            {
                return "table";
            }
        }

        protected override string CssClass
        {
            get
            {
                return "ms-cui-gallery";
            }
        }

        internal override void RefreshInternal()
        {
            EnsureDOMElementAndEmpty();
            TableBody tbody = new TableBody();
            ElementInternal.AppendChild(tbody);
            AppendChildrenToElement(tbody);
        }

        protected override void AppendChildrenToElement(HtmlElement elm)
        {
            int rows = Int32.Parse((Math.Ceiling((Double)Children.Count / (Double)Width)).ToString());

            // Create grid structure and put child elements in it
            TableRow tr;
            TableCell td;
            Component child;
            int c = 0;
            for (int i = 0; i < rows; i++)
            {
                tr = new TableRow();
                for (int j = 0; j < Width; j++)
                {
                    td = new TableCell();
                    td.ClassName = "ms-cui-gallery-td ms-cui-gallery-element-" + ElementDimensions.ToString();

                    // Insert empty cells to finish out row when out of children
                    if (c < Children.Count)
                    {
                        child = (Component)Children[c++];
                        child.EnsureDOMElement();
                        td.AppendChild(child.ElementInternal);
                        child.EnsureRefreshed();
                    }
                    tr.AppendChild(td);
                }
                elm.AppendChild(tr);
            }
        }

        bool _processingCommand = false;
        internal override bool OnPreBubbleCommand(CommandEventArgs command)
        {
            // If we have already processed the command and issued our own instead,
            // then we do not want to infinitely recurse by issueing another one.
            if (_processingCommand)
                return true;
            if (command.Type == CommandType.OptionSelection)
            {
                MenuItem item = (MenuItem)command.Source;
                if (!(item.Control is ISelectableControl))
                    return base.OnPreBubbleCommand(command);
                ISelectableControl isc = (ISelectableControl)item.Control;

                // If an item is currently selected, deselect it first
                if (!CUIUtility.IsNullOrUndefined(_selectedItem))
                    _selectedItem.Deselect();
                _selectedItem = isc;
            }

            if (command.Type == CommandType.OptionSelection
                || command.Type == CommandType.OptionPreview
                || command.Type == CommandType.OptionPreviewRevert)
            {
                string myCommand;
                switch (command.Type)
                {
                    case CommandType.OptionSelection:
                        myCommand = Properties.Command;
                        break;
                    case CommandType.OptionPreview:
                        myCommand = Properties.CommandPreview;
                        break;
                    case CommandType.OptionPreviewRevert:
                        myCommand = Properties.CommandRevert;
                        break;
                    default:
                        // This case should not be hit, but it allows compilation
                        myCommand = Properties.Command;
                        break;
                }

                // Keep track of the fact that we have already processed the command
                // so that we will not infinitely recurse.
                _processingCommand = true;
                // Stop the command here and send our own
                RaiseCommandEvent(myCommand,
                                  command.Type,
                                  command.Parameters);
                _processingCommand = false;
                base.OnPreBubbleCommand(command);
                return false;
            }

            return base.OnPreBubbleCommand(command);
        }

        #region Focus Methods
        internal override void ResetFocusedIndex()
        {
            if (Children.Count == 0)
                return;
            _focusedIndex = 0;
            foreach (Component c in Children)
            {
                c.ResetFocusedIndex();
            }
        }

        internal override void FocusOnFirstItem(HtmlEvent evt)
        {
            if (Children.Count == 0)
                return;

            if (_focusedIndex > -1)
                ((Component)Children[_focusedIndex]).ResetFocusedIndex();
            _focusedIndex = 0;
            ((Component)Children[_focusedIndex]).FocusOnFirstItem(evt);
        }

        internal override void FocusOnLastItem(HtmlEvent evt)
        {
            int count = Children.Count;

            if (count == 0)
                return;

            if (_focusedIndex > -1)
                ((Component)Children[_focusedIndex]).ResetFocusedIndex();
            _focusedIndex = count - 1;
            ((Component)Children[_focusedIndex]).FocusOnLastItem(evt);
        }

        internal override bool FocusOnItemById(string menuItemId)
        {
            if (Children.Count == 0)
                return false;

            int i = 0;
            foreach (Component c in Children)
            {
                if (c.FocusOnItemById(menuItemId))
                {
                    if (_focusedIndex > -1)
                        ((Component)Children[_focusedIndex]).ResetFocusedIndex();
                    _focusedIndex = i;
                    return true;
                }
                i++;
            }
            return false;
        }

        internal override bool FocusPrevious(HtmlEvent evt)
        {
            if (_focusedIndex == -1)
                _focusedIndex = Children.Count - 1;

            int i = _focusedIndex;
            while (i > -1)
            {
                Component comp = Children[i];

                if (comp.FocusPrevious(evt))
                {
                    // If focus is not moving, don't reset the focus of the gallery item
                    if (i != _focusedIndex)
                    {
                        ((Component)Children[_focusedIndex]).ResetFocusedIndex();
                        _focusedIndex = i;
                    }
                    return true;
                }
                i--;
            }
            ((Component)Children[_focusedIndex]).ResetFocusedIndex();
            _focusedIndex = -1;
            return false;
        }

        internal override bool FocusNext(HtmlEvent evt)
        {
            if (_focusedIndex == -1)
                _focusedIndex = 0;

            int i = _focusedIndex;
            while (i < Children.Count)
            {
                Component comp = Children[i];

                if (comp.FocusNext(evt))
                {
                    // If focus is not moving, don't reset the focus of the gallery item
                    if (i != _focusedIndex)
                    {
                        ((Component)Children[_focusedIndex]).ResetFocusedIndex();
                        _focusedIndex = i;
                    }
                    return true;
                }
                i++;
            }
            ((Component)Children[_focusedIndex]).ResetFocusedIndex();
            _focusedIndex = -1;
            return false;
        }
        #endregion

        protected override void EnsureCorrectChildType(Component child)
        {
            if (!typeof(ControlComponent).IsInstanceOfType(child))
                throw new ArgumentException("Galleries can only have children controls of type GalleryButton");
            ControlComponent cc = (ControlComponent)child;
            if (!typeof(GalleryButton).IsInstanceOfType(cc.Control))
                throw new ArgumentException("Galleries can only have children of type GalleryButton");
        }
        #endregion

        internal Table TableElementInternal
        {
            get
            {
                return (Table)ElementInternal;
            }
            set
            {
                ElementInternal = value;
            }
        }

        protected GalleryElementDimensions ElementDimensions
        {
            get
            {
                return _elmDim;
            }
            set
            {
                _elmDim = value;
            }
        }

        protected int Width
        {
            get
            {
                return _width;
            }
            set
            {
                _width = value;
            }
        }

        private GalleryProperties Properties
        {
            get
            {
                return _prop;
            }
            set
            {
                _prop = value;
            }
        }

        /// <summary>
        /// Converts a string into a GalleryElementDimensions value
        /// </summary>
        /// <param name="s">The string to convert</param>
        /// <returns>A GalleryElementDimensions variable with the value dictated by the string parameter</returns>
        internal static GalleryElementDimensions ConvertStringToGalleryElementDimensions(string s)
        {
            switch (s)
            {
                case "Size16by16":
                    return GalleryElementDimensions.Size16by16;
                case "Size32by32":
                    return GalleryElementDimensions.Size32by32;
                case "Size48by48":
                    return GalleryElementDimensions.Size48by48;
                case "Size64by48":
                    return GalleryElementDimensions.Size64by48;
                case "Size72by96":
                    return GalleryElementDimensions.Size72by96;
                case "Size96by72":
                    return GalleryElementDimensions.Size96by72;
                case "Size96by96":
                    return GalleryElementDimensions.Size96by96;
                case "Size128by128":
                    return GalleryElementDimensions.Size128by128;
                case "Size190by30":
                    return GalleryElementDimensions.Size190by30;
                case "Size190by40":
                    return GalleryElementDimensions.Size190by40;
                case "Size190by50":
                    return GalleryElementDimensions.Size190by50;
                case "Size190by60":
                    return GalleryElementDimensions.Size190by60;
                default:
                    throw new ArgumentException("s", "The parameter s is not a valid GalleryElementDimension");
            }
        }
    }
}