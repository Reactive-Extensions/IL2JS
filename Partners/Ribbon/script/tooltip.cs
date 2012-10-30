using System;
using System.Collections.Generic;
using Microsoft.LiveLabs.Html;

namespace Ribbon
{
    /// <summary>
    /// DisabledCommandInfo Properties
    /// </summary>
    /// <owner alias="HillaryM" />
    internal sealed class DisabledCommandInfoProperties
    {
        public string Description;
        public string Title;
        public string Icon;
        public string IconClass;
        public string IconTop;
        public string IconLeft;
        public string HelpKeyWord;
    }

    /// <summary>
    /// Tooltip
    /// </summary>
    /// <owner alias="HillaryM" />
    internal class ToolTip : Component
    {
        // main tooltip DOM elements
        Div _elmBody;
        Div _elmInnerDiv;

        // Tooltip title
        Heading1 _elmTitle;

        // Control description section
        Div _elmDescription;
        Image _elmDescriptionImage;
        Span _elmDescriptionImageCont;

        // Selected Item (for tooltips launched by DropDowns)
        Paragraph _elmSelectedItemTitle;

        // Control disabled information section
        Div _elmDisabledInfo;
        Image _elmDisabledInfoIcon;
        Span _elmDisabledInfoIconCont;
        Div _elmDisabledInfoTitle;
        Div _elmDisabledInfoTitleText;

        // Tooltip footer
        Div _elmFooter;
        Image _elmFooterIcon;
        Span _elmFooterIconCont;
        Div _elmFooterTitleText;

        // Misc spacers
        Div _spacerDiv1;
        Div _spacerDiv2;
        Break _spacerRow3;
        HorizontalRule _spacerRow1;
        HorizontalRule _spacerRow2;

        ControlProperties _properties;

        DisabledCommandInfoProperties _disabledInfoProperties;
        const int _controlDescriptionLength = 512;
        const int _controlTitleLength = 100;
        const int _tooltipHorizontalOffsetInMenus = 30;
        const int _tooltipVerticalOffsetInMenus = 10;

        /// <summary>
        /// ToolTip Contructor.
        /// </summary>
        /// <param name="ribbon">The Ribbon that this ToolTip was created by and is a part of.</param>
        /// <param name="id">The Component id of this ToolTip.</param>
        /// <param name="title">The Title of this ToolTip.</param>
        /// <param name="description">The Description of this ToolTip.</param>
        /// <owner alias="HillaryM" />
        internal ToolTip(Root root,
                      string id,
                      string title,
                      string description,
                      ControlProperties properties)
            : base(root, id, title, description)
        {
            _properties = properties;

            if (!string.IsNullOrEmpty(properties.ToolTipShortcutKey))
            {
                // switch display based on text direction
                if (Root.TextDirection == Direction.LTR)
                {
                    this.TitleInternal = String.Format("{0} ({1})", Title, Properties.ToolTipShortcutKey);
                }
                else
                {
                    this.TitleInternal = String.Format("({1}) {0}", Title, Properties.ToolTipShortcutKey);
                }
            }
        }

        /// <summary>
        /// Gets the properties of the ToolTip.
        /// </summary>
        /// <owner alias="HillaryM" />
        ControlProperties Properties
        {
            get
            {
                return _properties;
            }
        }

        /// <summary>
        /// Gets or Sets information to show if the control that the ToolTip is attached to is disabled.
        /// </summary>
        /// <owner alias="HillaryM" />
        internal DisabledCommandInfoProperties DisabledCommandInfo
        {
            get
            {
                return _disabledInfoProperties;
            }
            set
            {
                _disabledInfoProperties = value;
            }
        }

