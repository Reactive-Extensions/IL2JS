using System;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;

using MSLabel = Microsoft.LiveLabs.Html.Label;

namespace Ribbon.Controls
{
    [Import(MemberNameCasing = Casing.Exact)]
    public class LabelProperties : ControlProperties
    {
        extern public LabelProperties();
        extern public string LabelText { get; }
        extern public string ForId { get; }
        extern public string QueryCommand { get; }
        extern public string Image16by16 { get; }
        extern public string Image16by16Class { get; }
        extern public string Image16by16Top { get; }
        extern public string Image16by16Left { get; }
    }

    /// <summary>
    /// The properties that can be set via polling on a Label-based control
    /// </summary>
    public class LabelCommandProperties
    {
        public static string Value = "Value";
    }


    /// <summary>
    /// A class that displays a label in the Ribbon UI.
    /// This Control takes the following parameters:
    /// LblTxt - The text in the label.
    /// ForId - The id of the input this label refers.
    /// </summary>
    internal class Label : Control
    {
        // Medium
        HtmlElement _elmDefault;
        Image _elmDefaultIcon;
        Span _elmDefaultLabel;

        // Small
        HtmlElement _elmSmall;
        Image _elmSmallIcon;

        string _strInnerText;

        public Label(Root root, string id, LabelProperties properties)
            : base(root, id, properties)
        {
            AddDisplayMode("Medium");
            AddDisplayMode("Small");
        }

        protected override HtmlElement CreateDOMElementForDisplayMode(string displayMode)
        {
            string forId = Properties.ForId;
            string label = Properties.LabelText;
            switch (displayMode)
            {
                case "Medium":
                    if (!string.IsNullOrEmpty(forId))
                    {
                        _elmDefault = new MSLabel();
                        if (BrowserUtility.InternetExplorer7)
                        {
                            _elmDefault.SetAttribute("htmlFor", forId);
                        }
                        else
                        {
                            _elmDefault.SetAttribute("for", forId);
                        }
                    }
                    else
                    {
                        _elmDefault = new Span();
                    }

                    _elmDefault.SetAttribute("mscui:controltype", ControlType);
                    _elmDefault.ClassName = "ms-cui-ctl-small ms-cui-fslb";

                    if (!string.IsNullOrEmpty(Properties.Image16by16))
                    {
                        _elmDefaultIcon = new Image();
                        Span elmDefaultIconCont = Utility.CreateClusteredImageContainerNew(
                                                                                        ImgContainerSize.Size16by16,
                                                                                        Properties.Image16by16,
                                                                                        Properties.Image16by16Class,
                                                                                        _elmDefaultIcon,
                                                                                        true,
                                                                                        false,
                                                                                        Properties.Image16by16Top,
                                                                                        Properties.Image16by16Left);

                        Span elmDefaultIconContainer = new Span();
                        elmDefaultIconContainer.ClassName = "ms-cui-ctl-iconContainer";
                        elmDefaultIconContainer.AppendChild(elmDefaultIconCont);
                        _elmDefault.AppendChild(elmDefaultIconContainer);
                    }

                    _elmDefaultLabel = new Span();
                    _elmDefaultLabel.ClassName = "ms-cui-ctl-mediumlabel";

                    if (!string.IsNullOrEmpty(label))
                    {
                        UIUtility.SetInnerText(_elmDefaultLabel, label);
                    }

                    _elmDefault.AppendChild(_elmDefaultLabel);
                    return _elmDefault;
                case "Small":
                    if (!string.IsNullOrEmpty(forId))
                    {
                        _elmSmall = new MSLabel();
                        if (BrowserUtility.InternetExplorer7)
                        {
                            _elmSmall.SetAttribute("htmlFor", forId);

                        }
                        else
                        {
                            _elmSmall.SetAttribute("for", forId);
                        }
                    }
                    else
                    {
                        _elmSmall = new Span();
                    }

                    _elmSmall.SetAttribute("mscui:controltype", ControlType);
                    _elmSmall.ClassName = "ms-cui-ctl-small ms-cui-fslb";

                    if (string.IsNullOrEmpty(Properties.Image16by16))
                    {
                        throw new ArgumentNullException("Image16by16", "Small display mode must have an icon set");
                    }

                    _elmSmallIcon = new Image();
                    Span elmSmallIconCont = Utility.CreateClusteredImageContainerNew(
                                                                                  ImgContainerSize.Size16by16,
                                                                                  Properties.Image16by16,
                                                                                  Properties.Image16by16Class,
                                                                                  _elmSmallIcon,
                                                                                  true,
                                                                                  false,
                                                                                  Properties.Image16by16Top,
                                                                                  Properties.Image16by16Left);

                    if (!string.IsNullOrEmpty(label))
                    {
                        _elmSmallIcon.Alt = label;

                    }

                    Span elmSmallIconContainer = new Span();
                    elmSmallIconContainer.ClassName = "ms-cui-ctl-iconContainer";
                    elmSmallIconContainer.AppendChild(elmSmallIconCont);
                    _elmSmall.AppendChild(elmSmallIconContainer);
                    return _elmSmall;
                default:
                    EnsureValidDisplayMode(displayMode);
                    break;
            }
            return null;
        }

