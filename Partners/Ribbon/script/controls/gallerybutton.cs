using System;
using System.Collections.Generic;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Ribbon.Controls
{
    [Import(MemberNameCasing = Casing.Exact)]
    public class GalleryButtonProperties : MenuItemControlProperties
    {
        extern public GalleryButtonProperties();
        extern public string Alt { get; }
        extern public string CommandPreview { get; }
        extern public string CommandRevert { get; }
        extern public string CommandType { get; }
        extern public string Image { get; }
        extern public string ImageClass { get; }
        extern public string ImageTop { get; }
        extern public string ImageLeft { get; }
        extern public string InnerHTML { get; }
        extern public string QueryCommand { get; }
    }

    /// <summary>
    /// The properties that can be set via polling on a Gallery Button
    /// </summary>
    public static class GalleryButtonCommandProperties
    {
        public static string On = "On";
        public static string CommandValueId = "CommandValueId";
    }

    /// <summary>
    /// A class that represents a GalleryButton control
    /// </summary>
    internal class GalleryButton : Control, ISelectableControl
    {
        Span _elmDefault;
        Anchor _elmDefaultA;
        Image _elmDefaultImage;
        Span _elmDefaultImageCont;
        GalleryElementDimensions _dims;

        /// <summary>
        /// Creates a GalleryButton control.
        /// </summary>
        /// <param name="root">The Root that this control is in</param>
        /// <param name="id">The ID of this control</param>
        /// <param name="properties">The properties of this control</param>
        /// <param name="dims">The dimensions of this control</param>
        public GalleryButton(Root root, string id, GalleryButtonProperties properties, GalleryElementDimensions dims)
            : base(root, id, properties)
        {
            AddDisplayMode("Large");
            AddDisplayMode("Menu");
            ElementDimensions = dims;
        }

        protected override ControlComponent CreateComponentForDisplayModeInternal(string displayMode)
        {
            ControlComponent comp;
            if (displayMode == "Menu")
            {
                comp = Root.CreateMenuItem(
                    Id + "-" + displayMode + Root.GetUniqueNumber(),
                    displayMode,
                    this);
                if (string.IsNullOrEmpty(Properties.CommandValueId))
                    Properties.CommandValueId = Properties.MenuItemId;
            }
            else
            {
                comp = base.CreateComponentForDisplayModeInternal(displayMode);
            }
            return comp;
        }

        protected override HtmlElement CreateDOMElementForDisplayMode(string displayMode)
        {
            switch (displayMode)
            {
                case "Large":
                case "Menu":
                    string alt = CUIUtility.SafeString(Properties.Alt);
                    // Create elements
                    _elmDefault = new Span();
                    _elmDefault.SetAttribute("mscui:controltype", ControlType);
                    _elmDefault.ClassName = "ms-cui-gallerybutton ms-cui-gallerybutton-" + Utility.GalleryElementDimensionsToSizeString[(int)ElementDimensions];

                    _elmDefaultA = new Anchor();
                    _elmDefaultA.Title = alt;
                    _elmDefaultA.ClassName = "ms-cui-gallerybutton-a";
                    Utility.NoOpLink(_elmDefaultA);
                    Utility.SetAriaTooltipProperties(Properties, _elmDefaultA);
                    _elmDefault.AppendChild(_elmDefaultA);

                    if (!string.IsNullOrEmpty(Properties.InnerHTML))
                    {
                        _elmDefaultA.InnerHtml = Properties.InnerHTML;
                        Utility.SetUnselectable(_elmDefaultA, true, true);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(Properties.Image))
                        {
                            throw new ArgumentNullException("InnerHTML or Image must be defined for this GalleryButton");
                        }

                        ImgContainerSize size = ImgContainerSize.Size32by32;
                        switch (ElementDimensions)
                        {
                            case GalleryElementDimensions.Size16by16:
                                size = ImgContainerSize.Size16by16;
                                break;
                            case GalleryElementDimensions.Size32by32:
                                size = ImgContainerSize.Size32by32;
                                break;
                            case GalleryElementDimensions.Size48by48:
                                size = ImgContainerSize.Size48by48;
                                break;
                            case GalleryElementDimensions.Size64by48:
                                size = ImgContainerSize.Size64by48;
                                break;
                            case GalleryElementDimensions.Size72by96:
                                size = ImgContainerSize.Size72by96;
                                break;
                            case GalleryElementDimensions.Size96by72:
                                size = ImgContainerSize.Size96by72;
                                break;
                            case GalleryElementDimensions.Size96by96:
                                size = ImgContainerSize.Size96by96;
                                break;
                        }

                        _elmDefaultImage = new Image();
                        _elmDefaultImageCont = Utility.CreateClusteredImageContainerNew(
                                                                               size,
                                                                               Properties.Image,
                                                                               Properties.ImageClass,
                                                                               _elmDefaultImage,
                                                                               true,
                                                                               false,
                                                                               Properties.ImageTop,
                                                                               Properties.ImageLeft);
                        _elmDefaultImage.Alt = alt;
                        _elmDefaultA.AppendChild(_elmDefaultImageCont);
                    }

                    // Setup event handlers
                    AttachEventsForDisplayMode(displayMode);

                    return _elmDefault;
                default:
                    EnsureValidDisplayMode(displayMode);
                    return null;
            }
        }


        internal override void AttachDOMElementsForDisplayMode(string displayMode)
        {
            Span elm = (Span)Browser.Document.GetById(Id + "-" + displayMode);
            StoreElementForDisplayMode(elm, displayMode);

            switch (displayMode)
            {
                case "Large":
                case "Menu":
                    _elmDefault = elm;
                    _elmDefaultA = (Anchor)_elmDefault.ChildNodes[0];
                    _elmDefaultImageCont = (Span)_elmDefaultA.ChildNodes[0];
                    _elmDefaultImage = (Image)_elmDefaultImageCont.ChildNodes[0];

                    break;
            }
        }

        internal override void AttachEventsForDisplayMode(string displayMode)
        {
            switch (displayMode)
            {
                case "Large":
                case "Menu":
                    AttachEvents();
                    break;
            }
        }

        private void AttachEvents()
        {
            // Setup event handlers
            _elmDefaultA.Click += OnClick;
            _elmDefaultA.Focus += OnFocus;
            _elmDefaultA.MouseOver += OnFocus;
            _elmDefaultA.Blur += OnBlur;
            _elmDefaultA.MouseOut += OnBlur;
        }

        protected override void ReleaseEventHandlers()
        {
            // Clear event handlers
            _elmDefaultA.Click -= OnClick;
            _elmDefaultA.Focus -= OnFocus;
            _elmDefaultA.MouseOver -= OnFocus;
            _elmDefaultA.Blur -= OnBlur;
            _elmDefaultA.MouseOut -= OnBlur;
        }

        internal override string ControlType
        {
            get
            {
                return "GalleryButton";
            }
        }

        #region Event Handlers
        public override void OnEnabledChanged(bool enabled)
        {
            if (enabled)
                Utility.EnableElement(_elmDefaultA);
            else
                Utility.DisableElement(_elmDefaultA);
        }

        protected override void OnClick(HtmlEvent evt)
        {
            CloseToolTip();
            Utility.CancelEventUtility(evt, false, true);
            
            if (!Enabled)
                return;

            Toggle();

            CommandType ct = CommandType.General;
            string cmdtpe = Properties.CommandType;
            Dictionary<string, string> dict = StateProperties;
            dict[GalleryButtonCommandProperties.CommandValueId] = Properties.CommandValueId;
            dict["MenuItemId"] = Properties.MenuItemId;
            dict["SourceControlId"] = Properties.Id;
            if (!CUIUtility.IsNullOrUndefined(cmdtpe) && cmdtpe == "OptionSelection")
                ct = CommandType.OptionSelection;

            DisplayedComponent.RaiseCommandEvent(Properties.Command,
                                                 ct,
                                                 dict);

            if (Root.PollForState)
                PollForStateAndUpdate();
            else
                SetState(Utility.IsTrue(StateProperties[GalleryButtonCommandProperties.On]));
        }

        protected void OnFocus(HtmlEvent args)
        {
            OnBeginFocus();
            if (!Enabled)
                return;

            Root.LastFocusedControl = this;

            if (string.IsNullOrEmpty(Properties.CommandPreview))
                return;

            CommandType ct = CommandType.Preview;
            string cmdtpe = Properties.CommandType;
            StateProperties[GalleryButtonCommandProperties.CommandValueId] = Properties.CommandValueId;
            if (!CUIUtility.IsNullOrUndefined(cmdtpe) && cmdtpe == "OptionSelection")
            {
                ct = CommandType.OptionPreview;

            }

            DisplayedComponent.RaiseCommandEvent(Properties.CommandPreview,
                                                 ct,
                                                 StateProperties);
        }

        protected void OnBlur(HtmlEvent args)
        {
            OnEndFocus();
            if (!Enabled)
                return;

            if (string.IsNullOrEmpty(Properties.CommandRevert))
                return;

            CommandType ct = CommandType.PreviewRevert;
            string cmdtpe = Properties.CommandType;
            if (!CUIUtility.IsNullOrUndefined(cmdtpe) && cmdtpe == "OptionSelection")
            {
                ct = CommandType.OptionPreviewRevert;
                StateProperties[GalleryButtonCommandProperties.CommandValueId] = Properties.CommandValueId;
            }

            DisplayedComponent.RaiseCommandEvent(Properties.CommandRevert,
                                                 ct,
                                                 StateProperties);
        }
        #endregion

        #region ISelectableControl Members
        public HtmlElement GetDropDownDOMElementForDisplayMode(string displayMode)
        {
            return new Span();
        }

        public void Deselect()
        {
            Selected = false;
        }

        public string GetMenuItemId()
        {
            return Properties.MenuItemId;
        }

        public string GetCommandValueId()
        {
            return Properties.CommandValueId;
        }

        public void FocusOnDisplayedComponent()
        {
            ReceiveFocus();
        }
        #endregion

        #region IMenuItem Members
        public override string GetTextValue()
        {
            return Properties.Alt;
        }

        public override void ReceiveFocus()
        {
            _elmDefaultA.PerformFocus();
        }
        public override void OnMenuClosed()
        {
            base.OnMenuClosed();
        }
        #endregion

        private void Highlight()
        {
            Utility.EnsureCSSClassOnElement(_elmDefault, "ms-cui-gallerybutton-highlighted");
        }

        private void RemoveHighlight()
        {
            Utility.RemoveCSSClassFromElement(_elmDefault, "ms-cui-gallerybutton-highlighted");
        }

        protected void Toggle()
        {
            bool selected = !Utility.IsTrue(StateProperties[GalleryButtonCommandProperties.On]);
            StateProperties[GalleryButtonCommandProperties.On] = selected.ToString();
            SetState(selected);
        }

        private void SetState(bool selected)
        {
            if (selected)
                Highlight();
            else
                RemoveHighlight();
        }

        internal override void PollForStateAndUpdate()
        {
            Dictionary<string, string> dict = StateProperties;
            dict[GalleryButtonCommandProperties.CommandValueId] = Properties.CommandValueId;
            dict["MenuItemId"] = Properties.MenuItemId;
            dict["SourceControlId"] = Properties.Id;
            bool succeeded = PollForStateAndUpdateInternal(Properties.Command,
                                                            Properties.QueryCommand,
                                                            dict,
                                                            false);
            if (succeeded)
                SetState(Utility.IsTrue(StateProperties[GalleryButtonCommandProperties.On]));
        }

        protected bool Selected
        {
            get
            {
                return Utility.IsTrue(StateProperties[GalleryButtonCommandProperties.On]);
            }
            set
            {
                StateProperties[GalleryButtonCommandProperties.On] = value.ToString();
                if (value)
                    Highlight();
                else
                    RemoveHighlight();
            }
        }

        protected GalleryElementDimensions ElementDimensions
        {
            get
            {
                return _dims;
            }
            set
            {
                _dims = value;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _elmDefault = null;
            _elmDefaultA = null;
            _elmDefaultImage = null;
            _elmDefaultImageCont = null;
        }

        protected GalleryButtonProperties Properties
        {
            get
            {
                return (GalleryButtonProperties)base.ControlProperties;
            }
        }
    }
}