        /// <summary>
        /// Creates the HTML for the ToolTip.
        /// </summary>
        /// <owner alias="HillaryM" />
        internal override void RefreshInternal()
        {
            if (NeedsDelayIniting)
                DoDelayedInit();

            EnsureDOMElementAndEmpty();

            // set the aria role
            ElementInternal.SetAttribute("role", "tooltip");

            // set the aria visibility
            ElementInternal.SetAttribute("aria-hidden", "true");

            if (CUIUtility.IsNullOrUndefined(_elmBody))
            {
                _elmBody = new Div();
                _elmBody.ClassName = "ms-cui-tooltip-body";
            }
            else
            {
                _elmBody = (Div)Utility.RemoveChildNodes(_elmBody);
            }
            ElementInternal.AppendChild(_elmBody);

            if (CUIUtility.IsNullOrUndefined(_elmInnerDiv))
            {
                _elmInnerDiv = new Div();
                _elmInnerDiv.ClassName = "ms-cui-tooltip-glow";
                _elmBody.AppendChild(_elmInnerDiv);
            }
            else
            {
                _elmInnerDiv = (Div)Utility.RemoveChildNodes(_elmInnerDiv);
            }

            // set the title and shortcut
            if (CUIUtility.IsNullOrUndefined(_elmTitle))
            {
                _elmTitle = new Heading1();
                if (TitleInternal.Length > _controlTitleLength)
                {
                    UIUtility.SetInnerText(_elmTitle, TitleInternal.Substring(0, _controlTitleLength));
                }
                else
                {
                    UIUtility.SetInnerText(_elmTitle, Title);
                }
                _elmInnerDiv.AppendChild(_elmTitle);
            }

            // set the image if available
            if (CUIUtility.IsNullOrUndefined(_elmDescriptionImage) &&
                !string.IsNullOrEmpty(Properties.ToolTipImage32by32))
            {
                _elmDescriptionImage = new Image();
                _elmDescriptionImageCont = Utility.CreateClusteredImageContainerNew(
                                                                          ImgContainerSize.Size32by32,
                                                                          Properties.ToolTipImage32by32,
                                                                          Properties.ToolTipImage32by32Class,
                                                                          _elmDescriptionImage,
                                                                          true,
                                                                          false,
                                                                          Properties.ToolTipImage32by32Top,
                                                                          Properties.ToolTipImage32by32Left);
                _elmDescriptionImageCont.ClassName = _elmDescriptionImageCont.ClassName + " ms-cui-tooltip-bitmap ";
                _elmInnerDiv.AppendChild(_elmDescriptionImageCont);
            }

            // set the description
            string selectedItemTitle = Properties.ToolTipSelectedItemTitle;
            string descriptionText = Description;
            if (CUIUtility.IsNullOrUndefined(_elmDescription) 
                && (!string.IsNullOrEmpty(descriptionText) ||
                    !string.IsNullOrEmpty(selectedItemTitle)))
            {
                _elmDescription = new Div();
                _elmDescription.ClassName = "ms-cui-tooltip-description";
                if (!string.IsNullOrEmpty(Properties.ToolTipImage32by32))
                {
                    _elmDescription.Style.Width = "80%";
                }
                _elmInnerDiv.AppendChild(_elmDescription);

                string seletedItemTitlePrefix = Root.Properties.ToolTipSelectedItemTitlePrefix;
                if (!string.IsNullOrEmpty(selectedItemTitle) &&
                    !string.IsNullOrEmpty(seletedItemTitlePrefix))
                {
                    string selectedItemText = String.Format(seletedItemTitlePrefix, selectedItemTitle);
                    _elmSelectedItemTitle = new Paragraph();
                    UIUtility.SetInnerText(_elmSelectedItemTitle, selectedItemText);

                    _elmDescription.AppendChild(_elmSelectedItemTitle);
                    _spacerRow3 = new Break();
                    _elmDescription.AppendChild(_spacerRow3);
                }
                if (!string.IsNullOrEmpty(descriptionText))
                {
                    if (descriptionText.Length > _controlDescriptionLength)
                    {
                        _elmDescription.InnerHtml = _elmDescription.InnerHtml + Utility.HtmlEncodeAllowSimpleTextFormatting(descriptionText.Substring(0, _controlDescriptionLength), true);
                    }
                    else
                    {
                        _elmDescription.InnerHtml = _elmDescription.InnerHtml + Utility.HtmlEncodeAllowSimpleTextFormatting(descriptionText, true);
                    }
                }
            }

            // Disabled info explaining why a command is currently disabled
            if (CUIUtility.IsNullOrUndefined(_elmDisabledInfo) && 
                !CUIUtility.IsNullOrUndefined(_disabledInfoProperties) &&
                !string.IsNullOrEmpty(_disabledInfoProperties.Title))
            {
                // provide spacer to distinguish from main description above
                _spacerDiv1 = new Div();
                _spacerDiv1.ClassName = "ms-cui-tooltip-clear";
                _elmInnerDiv.AppendChild(_spacerDiv1);

                _spacerRow1 = new HorizontalRule();
                _elmInnerDiv.AppendChild(_spacerRow1);

                // title for this message
                _elmDisabledInfoTitle = new Div();
                _elmDisabledInfoTitle.ClassName = "ms-cui-tooltip-footer";
                _elmInnerDiv.AppendChild(_elmDisabledInfoTitle);

                _elmDisabledInfoTitleText = new Div();
                UIUtility.SetInnerText(_elmDisabledInfoTitleText, _disabledInfoProperties.Title);

                // icon for this message
                _elmDisabledInfoIcon = new Image();
                _elmDisabledInfoIconCont = Utility.CreateClusteredImageContainerNew(
                                                                            ImgContainerSize.Size16by16,
                                                                            _disabledInfoProperties.Icon,
                                                                            _disabledInfoProperties.IconClass,
                                                                            _elmDisabledInfoIcon,
                                                                            true,
                                                                            false,
                                                                            _disabledInfoProperties.IconTop,
                                                                            _disabledInfoProperties.IconLeft);

                _elmDisabledInfoIconCont.Style.VerticalAlign = "top";

                // switch display based on text direction
                // REVIEW(jkern,josefl): I don't think that we need to manually do this.  We should get it for free in 
                // the browser with the "dir=rtl" attribute.  Check this when the RTL work is done.
                if (Root.TextDirection == Direction.LTR)
                {
                    _elmDisabledInfoTitle.AppendChild(_elmDisabledInfoIconCont);
                    _elmDisabledInfoTitle.AppendChild(_elmDisabledInfoTitleText);
                }
                else
                {
                    _elmDisabledInfoTitle.AppendChild(_elmDisabledInfoTitleText);
                    _elmDisabledInfoTitle.AppendChild(_elmDisabledInfoIconCont);
                }

                // disabled info text
                if (!string.IsNullOrEmpty(_disabledInfoProperties.Description))
                {
                    _elmDisabledInfo = new Div();
                    _elmDisabledInfo.ClassName = "ms-cui-tooltip-description";
                    _elmDisabledInfo.Style.Width = "90%";
                    UIUtility.SetInnerText(_elmDisabledInfo, _disabledInfoProperties.Description);
                    _elmInnerDiv.AppendChild(_elmDisabledInfo);
                }
            }

            // set the footer
            if (!CUIUtility.IsNullOrUndefined(_elmFooter) &&
                !string.IsNullOrEmpty(Root.Properties.ToolTipFooterText) &&
                !string.IsNullOrEmpty(Root.Properties.ToolTipFooterImage16by16) &&
                (((!CUIUtility.IsNullOrUndefined(_disabledInfoProperties)) &&
                    (!string.IsNullOrEmpty(_disabledInfoProperties.HelpKeyWord))) ||
                    (!string.IsNullOrEmpty(Properties.ToolTipHelpKeyWord))))
            {
                _spacerDiv2 = new Div();
                _spacerDiv2.ClassName = "ms-cui-tooltip-clear";
                _elmInnerDiv.AppendChild(_spacerDiv2);

                _spacerRow2 = new HorizontalRule();
                _elmInnerDiv.AppendChild(_spacerRow2);

                _elmFooter = new Div();
                _elmFooter.ClassName = "ms-cui-tooltip-footer";
                _elmInnerDiv.AppendChild(_elmFooter);

                _elmFooterTitleText = new Div();
                UIUtility.SetInnerText(_elmFooterTitleText, Root.Properties.ToolTipFooterText);

                _elmFooterIcon = new Image();

                _elmFooterIconCont = Utility.CreateClusteredImageContainerNew(
                                                                      ImgContainerSize.Size16by16,
                                                                      Root.Properties.ToolTipFooterImage16by16,
                                                                      Root.Properties.ToolTipFooterImage16by16Class,
                                                                      _elmFooterIcon,
                                                                      true,
                                                                      false,
                                                                      Root.Properties.ToolTipFooterImage16by16Top,
                                                                      Root.Properties.ToolTipFooterImage16by16Left
                                                                      );

                _elmFooterIconCont.Style.VerticalAlign = "top";

                // switch display based on text direction
                // REVIEW(jkern,josefl): I don't think that we need to manually do this.  We should get it for free in 
                // the browser with the "dir=rtl" attribute.  Check this when the RTL work is done.
                if (Root.TextDirection == Direction.LTR)
                {
                    _elmFooter.AppendChild(_elmFooterIconCont);
                    _elmFooter.AppendChild(_elmFooterTitleText);
                }
                else
                {
                    _elmFooter.AppendChild(_elmFooterTitleText);
                    _elmFooter.AppendChild(_elmFooterIconCont);
                }
            }

            // build DOM structure
            this.AppendChildrenToElement(_elmBody);
            base.RefreshInternal();
        }