        internal override void AttachDOMElementsForDisplayMode(string displayMode)
        {
            HtmlElement elm = Browser.Document.GetById(Id + "-" + displayMode);
            this.StoreElementForDisplayMode(elm, displayMode);

            switch (displayMode)
            {
                case "Medium":
                    _elmDefault = elm;
                    if (!string.IsNullOrEmpty(Properties.Image16by16))
                    {
                        _elmDefaultIcon = (Image)_elmDefault.FirstChild.FirstChild.FirstChild;
                        _elmDefaultLabel = (Span)_elmDefault.ChildNodes[1];
                    }
                    else
                    {
                        _elmDefaultLabel = (Span)_elmDefault.FirstChild;
                    }
                    break;
                case "Small":
                    _elmSmall = elm;
                    _elmSmallIcon = (Image)_elmSmall.FirstChild.FirstChild.FirstChild;
                    break;
            }
        }

        public override void OnEnabledChanged(bool enabled)
        {
            Utility.SetEnabledOnElement(_elmDefault, enabled);
            Utility.SetEnabledOnElement(_elmSmall, enabled);
        }

        internal override string ControlType
        {
            get
            {
                return "Label";
            }
        }

        internal override void PollForStateAndUpdate()
        {
            // Attaching a command to the label is not required
            if (string.IsNullOrEmpty(Properties.Command))
                return;

            bool succeeded = PollForStateAndUpdateInternal(Properties.Command,
                                                           Properties.QueryCommand,
                                                           StateProperties,
                                                           false);

            // Only update the state if there is a query command and the poll for state was successful
            if (succeeded && !string.IsNullOrEmpty(Properties.QueryCommand))
            {
                string strInnerTextSav = CUIUtility.SafeString(_strInnerText);

                if (!string.IsNullOrEmpty(StateProperties[LabelCommandProperties.Value]))
                {
                    UIUtility.SetInnerText(_elmDefaultLabel, StateProperties[LabelCommandProperties.Value]);
                    _strInnerText = StateProperties[LabelCommandProperties.Value];
                }
                else
                {
                    UIUtility.SetInnerText(_elmDefaultLabel, Properties.LabelText);
                    _strInnerText = Properties.LabelText;
                }

                // If we're changed the size of the label, then the 
                // whole ribbon may need to be re-scaled.
                if (_strInnerText != strInnerTextSav)
                    Root.NeedScaling = true;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _elmDefault = null;
            _elmDefaultIcon = null;
            _elmDefaultLabel = null;
            _elmSmall = null;
            _elmSmallIcon = null;
        }

        private LabelProperties Properties
        {
            get
            {
                return (LabelProperties)base.ControlProperties;
            }
        }
    }
}
