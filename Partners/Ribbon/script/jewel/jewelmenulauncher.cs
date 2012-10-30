using System;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Ribbon.Controls
{
    [Import(MemberNameCasing = Casing.Exact)]
    public class JewelMenuLauncherProperties : MenuLauncherControlProperties
    {
        extern public JewelMenuLauncherProperties();
        extern public string Alt { get; }
        extern public string Height { get; }
        extern public string LabelText { get; }

        // Left image
        extern public string ImageLeftSide { get; }
        extern public string ImageLeftSideClass { get; }
        extern public string ImageLeftSideTop { get; }
        extern public string ImageLeftSideLeft { get; }
        extern public string ImageLeftSideWidth { get; }
        extern public string ImageLeftSideHover { get; }
        extern public string ImageLeftSideHoverClass { get; }
        extern public string ImageLeftSideHoverTop { get; }
        extern public string ImageLeftSideHoverLeft { get; }
        extern public string ImageLeftSideDown { get; }
        extern public string ImageLeftSideDownClass { get; }
        extern public string ImageLeftSideDownTop { get; }
        extern public string ImageLeftSideDownLeft { get; }

        // Center image
        extern public string Image { get; }
        extern public string ImageClass { get; }
        extern public string ImageTop { get; }
        extern public string ImageLeft { get; }
        extern public string ImageHover { get; }
        extern public string ImageHoverClass { get; }
        extern public string ImageHoverTop { get; }
        extern public string ImageHoverLeft { get; }
        extern public string ImageDown { get; }
        extern public string ImageDownClass { get; }
        extern public string ImageDownTop { get; }
        extern public string ImageDownLeft { get; }

        // Right image
        extern public string ImageRightSide { get; }
        extern public string ImageRightSideClass { get; }
        extern public string ImageRightSideTop { get; }
        extern public string ImageRightSideLeft { get; }
        extern public string ImageRightSideWidth { get; }
        extern public string ImageRightSideHover { get; }
        extern public string ImageRightSideHoverClass { get; }
        extern public string ImageRightSideHoverTop { get; }
        extern public string ImageRightSideHoverLeft { get; }
        extern public string ImageRightSideDown { get; }
        extern public string ImageRightSideDownClass { get; }
        extern public string ImageRightSideDownTop { get; }
        extern public string ImageRightSideDownLeft { get; }
    }

    /// <summary>
    /// This class represents the Menu launcher button for a Jewel
    /// </summary>
    internal class JewelMenuLauncher : MenuLauncher
    {
        internal JewelMenuLauncher(Jewel jewel, string id, JewelMenuLauncherProperties properties, Menu menu)
            : base(jewel, id, properties, menu)
        {
            AddDisplayMode("Default");
        }

        Span _elmDefault;
        Anchor _elmDefaultA;

        // Single-image mode elements
        Image _elmDefaultImg;
        Span _elmDefaultImgCont;

        // Text with 3 images mode elements
        Span _elmLeft;
        Span _elmRight;
        Span _elmLabel;
        Span _elmMiddle;

        bool _inLabelMode;

        protected override HtmlElement CreateDOMElementForDisplayMode(string displayMode)
        {
            if (displayMode != "Default")
            {
                EnsureValidDisplayMode(displayMode);
                return null;
            }

            string alt = CUIUtility.SafeString(Properties.Alt);

            // Create Elements
            _elmDefault = new Span();

            _elmDefault.Id = Properties.Id + "-Default";
            _elmDefault.ClassName = "ms-cui-jewel-jewelMenuLauncher";

            _elmDefaultA = new Anchor();
            Utility.NoOpLink(_elmDefaultA);
            _elmDefaultA.Title = alt;

            _inLabelMode = !string.IsNullOrEmpty(Properties.LabelText);

            if (!_inLabelMode)
            {
                _elmDefaultImg = new Image();

                _elmDefaultImgCont = Utility.CreateClusteredImageContainerNew(
                                                                     ImgContainerSize.Size56by24,
                                                                     Properties.Image,
                                                                     Properties.ImageClass,
                                                                     _elmDefaultImg,
                                                                     true,
                                                                     false,
                                                                     Properties.ImageTop,
                                                                     Properties.ImageLeft);
                _elmDefaultImg.Alt = alt;
                _elmDefaultA.AppendChild(_elmDefaultImgCont);
            }
            else
            {
                bool hasLeftImage = !string.IsNullOrEmpty(Properties.ImageLeftSide);
                bool hasRightImage = !string.IsNullOrEmpty(Properties.ImageRightSide);

                if (hasLeftImage)
                {
                    _elmLeft = new Span();
                    _elmLeft.ClassName = "ms-cui-jewel-left";
                    _elmLeft.Id = Properties.Id + "-Default-left";
                    Utility.PrepareClusteredBackgroundImageContainer(_elmLeft,
                                                                     Properties.ImageLeftSide,
                                                                     Properties.ImageLeftSideClass,
                                                                     Properties.ImageLeftSideTop,
                                                                     Properties.ImageLeftSideLeft,
                                                                     null,
                                                                     Properties.Height);

                    _elmLeft.Style.Width = Properties.ImageLeftSideWidth + "px";
                    _elmLeft.Style.Height = Properties.Height + "px";
                    _elmDefaultA.AppendChild(_elmLeft);
                }

                _elmMiddle = new Span();
                _elmMiddle.ClassName = "ms-cui-jewel-middle";
                _elmMiddle.Id = Properties.Id + "-Default-middle";
                Utility.PrepareClusteredBackgroundImageContainer(_elmMiddle,
                                                                 Properties.Image,
                                                                 Properties.ImageClass,
                                                                 Properties.ImageTop,
                                                                 Properties.ImageLeft,
                                                                 null,
                                                                 Properties.Height);
                _elmLabel = new Span();
                if (!string.IsNullOrEmpty(Properties.LabelCss))
                    _elmLabel.Style.CssText = Properties.LabelCss;
                _elmLabel.ClassName = "ms-cui-jewel-label";
                if (!string.IsNullOrEmpty(Properties.Height))
                    _elmLabel.Style.MarginTop = (Math.Floor(Int32.Parse(Properties.Height) - 14 /* 8px for text, 6px for existing padding */) / 2) + "px";
                UIUtility.SetInnerText(_elmLabel, Properties.LabelText);
                _elmMiddle.AppendChild(_elmLabel);
                _elmDefaultA.AppendChild(_elmMiddle);

                if (hasRightImage)
                {
                    _elmRight = new Span();
                    _elmRight.ClassName = "ms-cui-jewel-right";
                    _elmRight.Id = Properties.Id + "-Default-right";
                    Utility.PrepareClusteredBackgroundImageContainer(_elmRight,
                                                                     Properties.ImageRightSide,
                                                                     Properties.ImageRightSideClass,
                                                                     Properties.ImageRightSideTop,
                                                                     Properties.ImageRightSideLeft,
                                                                     null,
                                                                     Properties.Height);

                    _elmRight.Style.Width = Properties.ImageRightSideWidth + "px";
                    _elmRight.Style.Height = Properties.Height + "px";
                    _elmDefaultA.AppendChild(_elmRight);
                }
            }

            // Setup Event Handlers
            AttachEventsForDisplayMode(displayMode);

            // Build DOM Structure
            _elmDefault.AppendChild(_elmDefaultA);

            return _elmDefault;
        }

        internal override void AttachDOMElementsForDisplayMode(string displayMode)
        {
            EnsureValidDisplayMode(displayMode);

            _elmDefault = (Span)Browser.Document.GetById(Id + "-" + displayMode);
            StoreElementForDisplayMode(_elmDefault, displayMode);
            _elmDefaultA = (Anchor)_elmDefault.ChildNodes[0];

            _inLabelMode = !string.IsNullOrEmpty(Properties.LabelText);
            if (!_inLabelMode)
            {
                _elmDefaultImgCont = (Span)_elmDefaultA.ChildNodes[0];
                _elmDefaultImg = (Image)_elmDefaultImgCont.ChildNodes[0];
            }
            else
            {
                _elmLeft = (Span)Browser.Document.GetById(Id + "-" + displayMode + "-left");
                _elmRight = (Span)Browser.Document.GetById(Id + "-" + displayMode + "-right");
                _elmMiddle = (Span)Browser.Document.GetById(Id + "-" + displayMode + "-middle");

                if (!CUIUtility.IsNullOrUndefined(_elmMiddle))
                    _elmLabel = (Span)_elmMiddle.FirstChild;
            }
        }

        internal override void AttachEventsForDisplayMode(string displayMode)
        {
            EnsureValidDisplayMode(displayMode);

            _elmDefaultA.MouseOver += OnFocus;
            _elmDefaultA.Focus += OnFocus;
            _elmDefaultA.MouseOut += OnBlur;
            _elmDefaultA.Blur += OnBlur;
            _elmDefaultA.Click += OnClick;
            _elmDefaultA.KeyPress += OnKeyPress;

            if (BrowserUtility.InternetExplorer)
                _elmDefaultA.ContextMenu += OnContextMenu;
        }

        protected override void ReleaseEventHandlers()
        {
            _elmDefaultA.Focus -= OnFocus;
            _elmDefaultA.MouseOver -= OnFocus;
            _elmDefaultA.Blur -= OnBlur;
            _elmDefaultA.MouseOut -= OnBlur;
            _elmDefaultA.Click -= OnClick;
            _elmDefaultA.KeyPress -= OnKeyPress;

            if (BrowserUtility.InternetExplorer)
                _elmDefaultA.ContextMenu -= OnContextMenu;
        }

        public override void OnEnabledChanged(bool enabled)
        {
            Utility.SetEnabledOnElement(_elmDefaultA, enabled);
        }

        #region Event Handlers
        protected void OnFocus(HtmlEvent args)
        {
            if (!Enabled || MenuLaunched)
                return;

            Highlight();
        }

        protected void OnBlur(HtmlEvent args)
        {
            if (!Enabled || MenuLaunched)
                return;

            RemoveHighlight();
        }

        protected override void OnClick(HtmlEvent args)
        {
            Utility.CancelEventUtility(args, false, true);
            if (!Enabled ||
                CUIUtility.IsNullOrUndefined(args) ||
                args.Button != (int)MouseButton.LeftButton)
            {
                return;
            }
           
            if (MenuLaunched)
            {
                CloseMenu();
                return;
            }
            LaunchJewelMenu();
        }

        protected void OnKeyPress(HtmlEvent args)
        {
            if (!Enabled)
                return;

            // Make OACR happy
            if (CUIUtility.IsNullOrUndefined(args))
                return;

            int key = args.KeyCode;
            if (key == (int)Key.Enter || key == (int)Key.Space || key == (int)Key.Down)
            {
                LaunchedByKeyboard = true;

                if (MenuLaunched)
                {
                    CloseMenu();
                }
                else
                {
                    LaunchJewelMenu();
                }

                Utility.CancelEventUtility(args, false, true);
            }
            // No need to check for escape to close here because the modal element will do that for us (O14:391733)
        }

        protected void OnContextMenu(HtmlEvent args)
        {
            Utility.CancelEventUtility(args, false, true);
        }

        private void LaunchJewelMenu()
        {
            if (string.IsNullOrEmpty(Properties.ImageDown))
                return;

            if (!_inLabelMode)
            {
                _elmDefaultImg.Src = Properties.ImageDown;
                if (!string.IsNullOrEmpty(Properties.ImageDownClass))
                    _elmDefaultImg.ClassName = Properties.ImageDownClass;
            }
            else
            {
                if (_elmLeft != null)
                {
                    Utility.PrepareClusteredBackgroundImageContainer(_elmLeft,
                                                                     Properties.ImageLeftSideDown,
                                                                     Properties.ImageLeftSideDownClass,
                                                                     Properties.ImageLeftSideDownTop,
                                                                     Properties.ImageLeftSideDownLeft,
                                                                     null,
                                                                     Properties.Height);
                }

                Utility.PrepareClusteredBackgroundImageContainer(_elmMiddle,
                                                                 Properties.ImageDown,
                                                                 Properties.ImageDownClass,
                                                                 Properties.ImageDownTop,
                                                                 Properties.ImageDownLeft,
                                                                 null,
                                                                 Properties.Height);

                if (_elmRight != null)
                {
                    Utility.PrepareClusteredBackgroundImageContainer(_elmRight,
                                                                     Properties.ImageRightSideDown,
                                                                     Properties.ImageRightSideDownClass,
                                                                     Properties.ImageRightSideDownTop,
                                                                     Properties.ImageRightSideDownLeft,
                                                                     null,
                                                                     Properties.Height);
                }
            }

            LaunchMenuInternal(_elmDefaultA);
        }

        protected override void OnLaunchedMenuClosed()
        {
            RemoveHighlight();
            DisplayedComponent.RaiseCommandEvent(Properties.CommandMenuClose,
                                                 CommandType.MenuClose,
                                                 null);
        }
        #endregion

        #region Menu Launcher Methods
        protected void LaunchMenuInternal(HtmlElement elmHadFocus)
        {
            LaunchMenu(elmHadFocus);

            DisplayedComponent.RaiseCommandEvent(Properties.CommandMenuOpen,
                                                 CommandType.MenuCreation,
                                                 null);
        }
        #endregion

        private void Highlight()
        {
            if (string.IsNullOrEmpty(Properties.ImageHover))
                return;

            if (!_inLabelMode)
            {
                _elmDefaultImg.Src = Properties.ImageHover;
                if (!string.IsNullOrEmpty(Properties.ImageHoverClass))
                    _elmDefaultImg.ClassName = Properties.ImageHoverClass;
            }
            else
            {
                if (_elmLeft != null)
                {
                    Utility.PrepareClusteredBackgroundImageContainer(_elmLeft,
                                                                     Properties.ImageLeftSideHover,
                                                                     Properties.ImageLeftSideHoverClass,
                                                                     Properties.ImageLeftSideHoverTop,
                                                                     Properties.ImageLeftSideHoverLeft,
                                                                     null,
                                                                     Properties.Height);
                }

                Utility.PrepareClusteredBackgroundImageContainer(_elmMiddle,
                                                                 Properties.ImageHover,
                                                                 Properties.ImageHoverClass,
                                                                 Properties.ImageHoverTop,
                                                                 Properties.ImageHoverLeft,
                                                                 null,
                                                                 Properties.Height);

                if (_elmRight != null)
                {
                    Utility.PrepareClusteredBackgroundImageContainer(_elmRight,
                                                                     Properties.ImageRightSideHover,
                                                                     Properties.ImageRightSideHoverClass,
                                                                     Properties.ImageRightSideHoverTop,
                                                                     Properties.ImageRightSideHoverLeft,
                                                                     null,
                                                                     Properties.Height);
                }
            }
        }

        private void RemoveHighlight()
        {
            if (string.IsNullOrEmpty(Properties.ImageHover))
                return;

            if (!_inLabelMode)
            {
                _elmDefaultImg.Src = Properties.Image;
                if (!string.IsNullOrEmpty(Properties.ImageClass))
                    _elmDefaultImg.ClassName = Properties.ImageClass;
            }
            else
            {
                if (_elmLeft != null)
                {
                    Utility.PrepareClusteredBackgroundImageContainer(_elmLeft,
                                                                     Properties.ImageLeftSide,
                                                                     Properties.ImageLeftSideClass,
                                                                     Properties.ImageLeftSideTop,
                                                                     Properties.ImageLeftSideLeft,
                                                                     null,
                                                                     Properties.Height);
                }

                Utility.PrepareClusteredBackgroundImageContainer(_elmMiddle,
                                                                 Properties.Image,
                                                                 Properties.ImageClass,
                                                                 Properties.ImageTop,
                                                                 Properties.ImageLeft,
                                                                 null,
                                                                 Properties.Height);

                if (_elmRight != null)
                {
                    Utility.PrepareClusteredBackgroundImageContainer(_elmRight,
                                                                     Properties.ImageRightSide,
                                                                     Properties.ImageRightSideClass,
                                                                     Properties.ImageRightSideTop,
                                                                     Properties.ImageRightSideLeft,
                                                                     null,
                                                                     Properties.Height);
                }
            }
        }

        protected JewelMenuLauncherProperties Properties
        {
            get 
            { 
                return (JewelMenuLauncherProperties)base.ControlProperties; 
            }
        }

        /// <summary>
        /// Focuses on the Launcher for the menu
        /// </summary>
        internal void FocusOnLauncher()
        {
            _elmDefaultA.PerformFocus();
        }
    }
}