        /// <summary>
        /// Display the tooltip.
        /// </summary>        
        /// <owner alias="HillaryM" />
        internal bool Display()
        {
            RefreshInternal();

            // Hide tooltip while positioning
            ElementInternal.Style.Visibility = "hidden";
            ElementInternal.Style.Position = "absolute";
            ElementInternal.Style.Top = "0px";
            ElementInternal.Style.Left = "0px";
            Browser.Document.Body.AppendChild(ElementInternal);
            PositionToolTip();

            // For IE, we need a backframe IFrame in order for the tooltip to show up over 
            // ActiveX controls
            if (BrowserUtility.InternetExplorer)
            {
                Utility.PositionBackFrame(Root.TooltipBackFrame, ElementInternal);
                Root.TooltipBackFrame.Style.Visibility = "visible";
            }

            // Show menu once it is positioned
            ElementInternal.Style.Visibility = "visible";
            Visible = true;

            // set the aria visibility 
            ElementInternal.SetAttribute("aria-hidden", "false");
            return true;
        }

        /// <summary>
        /// Hide the ToolTip.
        /// </summary>
        /// <owner alias="HillaryM" />
        internal void Hide()
        {
            if (!CUIUtility.IsNullOrUndefined(ElementInternal))
            {
                ElementInternal.Style.Visibility = "hidden";
                // set the aria visibility
                ElementInternal.SetAttribute("aria-hidden", "true");
            }
            if (BrowserUtility.InternetExplorer)
            {
                Root.TooltipBackFrame.Style.Visibility = "hidden";

            }
            Visible = false;
        }

        /// <summary>
        /// Gets the CSS class of used for rendering the ToolTip.
        /// </summary>
        /// <owner alias="HillaryM" />
        protected override string CssClass
        {
            get
            {
                return "ms-cui-tooltip";
            }
        }

        /// <summary>
        /// The inner div of the ToolTip.
        /// </summary>
        /// <owner alias="HillaryM" />
        protected Div InnerDiv
        {
            get
            {
                return _elmInnerDiv;
            }
            set
            {
                _elmInnerDiv = value;
            }
        }

        internal void PositionToolTip()
        {
            HtmlElement flyOut = this.ElementInternal;
            HtmlElement launcher = this.Parent.ElementInternal;

            if (CUIUtility.IsNullOrUndefined(flyOut) || CUIUtility.IsNullOrUndefined(launcher))
            {
                return;
            }

            // Set temporary position on flyOut and get dimensions
            flyOut.Style.Top = "0px";
            flyOut.Style.Left = "0px";
            Dictionary<string, int> d = Root.GetAllElementDimensions(flyOut, launcher);
            int flyOutWidth = d["flyOutWidth"];
            // check if launching control is within ribbon body
            if ((Parent.ComponentTopPosition > Root.ComponentTopPosition) &&
                ((Parent.ComponentTopPosition + Parent.ComponentHeight) < (Root.ComponentTopPosition + Root.ComponentHeight)))
            {
                d["launcherTop"] = Root.ComponentTopPosition;
                d["launcherHeight"] = Root.ComponentHeight;
            }
            else
            {
                // if the tooltip is launched within a menu, we offset it vertically and horizontally to prevent occluding menu items
                int launcherTop = d["launcherTop"];
                int launcherLeft = d["launcherLeft"];

                launcherLeft += _tooltipHorizontalOffsetInMenus;
                launcherTop += _tooltipVerticalOffsetInMenus;
                d["launcherLeft"] = launcherLeft;
                d["launcherTop"] = launcherTop;
            }
            Root.SetFlyOutCoordinates(flyOut, d, false);
            // O14: 545739 - the setFlyoutCoordinates method sets the midwidth of the flyout. We don't want this set for tootips
            flyOut.Style.MinWidth = flyOutWidth + "px";
        }

        internal void OnClick(HtmlEvent evt)
        {
            if (this.Root != null)
            {
                Root.CloseOpenTootips();
            }
        }

        internal void OnKeyPress(HtmlEvent evt)
        {
            // To make OACR happy
            if (evt != null)
            {
                // using F2 key
                int helpKeyPC = 113;
                int helpKeyMac = 123;

                if (((evt.KeyCode == helpKeyPC) || (evt.KeyCode == helpKeyMac)))
                {
                    string keyword = null;
                    if (!string.IsNullOrEmpty(_properties.ToolTipHelpKeyWord))
                    {
                        keyword = _properties.ToolTipHelpKeyWord;

                    }
                    if ((!CUIUtility.IsNullOrUndefined(_disabledInfoProperties)) &&
                            (!string.IsNullOrEmpty(_disabledInfoProperties.HelpKeyWord)))
                    {
                        // if the control is disabled, the disabled info HelpKeyword overrules any previously-set help keyword
                        keyword = _disabledInfoProperties.HelpKeyWord;
                    }
                    if (!string.IsNullOrEmpty(keyword))
                    {
                        Dictionary<string, string> dict = new Dictionary<string, string>();
                        dict["HelpKeyword"] = keyword;
                        if (!string.IsNullOrEmpty(Root.Properties.ToolTipHelpCommand))
                        {
                            RaiseCommandEvent(Root.Properties.ToolTipHelpCommand, CommandType.General, dict);
                        }
                    }

                    // prevent the key's default action
                    Utility.CancelEventUtility(evt, true, true);
                }
                else
                {
                    // Close open tooltips
                    Root.CloseOpenTootips();
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _elmBody = null;
            _elmDescription = null;
            _elmDescriptionImage = null;
            _elmDescriptionImageCont = null;
            _elmDisabledInfo = null;
            _elmDisabledInfoIcon = null;
            _elmDisabledInfoTitle = null;
            _elmDisabledInfoTitleText = null;
            _elmFooter = null;
            _elmFooterIcon = null;
            _elmFooterIconCont = null;
            _elmFooterTitleText = null;
            _elmInnerDiv = null;
            _elmTitle = null;
            _elmSelectedItemTitle = null;
        }
    }
}
