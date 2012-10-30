using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Ribbon
{
    // WARNING!!!  Keep this sequence in sync with the ImageSizeToClass array!
    public enum ImgContainerSize
    {
        None = 0,
        Size5by3 = 1,
        Size13by13 = 2,
        Size16by16 = 3,
        Size32by32 = 4,
        Size48by48 = 5,
        Size64by48 = 6,
        Size72by96 = 7,
        Size96by72 = 8,
        Size96by96 = 9,
        /// <summary>
        /// For Jewel button
        /// </summary>
        Size56by24 = 10,
        /// <summary>
        /// For Separator control
        /// </summary>
        Size2by16 = 11,
    }

    public enum MouseButton
    {
        LeftButton = 0,
        MiddleButton = 1,
        RightButton = 2,
    }

    public enum Key
    {
        Backspace = 8,
        Tab = 9,
        Enter = 13,
        Esc = 27,
        Space = 32,
        PageUp = 33,
        PageDown = 34,
        End = 35,
        Home = 36,
        Left = 37,
        Up = 38,
        Right = 39,
        Down = 40,
        Del = 127,
    }

    internal class Bounds
    {
        //   x: The number of pixels between location and left edge of parent frame.
        //   y: The number of pixels between location and top edge of parent frame.
        //   width: The width in pixels.
        //   height: The height in pixels.
        public Bounds(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    internal class Utility
    {
        // WARNING!!!  Keep this sequence in sync with ImgContainerSize!
        internal static string[] ImageSizeToClass = { "", 
                                                      "ms-cui-img-5by3",
                                                      "ms-cui-img-13by13",
                                                      "ms-cui-img-16by16",
                                                      "ms-cui-img-32by32",
                                                      "ms-cui-img-48by48",
                                                      "ms-cui-img-64by48",
                                                      "ms-cui-img-72by96",
                                                      "ms-cui-img-96by72",
                                                      "ms-cui-img-96by96",
                                                      "ms-cui-img-56by24",
                                                      "ms-cui-img-2by16"
                                                    };

        // WARNING!!!  Keep this sequence in sync with GalleryElementDimensions!
        internal static string[] GalleryElementDimensionsToSizeString = {"",
                                                                        "Size16by16",
                                                                        "Size32by32",
                                                                        "Size48by48",
                                                                        "Size64by48",
                                                                        "Size72by96",
                                                                        "Size96by72",
                                                                        "Size96by96",
                                                                        "Size128by128",
                                                                        "Size190by30",
                                                                        "Size190by40",
                                                                        "Size190by50",
                                                                        "Size190by60"
                                                                    };

        /// <summary>
        /// Removes all the child nodes from a DOMElement.
        /// </summary>
        /// <param name="elm"></param>
        /// <returns>A pointer to the replacement node</returns>
        /// <owner alias="FredeM" />
        public static HtmlElement RemoveChildNodes(HtmlElement elm)
        {
            if (CUIUtility.IsNullOrUndefined(elm))
                return null;

            DomNode parent = elm.ParentNode;
            if (parent != null)
            {
                DomNode d = elm.CloneNode(false);
                parent.ReplaceChild(d, elm);
                return (HtmlElement)d;
            }
            else
            {
                DomNode p = elm.FirstChild;
                DomNode aux;
                while (p != null)
                {
                    aux = p.NextSibling;
                    elm.RemoveChild(p);
                    p = aux;
                }
                return elm;
            }
        }

        /// <summary>
        /// Removes all the child nodes from a DOMElement the slow way.
        /// Don't use this unless you have to (see remarks)
        /// </summary>
        /// <param name="elm">A pointer to the node to remove children from</param>
        /// <remarks>
        /// This method should only be used when we can't replace elm in the DOM tree.
        /// If you don't need this guarantee, please use RemoveChildNodes(DOMElement elm) instead.
        /// </remarks>
        /// <owner alias="JKern" />
        public static void RemoveChildNodesSlow(DomNode elm)
        {
            while (elm.HasChildNodes())
            {
                elm.RemoveChild(elm.FirstChild);
            }
        }

        /// <summary>
        /// Makes sure that a css class is on an element
        /// </summary>
        /// <param name="element">the DOMElement</param>
        /// <param name="cssClass">the css class name</param>
        /// <seealso cref="RemoveCSSClassFromElement"/>
        public static void EnsureCSSClassOnElement(HtmlElement element, string cssClass)
        {
            if (CUIUtility.IsNullOrUndefined(element) || string.IsNullOrEmpty(cssClass))
                return;

            string oldValue = element.ClassName;
            if (!string.IsNullOrEmpty(oldValue) && oldValue.IndexOf(cssClass) != -1)
                return;

            string cn = (oldValue.Trim() + " " + cssClass);
            cn = cn.Trim();
            element.ClassName = cn;
        }

        /// <summary>
        /// Check if one element is a descendant of another
        /// </summary>
        /// <param name="parent">
        /// The ancestor element
        /// </param>
        /// <param name="child">
        /// The child element in question
        /// </param>
        /// <returns>
        /// True if child is a descendant of parent, false otherwise
        /// </returns>
        public static bool IsDescendantOf(DomNode parent, DomNode child)
        {
            while (!CUIUtility.IsNullOrUndefined(child))
            {
                try
                {
                    if (child.NodeName.ToLower() == "body")
                    {
                        break;
                    }
                }
                catch
                {
                    // Firefox will sometimes start trying to iterate its own chrome nodes such as
                    // the scrollbar which causes an access denied exception. If we get here, there's
                    // nothing we can do, so just check the condition and break out of the loop
                    if (child == parent)
                        return true;
                    break;
                }

                if (child == parent)
                    return true;
                child = child.ParentNode;
            }
            return false;
        }

        /// <summary>
        /// Makes sure that a css class is not on a DOMElement
        /// </summary>
        /// <param name="element">the DOMElement</param>
        /// <param name="cssClass">the css class name</param>
        /// <seealso cref="EnsureCSSClassOnElement"/>
        public static void RemoveCSSClassFromElement(HtmlElement element, string cssClass)
        {
            if (CUIUtility.IsNullOrUndefined(element) ||
                    string.IsNullOrEmpty(cssClass) ||
                    string.IsNullOrEmpty(element.ClassName))
            {
                return;
            }

            string cn = element.ClassName;
            if (cn != cn.Replace(cssClass, ""))
            {
                element.ClassName = cn.Replace(cssClass, "");
            }
        }

        public static void SetEnabledOnElement(HtmlElement element, bool enabled)
        {
            if (enabled)
                EnableElement(element);
            else
                DisableElement(element);
        }

        /// <summary>
        /// Enable a DOMElement
        /// </summary>
        /// <param name="element">the element to be enabled</param>
        /// <seealso cref="DisableElement"/>
        public static void EnableElement(HtmlElement element)
        {
            if (CUIUtility.IsNullOrUndefined(element))
                return;
            RemoveCSSClassFromElement(element, "ms-cui-disabled");
        }

        /// <summary>
        /// Disable a DOMElement
        /// </summary>
        /// <param name="element">the element to be disabled</param>
        /// <seealso cref="EnableElement"/>
        public static void DisableElement(HtmlElement element)
        {
            if (CUIUtility.IsNullOrUndefined(element))
                return;
            EnsureCSSClassOnElement(element, "ms-cui-disabled");
        }

        public static void SetDisabledAttribute(HtmlElement element, bool disabled)
        {
            if (CUIUtility.IsNullOrUndefined(element))
                return;
            element.Disabled = disabled;
        }

        /// <summary>
        /// Make a link so that it will not navigate the page anywhere when it is invoked.
        /// </summary>
        /// <param name="element"></param>
        internal static void NoOpLink(HtmlElement element)
        {
            element.SetAttribute("href", "javascript:;");
            element.SetAttribute("onclick", "return false;");
        }

        internal static Anchor CreateNoOpLink()
        {
            Anchor elm = new Anchor();
            elm.Href = "javascript:;";
            elm.SetAttribute("onclick", "return false;");
            return elm;
        }

        internal static void Debug(string message)
        {
            Div div = new Div();
            UIUtility.SetInnerText(div, message);
            div.Style.FontSize = "10px";
            div.Style.Color = "red";
            Browser.Document.Body.AppendChild(div);
        }

        internal static bool InBounds(HtmlElement element, int x, int y)
        {
            Bounds position = Utility.GetElementPosition(element);

            return x > position.X &&
                   x < (position.X + position.Width) &&
                   y > position.Y &&
                   y < (position.Y + position.Height);
        }

        internal static Bounds GetElementPosition(HtmlElement element)
        {
            HtmlElement parent = element;
            int x = 0;
            int y = 0;
            int width = element.OffsetWidth;
            int height = element.OffsetHeight;

            while (!CUIUtility.IsNullOrUndefined(parent) &&
                   !string.IsNullOrEmpty(parent.NodeName) &&
                   parent.NodeName.ToLower() != "body")
            {
                // Offsets seem to be the effective margin and client seems to be border
                // Having a style applied to a table element that has padding seems to 
                // break this.
                int cl = parent.ClientLeft;
                int ct = parent.ClientTop;

                x += parent.OffsetLeft + cl;
                y += parent.OffsetTop + ct;
                parent = parent.OffsetParent;
            }
            return new Bounds(x, y, width, height);
        }

        private static readonly ushort[] HTMLCharMap2 =
        {
            // OA: \n    OD: \r
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 7, 0, 0, 6, 0, 0,  // 0000 -- 000F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            // 20: space    22: "    26: &    27: '  
            8, 0, 1, 0, 0, 0, 2, 3, 0, 0, 0, 0, 0, 0, 0, 0,
            // 3C: <    3E: >
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4, 0, 5, 0   // 0030 -- 003F 
        };

        private static readonly string[] HTMLData = 
        {
            "",         // 0
            "&quot;",   // 1  -- "
            "&amp;",    // 2  -- &
            "&#39;",    // 3  -- '
            "&lt;",     // 4  -- <
            "&gt;",     // 5  -- >
            " ",        // 6  -- \r
            "<br>",     // 7  -- \n
            "&nbsp;",   // 8  -- for space followed by another space
            "<b>",      // 9
            "<i>",      //10
            "<p>",     //11
            "<u>",      //12
            "</b>",     //13
            "</i>",     //14
            "</p>",      //15
            "</u>",     //16
        };

        /// <summary>
        /// This encodes the provided string and allows for simple HTML formatting.
        /// </summary>
        /// <param name="valueToEncode">The string to encode</param>
        /// <param name="allowHtmlTags">True if we should allow html tags. False otherwise.</param>
        /// <returns>The encoded string.</returns>
        internal static string HtmlEncodeAllowSimpleTextFormatting(string valueToEncode, bool allowHtmlTags)
        {
            StringBuilder output = new StringBuilder();
            if (string.IsNullOrEmpty(valueToEncode) || 0 == valueToEncode.Length)
            {
                return string.Empty;
            }

            //allow, <br>, <b>, <p>, <i>, <u>, </b>, </i>, </p>, </u>, &nbsp;
            //turns ' ' after "\n" into &nbsp;
            //turns ' ' followed by another into &nbsp;
            //turns '\n' into <br>
            //turns '\r' into ' '
            bool bWaitingForNonWhitespaceAfterBR = false;

            int start = 0;
            int length = 0;
            int i = 0;
            int nValueToEncodeLength = valueToEncode.Length;

            while (i < nValueToEncodeLength)
            {
                int specialCharIdx;
                int ch = valueToEncode[i];
                if (ch < 0x003F)     //All special characters < 0x003F
                {
                    specialCharIdx = HTMLCharMap2[ch];
                }
                else if (ch >= 0x00a0 && ch <= 0x00ff)
                {
                    specialCharIdx = -2;
                }
                else
                {
                    specialCharIdx = 0;
                }

                if (specialCharIdx != 0)
                {
                    if (length > 0)
                    {
                        output.Append(valueToEncode.Substring(start, length));
                        length = 0;
                    }
                    start = i + 1;

                    if (specialCharIdx == -2)
                    {
                        output.Append("&#");
                        output.Append(ch.ToString());
                        output.Append(";");
                    }
                    else if (specialCharIdx == 8) // ' ' ==> "&nbsp;" or " "
                    {
                        char ch1 = valueToEncode[i + 1];
                        if ((ch1 == ' ') || bWaitingForNonWhitespaceAfterBR)
                        {
                            output.Append(HTMLData[specialCharIdx]);
                        }
                        else
                        {
                            output.Append(" ");
                        }
                    }
                    else
                    {
                        if (specialCharIdx == 2 && 
                            allowHtmlTags)
                        { //"&", this could be the start of &nbsp;
                            if (i + 5 < nValueToEncodeLength &&
                                valueToEncode[i + 1] == 'n' &&
                                valueToEncode[i + 2] == 'b' &&
                                valueToEncode[i + 3] == 's' &&
                                valueToEncode[i + 4] == 'p' &&
                                valueToEncode[i + 5] == ';')
                            {
                                output.Append(HTMLData[8]);    //&nbsp;
                                i += 6;
                                start += 5;
                                continue;
                            }
                            // or the start of &#160;
                            if (i + 5 < nValueToEncodeLength &&
                                valueToEncode[i + 1] == '#' &&
                                valueToEncode[i + 2] == '1' &&
                                valueToEncode[i + 3] == '6' &&
                                valueToEncode[i + 4] == '0' &&
                                valueToEncode[i + 5] == ';')
                            {
                                output.Append(HTMLData[8]);    //&#160;
                                i += 6;
                                start += 5;
                                continue;
                            }
                        }
                        else if (specialCharIdx == 4 &&
                                 allowHtmlTags)
                        { //"<", this could be the start of 
                            // <b>, <i>, <u>, </b>, </i>, </u>, or <br>

                            char ch1, ch2, ch3;

                            ch1 = valueToEncode[i + 1];
                            if ('b' == ch1 || 'B' == ch1)
                            {
                                ch2 = valueToEncode[i + 2];
                                if ('>' == ch2)
                                {
                                    output.Append(HTMLData[9]);    //<b>
                                    i += 3;
                                    start += 2;
                                    continue;
                                }
                                else if ('r' == ch2 || 'R' == ch2)
                                {
                                    ch3 = valueToEncode[i + 3];

                                    // <br>
                                    if ('>' == ch3)
                                    {
                                        output.Append(HTMLData[7]); //<br />
                                        i += 4;
                                        start += 3;
                                        continue;
                                    }
                                    // <br/>
                                    if ('/' == ch3)
                                    {
                                        char ch4 = valueToEncode[i + 4];
                                        if (ch4 == '>')
                                        {
                                            output.Append(HTMLData[7]); //<br />
                                            i += 5;
                                            start += 4;
                                            continue;
                                        }
                                    }
                                    // <br />
                                    if (' ' == ch3)
                                    {
                                        char ch4 = valueToEncode[i + 4];
                                        char ch5 = valueToEncode[i + 5];
                                        if (ch4 == '/' && ch5 == '>')
                                        {
                                            output.Append(HTMLData[7]); //<br />
                                            i += 6;
                                            start += 5;
                                            continue;
                                        }
                                    }
                                }
                            }
                            else if ('i' == ch1 || 'I' == ch1)
                            {
                                ch2 = valueToEncode[i + 2];
                                if ('>' == ch2)
                                {
                                    output.Append(HTMLData[10]);    //<i>
                                    i += 3;
                                    start += 2;
                                    continue;
                                }
                            }
                            else if ('p' == ch1 || 'P' == ch1)
                            {
                                ch2 = valueToEncode[i + 2];
                                if ('>' == ch2)
                                {
                                    output.Append(HTMLData[11]);    //<p>
                                    i += 3;
                                    start += 2;
                                    continue;
                                }
                            }
                            else if ('u' == ch1 || 'U' == ch1)
                            {
                                ch2 = valueToEncode[i + 2];
                                if ('>' == ch2)
                                {
                                    output.Append(HTMLData[12]);    //<u>
                                    i += 3;
                                    start += 2;
                                    continue;
                                }
                            }
                            else if ('/' == ch1)
                            {
                                ch3 = valueToEncode[i + 3];
                                if ('>' == ch3)
                                {
                                    ch2 = valueToEncode[i + 2];
                                    if ('b' == ch2 || 'B' == ch2)
                                    {
                                        output.Append(HTMLData[13]);    //</b>
                                        i += 4;
                                        start += 3;
                                        continue;
                                    }
                                    else if ('i' == ch2 || 'I' == ch2)
                                    {
                                        output.Append(HTMLData[14]);    //</i>
                                        i += 4;
                                        start += 3;
                                        continue;
                                    }
                                    else if ('p' == ch2 || 'P' == ch2)
                                    {
                                        output.Append(HTMLData[15]);    //</p>
                                        i += 4;
                                        start += 3;
                                        continue;
                                    }
                                    else if ('u' == ch2 || 'U' == ch2)
                                    {
                                        output.Append(HTMLData[16]);    //</u>
                                        i += 4;
                                        start += 3;
                                        continue;
                                    }
                                }
                            }
                        }
                        else if (specialCharIdx == 7)  // '\n' ==> "<br>"
                        {
                            bWaitingForNonWhitespaceAfterBR = true;
                        }
                        else
                        {
                            bWaitingForNonWhitespaceAfterBR = false;
                        }
                        output.Append(HTMLData[specialCharIdx]);
                    }

                }
                else
                {
                    bWaitingForNonWhitespaceAfterBR = false;
                    length++;
                }
                i++;
            }

            if (start < nValueToEncodeLength)
            {
                output.Append(valueToEncode.Substring(start, nValueToEncodeLength - start));
            }

            return output.ToString();
        }

        // Because right now we always want to set nearly everything to unselectable
        // and to suppress the context menu, I'm overloading this to do them both.
        // If we need to separate them in the future, then we can add an additional 
        // argument to this function or split it out into its own function.
        internal static void SetUnselectable(HtmlElement element, bool on, bool recursive)
        {
            // Apparently text nodes do not have an "unselectable" property.
            if (CUIUtility.IsNullOrUndefined(element) || string.Compare(element.NodeName.ToLower(), "#text") == 0)
                return;

            if (on)
                element.SetAttribute("unselectable", "on");
            else
                element.SetAttribute("unselectable", "off");

            if (recursive)
            {
                DomNode p = element.FirstChild;
                while (p != null)
                {
                    SetUnselectable((HtmlElement)p, on, true);
                    p = p.NextSibling;
                }
            }
        }

        internal static Span CreateClusteredImageContainerNew(ImgContainerSize size,
                                                                     string imageSource,
                                                                     string imageClass,
                                                                     Image elmImage,
                                                                     bool inlineBlockDisplay,
                                                                     bool blockDisplay,
                                                                     string imageTop,
                                                                     string imageLeft)
        {
            Span cont = new Span();
            if (blockDisplay)
                Utility.EnsureCSSClassOnElement(cont, "ms-cui-block");

            string cssClass = ImageSizeToClass[(int)size];

            // Cluster mode detection
            bool hasTop = !string.IsNullOrEmpty(imageTop);
            bool hasLeft = !string.IsNullOrEmpty(imageLeft);
            bool hasClass = !string.IsNullOrEmpty(imageClass);

            // These classes are needed for unclustered ribbon icon disabling.
            if (inlineBlockDisplay)
                cssClass += " ms-cui-img-cont-float";
            else
                cssClass += " ms-cui-img-container";

            // Revisit this.  Right now we dont' append here to help on performance
            if (hasClass)
                elmImage.ClassName = imageClass;

            cont.ClassName += " " + cssClass;
            if (!string.IsNullOrEmpty(imageSource))
            {
                cont.AppendChild(elmImage);
                elmImage.Src = imageSource;
                if (hasTop)
                    elmImage.Style.Top = imageTop + "px";
                if (hasLeft)
                    elmImage.Style.Left = imageLeft + "px";
            }
            return cont;
        }

        internal static void PrepareClusteredBackgroundImageContainer(HtmlElement container,
                                                                      string imageSource,
                                                                      string imageClass,
                                                                      string imageTop,
                                                                      string imageLeft,
                                                                      string imageWidth,
                                                                      string imageHeight)
        {
            container.Style.Display = "inline-block";

            if (!string.IsNullOrEmpty(imageClass))
                container.ClassName = imageClass;
            if (!string.IsNullOrEmpty(imageHeight))
                container.Style.Height = imageHeight + "px";
            if (!string.IsNullOrEmpty(imageWidth))
                container.Style.Width = imageWidth + "px";
            if (!string.IsNullOrEmpty(imageSource))
                container.Style.BackgroundImage = "url(" + imageSource + ")";

            string bgx = "0px", bgy = "0px";
            bool hasBackgroundPosition = false;
            if (!string.IsNullOrEmpty(imageLeft))
            {
                bgx = imageLeft + "px";
                hasBackgroundPosition = true;
            }
            if (!string.IsNullOrEmpty(imageTop))
            {
                bgy = imageTop + "px";
                hasBackgroundPosition = true;
            }
            if (hasBackgroundPosition)
            {
                container.Style.BackgroundPosition = bgx + " " + bgy;
            }
        }

        /// <summary>
        /// Creates a hidden Iframe element.
        /// </summary>
        /// <returns>The IFrameElement created. </returns>
        internal static IFrame CreateHiddenIframeElement()
        {
            IFrame _elmFrame = new IFrame();
            _elmFrame.Style.Position = "absolute";
            _elmFrame.Style.Visibility = "hidden";
            return _elmFrame;
        }

        internal static Span CreateGlassElement()
        {
            Span elm = new Span();

            if (BrowserUtility.InternetExplorer)
            {
                elm.ClassName = "ms-cui-glass-ie";
            }
            else
            {
                elm.ClassName = "ms-cui-glass-ff";
            }

            return elm;
        }

        /// <summary>
        /// Sets the size and location of a background iframe to cover the area behind the flyout.
        /// </summary>
        /// <remarks>
        /// The background iframe is positioned under a flyout when the flyout is launched in InternetExplorer. Without
        /// this, the flyout appears under ActiveX controls on the page.
        /// </remarks>
        /// <param name="elmFlyout">The flyout to be launched.</param>
        /// <param name="elmBackFrame">The iframe positioned under the flyout.</param>
        internal static void PositionBackFrame(IFrame elmBackFrame, HtmlElement elmFlyout)
        {
            elmBackFrame.Style.Position = "absolute";
            elmBackFrame.Style.Visibility = "hidden";
            elmBackFrame.Style.Left = elmFlyout.Style.Left;
            elmBackFrame.Style.Top = elmFlyout.Style.Top;

            int realWidth = elmFlyout.OffsetWidth;
            int realHeight = elmFlyout.OffsetHeight;
            elmBackFrame.Style.Width = realWidth.ToString() + "px";
            elmBackFrame.Style.Height = realHeight.ToString() + "px";

            elmBackFrame.Style.Visibility = "visible";
        }

        /// <summary>
        /// Sets the Aria 'describedby' attribute for controls that have a SuperToolTip defined.
        /// </summary>
        /// <remarks>
        /// The 'aria-describedby' attribute points to the SuperToolTip defined for this control. It is used by Assistive Technologies to indicate
        /// the DOM element containing more information on the selected control.
        /// </remarks>
        /// <param name="properties">The control's properties.</param>
        /// <param name="focusElement">The element of the control that recieves focus when the control is selected.</param>
        internal static void SetAriaTooltipProperties(ControlProperties properties, HtmlElement focusElement)
        {
            if (!CUIUtility.IsNullOrUndefined(properties) &&
                    !CUIUtility.IsNullOrUndefined(focusElement) &&
                    !string.IsNullOrEmpty(properties.ToolTipTitle))
                focusElement.SetAttribute("aria-describedby", properties.Id + "_ToolTip");
        }

        /// <summary>
        /// This method is used to format the text for a large control.  A large control
        /// in the ribbon has either one or two lines for text and sometimes an arrow.
        /// We use a region with style:  "white-space:pre;" for this.  This means that
        /// Newlines in the text will end up being a literal newline in the summary.
        /// The algorithm below first looks for a special newline character:
        /// This character is:  "\u200b\u200b" in javascript
        /// This character is: "&#x200b;&#x200b;" in XML
        /// If it finds one of these, then it replaces the last one with a literal newline.
        /// If it does not find the special character, then it looks for a space ' ' and puts
        /// a newline at the last space.
        /// If this is an arrow control (SplitButton, FlyoutAnchor etc.), and there is no special
        /// character or space, then it puts the newline at the very end of the string which
        /// has the effect of putting the arrow on a newline by itself.      
        /// </browser>
        /// <param name="text">The text that needs to be "fixed". It's a plain text and is not HTMLEncoded.</param>
        /// <param name="arrow">Whether this control has an arrow after its text (FlyoutAnchor, SplitButton etc.)</param>
        /// <returns>The return value is a HTML string.</returns>
        internal static string FixLargeControlText(string text, bool arrow)
        {
            // WARNING:  KEEP IN SYNC WITH %WCUI$\SERVER\COMMANDUI\RIBBON.CS
            string result;
            string newLineChar = "<br />";

            int idxChar = text.LastIndexOf("\u200b\u200b");
            int idxSpace = text.LastIndexOf(' ');

            if (idxChar != -1)
            {
                // If the special character is present, then we replace it
                // with a newline.
                result = HtmlEncode(text.Substring(0, idxChar)) + newLineChar;
                if (idxChar < text.Length)
                    result += HtmlEncode(text.Substring(idxChar + 2));
            }
            else if (idxSpace != -1)
            {
                // If there are any spaces, then we replace the last one with a newline.
                result = HtmlEncode(text.Substring(0, idxSpace)) + newLineChar;
                if (idxSpace < text.Length)
                    result += HtmlEncode(text.Substring(idxSpace + 1));
                if (arrow)
                    result += " ";
            }
            else if (idxSpace == -1 && arrow)
            {
                // If this is a control with an arrow and the text doesn't have a space,
                // then we put the newline at the end of the string (right before the arrow)
                // so that it (the arrow) will appear on the second line.
                result = HtmlEncode(text) + newLineChar;
            }
            else
            {
                result = HtmlEncode(text);
            }

            return result;
        }
        internal static string HtmlEncode(string str)
        {
            if (string.IsNullOrEmpty(str))
                return "";

            StringBuilder sb = new StringBuilder();
            int length = str.Length;
            for (int i = 0; i < length; i++)
            {
                char ch = str[i];
                switch (ch)
                {
                    case '<':
                        sb.Append("&lt;");
                        break;
                    case '>':
                        sb.Append("&gt;");
                        break;
                    case '&':
                        sb.Append("&amp;");
                        break;
                    case '\"':
                        sb.Append("&quot;");
                        break;
                    case '\'':
                        sb.Append("&#39;");
                        break;
                    default:
                        sb.Append(ch.ToString());
                        break;
                }
            }
            return sb.ToString();
        }

        internal static bool IsTrue(string value)
        {
            return string.IsNullOrEmpty(value) ?
                false : (string.Compare(value.ToLower(), "true") == 0);
        }

        internal static int GetViewPortWidth()
        {
            // Get dimensions of viewport
            // IE7, Mozilla, Firefox, Opera, Safari
            int width = Browser.Window.InnerWidth;
            // IE6 Standards Mode
            if (CUIUtility.IsNullOrUndefined(width) || width == 0)
                width = Browser.Document.DocumentElement.ClientWidth;
            // IE6 Quirks Mode & IE<6
            if (CUIUtility.IsNullOrUndefined(width) || width == 0)
                width = Browser.Document.Body.ClientWidth;
            return width;
        }

        internal static int GetViewPortHeight()
        {
            // Get dimensions of viewport
            // IE7, Mozilla, Firefox, Opera, Safari
            int height = Browser.Window.InnerHeight;
            // IE6 Standards Mode
            if (CUIUtility.IsNullOrUndefined(height) || height == 0)
                height = Browser.Document.DocumentElement.ClientHeight;
            // IE6 Quirks Mode & IE<6
            if (CUIUtility.IsNullOrUndefined(height) || height == 0)
                height = Browser.Document.Body.ClientHeight;
            return height;
        }

        /// <summary>
        /// Gets the first DOM element that is a child of a given parent element that has a given class applied to it
        /// </summary>
        /// <param name="parent">The parent element</param>
        /// <param name="className">The class name to look for</param>
        /// <returns>A DOM Element reference to the first element in parent with the given class name applied</returns>
        internal static HtmlElement GetFirstChildElementByClassName(HtmlElement parent, string className)
        {
            HtmlElementCollection result = parent.QuerySelectorAll(className);
            if (!CUIUtility.IsNullOrUndefined(result) && result.Length > 0)
                return result[0];

            return null;
        }

        internal static HtmlElement GetNearestContainingParentElementOfType(HtmlElement elem, string strTagName)
        {
            return GetNearestContainingParentElementOfTypes(elem, new string[] { strTagName });
        }

        // REVIEW(josefl):  This was copied out of rteutility.cs  It would be good if we can 
        // unify these into one low level utility sometime in the future.
        // This function finds the first applicable parent element of the element type.
        internal static HtmlElement GetNearestContainingParentElementOfTypes(HtmlElement elem, string[] aTagNames)
        {
            int aTagNames_length = aTagNames.Length;

            if (CUIUtility.IsNullOrUndefined(elem))
            {
                return null;
            }

            // check the current element first.
            for (int i = 0; i < aTagNames_length; i++)
            {
                if (string.Compare(elem.TagName.ToLower(), aTagNames[i].ToLower()) == 0)
                {
                    return elem;
                }
            }

            // walk the parent chain to find the element type of interest.
            HtmlElement elemParent = (HtmlElement)elem.ParentNode;
            while (elemParent != null)
            {
                for (int i = 0; i < aTagNames_length; i++)
                {
                    if (string.Compare(elemParent.TagName.ToLower(), aTagNames[i].ToLower()) == 0)
                    {
                        return elemParent;
                    }
                }
                elemParent = (HtmlElement)elemParent.ParentNode;
            }
            return null;
        }

        internal static Label CreateHiddenLabel(string text)
        {
            Label ret = new Label();
            ret.SetAttribute("unselectable", "on");
            ret.ClassName = "ms-cui-hidden";
            UIUtility.SetInnerText(ret, text);
            return ret;
        }

        internal static void SetImeMode(HtmlElement e, string mode)
        {
            if (!string.IsNullOrEmpty(mode))
                e.Style.ImeMode = IsTrue(mode) ? "auto" : "disabled";
        }

        public static void ReturnFalse(HtmlEvent args)
        {
            CancelEventUtility(args, true, true);
        }

        public static void CancelEventUtility(HtmlEvent args, bool ret, bool prop)
        {
            if (CUIUtility.IsNullOrUndefined(args))
                return;

            if (BrowserUtility.InternetExplorer)
            {
                if (ret)
                    args.ReturnValue = false;
                if (prop)
                    args.CancelBubble = true;
            }
            else
            {
                if (ret)
                    args.PreventDefault();
                if (prop)
                    args.StopPropagation();
            }
        }
    }

    internal static class CUIUtility
    {
        public static bool IsNullOrUndefined(object obj)
        {
            return null == obj;
        }

        public static string SafeString(string input)
        {
            return CUIUtility.IsNullOrUndefined(input) ? string.Empty : input;
        }
    }

    internal static class BrowserUtility
    {
        public static void InitBrowserUtility()
        {
            FireFox = false;
            InternetExplorer = false;
            InternetExplorer7 = false;
            InternetExplorer8Standard = false;

            string agt = Browser.Window.Navigator.UserAgent.ToLower();
            InternetExplorer = (agt.IndexOf("msie") != -1);
            if (InternetExplorer)
            {
		        string stIEVer = agt.Substring(agt.IndexOf("msie ") + 5);
                var j = stIEVer.IndexOf(';');
                if (j > 0)
                    stIEVer = stIEVer.Substring(0, j);
                var v = default(double);
                if (double.TryParse(stIEVer, out v))
                    InternetExplorer7 = v == 7.0;

                // In IE8, the version number, more specifically the number follows msie in userAgent string is not necessary 8,
                // Thus we should really look at documentMode to see how the page is rendered. The documentMode can be 8,7,5
                InternetExplorer8Standard = Browser.Document.DocumentMode == 8;
            }

            bool nav = ((agt.IndexOf("mozilla") != -1) && ((agt.IndexOf("spoofer") == -1) && (agt.IndexOf("compatible") == -1)));
            FireFox = nav && (agt.IndexOf("firefox") != -1);
        }

        public static bool FireFox { get; private set; }
        public static bool InternetExplorer { get; private set; }
        public static bool InternetExplorer7 { get; private set; }
        public static bool InternetExplorer8Standard { get; private set; }
    }

    internal static class UIUtility
    {
        public static void RemoveNode(HtmlElement elem)
        {
            if (elem.ParentNode != null)
            {
                elem.ParentNode.RemoveChild(elem);
            }
        }
        public static int CalculateOffsetLeft(HtmlElement elem)
        {
            int total = 0;
            while (elem != null)
            {
                total += elem.OffsetLeft;
                elem = elem.OffsetParent;
            }
            return total;
        }
        public static int CalculateOffsetTop(HtmlElement elem)
        {
            int total = 0;
            while (elem != null)
            {
                total += elem.OffsetTop;
                elem = elem.OffsetParent;
            }
            return total;
        }
        public static void SetInnerText(HtmlElement elem, string text)
        {
            NativeUtility.SetInnerText(elem, text);
        }

        // Encode a string for JSON or for javascript string literal
        public static string EcmaScriptStringLiteralEncode(string str)
        {
            if(string.IsNullOrEmpty(str))
                return "";

            string strIn = str;
            int max = strIn.Length;
            List<string> strOut = new List<string>();
            for (int ix = 0; ix < max; ix++)
            {
                char charCode = strIn[ix];
                if (charCode > 0x0fff)
                {
                    // handle the unicode
                    strOut.Add("\\u" + charCode.ToString().ToUpper());
                }
                else if (charCode > 0x00ff)
                {
                    // handle the unicode
                    strOut.Add("\\u0" + charCode.ToString().ToUpper());
                }
                else if (charCode > 0x007f)
                {
                    // handle the unicode
                    strOut.Add("\\u00" + charCode.ToString().ToUpper());
                }
                else
                {
                    char c = strIn[ix];
                    switch (c)
                    {
                        case '\n':
                            strOut.Add("\\n");
                            break;
                        case '\r':
                            strOut.Add("\\r");
                            break;
                        case '\"':
                            strOut.Add("\\u0022");
                            break;
                        case '%':
                            strOut.Add("\\u0025");
                            break;
                        case '&':
                            strOut.Add("\\u0026");
                            break;
                        case '\'':
                            strOut.Add("\\u0027");
                            break;
                        case '(':
                            strOut.Add("\\u0028");
                            break;
                        case ')':
                            strOut.Add("\\u0029");
                            break;
                        case '+':
                            strOut.Add("\\u002b");
                            break;
                        case '/':
                            strOut.Add("\\u002f");
                            break;
                        case '<':
                            strOut.Add("\\u003c");
                            break;
                        case '>':
                            strOut.Add("\\u003e");
                            break;
                        case '\\':
                            strOut.Add("\\\\");
                            break;
                        default:
                            strOut.Add(c.ToString());
                            break;
                    };
                }
            }

            string result = "";
            foreach (string strVal in strOut)
                result += strVal;
            return result;
        }
    }

    #region External Scripts
    // This should be internal but ship/debug compile this name differently so that 
    // we cannot reliably use its name in ScriptTemplate.txt.  So, I'm making this
    // public for now hoping that this will be resolved perhaps by a future
    // release of Script#.  (josefl)
    [Import]
    public static class NativeUtility
    {
        // Verify whether a method or property is available
        [Import(@"function(elem, text) { var doc = elem.ownerDocument; if (doc.createTextNode){var textNode = doc.createTextNode(text); elem.innerHTML = ''; elem.appendChild(textNode);} else {elem.innerText = text;} }")]
        extern public static void SetInnerText(HtmlElement elm, string text);

        // Execute Ribbon scaling computations
        [Import(@"function(elmTopBars, isRtl) { if (typeof(_ribbonScaleHeader) == 'function') _ribbonScaleHeader(elmTopBars, isRtl); }")]
        extern public static void RibbonScaleHeader(HtmlElement elmTopBars, bool isRtl);

        // Verify scripts loaded and ready to init
        [Import(@"function() { return typeof(_ribbonReadyForInit) == 'function' && _ribbonReadyForInit(); }")]
        extern public static bool RibbonReadyForInit();

        // Update Ribbon to show Loading text before changing tab
        [Import(@"function(ribbon) { if (typeof(_ribbonOnStartInit) == 'function') _ribbonOnStartInit(ribbon)}")]
        extern public static void RibbonOnStartInit(JSObject ribbon);

        // Get global var from the window object
        [Import(@"function() { return typeof _v_rg_spbutton != ""undefined"" ? _v_rg_spbutton : null; }")]
        extern public static JSObject GetSPButton();

        // Update the dimensions of the elements on the page
        [Import(@"function() { if (typeof(FixRibbonAndWorkspaceDimensions) == 'function') FixRibbonAndWorkspaceDimensions(); }")]
        extern public static void FixRibbonAndWorkspaceDimensions();

        // Update dimensions after tab switch
        [Import(@"function(min) { if (typeof(OnRibbonMinimizedChanged) == 'function') OnRibbonMinimizedChanged(min); }")]
        extern public static void OnRibbonMinimizedChanged(bool minimized);
    }
    #endregion

    #region Perf Metrics
    // This is the proxy for the PerfMetrics API
    public static class PMetrics
    {
        private class PRecord
        {
            public PMarker m { get; set; }
            public DateTime mt { get; set; }
        }

        private static List<PRecord> g_records;
        private static List<PRecord> Records
        {
            get
            {
                if (CUIUtility.IsNullOrUndefined(g_records))
                    g_records = new List<PRecord>();
                return g_records;
            }
        }

        [Export]
        public static void PerfMark(PMarker mark)
        {
            PRecord r = new PRecord();
            r.m = mark;
            r.mt = DateTime.Now;

            if (Records.Count == 1000)
                Records.Clear();
            Records.Add(r);
        }

        [Export]
        public static void PerfReport()
        {
            int l = Records.Count;
            if (l == 0)
                return;

            Div elmResults = (Div)Browser.Document.GetById("perf-markers");
            if (CUIUtility.IsNullOrUndefined(elmResults))
            {
                elmResults = new Div();
                elmResults.Id = "perf-markers";
                elmResults.Style.Position = "fixed";
                elmResults.Style.Right = "0px";
                elmResults.Style.Bottom = "0px";
                elmResults.Style.BorderColor = "#000000";
                elmResults.Style.BorderStyle = "outset";
                elmResults.Style.BorderWidth = "2px";
                elmResults.Style.BackgroundColor = "#e0e0e0";
                elmResults.Style.FontFamily = "Helvetica";
                elmResults.Style.FontSize = "10pt";
            }

            int recordCount = Records.Count;

            if (recordCount != 2)
            {
                Records.Clear();
                return;
            }

            PRecord start = Records[0];
            PRecord finish = Records[1];
            TimeSpan diff = finish.mt - start.mt;

            // string rstr = "";
            // rstr += "<p><span class='startTime'>Start Time : " + start.mt.ToString() + "</span></p>";
            // rstr += "<p><span class='difference'>Difference :" + diff.Milliseconds + " (ms)</span></p>";
            // rstr += "<p><span class='finishTime'>Finish Time : " + finish.mt.ToString() + "</span></p>";
            // elmResults.InnerHtml += rstr;

            elmResults.InnerText = "Time: " + diff.Milliseconds + "ms";

            Browser.Document.Body.AppendChild(elmResults);
            Records.Clear();
        }

        [Export]
        public static void PerfClear()
        {
            Records.Clear();
        }
    }

    // Performance markers for the client JavaScript perf-metrics.
    // WARNING: If you need to add additional markers, 
    // you'll also need to add those to "\\dev14\otools\inc\misc\perfhost.h" before check-in
    // This is to keep them in sync with other perfmarkers to prevent future conflicts.
    public enum PMarker
    {
        beginSession = 1,
        endSession = 2,
        perfCUIRibbonInitStart = 7103,
        perfCUIRibbonInitPercvdEnd = 7104,
        perfCUIRibbonTabSwitchWarmStart = 7105,
        perfCUIRibbonTabSwitchWarmPercvdEnd = 7106,
        perfCUIRibbonTabSwitchWarmEnd = 7107,
        perfCUIRibbonCompleteConstruction = 7108,
        perfCUIRibbonQueryDataStart = 7109,
        perfCUIRibbonQueryDataEnd = 7110,
        perfWSSWikiUpdatePanelStart = 7111,
        perfWSSWikiUpdatePanelEnd = 7112,
        perfWSSWebPartComponentMouseClickStart = 7186,
        perfWSSWebPartComponentMouseClickEnd = 7187,
        perfCUIAddAndPositionBackFrameStart = 7188,
        perfCUIAddAndPositionBackFrameEnd = 7189,
        perfCUIFlyoutAnchorOnClickStart = 7190,
        perfCUIFlyoutAnchorOnClickEnd = 7191,
        perfCUIDropDownOnArrowButtonClickStart = 7192,
        perfCUIDropDownOnArrowButtonClickEnd = 7193,
        perfWSSBreadcrumbStart = 7386,
        perfWSSBreadcrumbEnd = 7387,
        perfWSSSelectOrDeselectAllStart = 7388,
        perfWSSSelectOrDeselectAllEnd = 7389,
        perfWSSSelectItemStart = 7390,
        perfWSSSelectItemEnd = 7391,
        perfWSSFilterSortStart = 7392,
        perfWSSFilterSortEnd = 7393,
        perfWSSMMUOpenStart = 7394,
        perfWSSMMUOpenEnd = 7395,
        perfWSSECBClickStart = 7396,
        perfWSSECBClickEnd = 7397,
        perfSPSSaveStatusNoteBegin = 7634,
        perfSPSSaveStatusNoteEnd = 7635,
        perfWSSCalendarRenderStart = 7644,
        perfWSSCalendarRenderEnd = 7645,
        perfPLTxInstrumentStart = 7698,
        perfPLTxInstrumentEnd = 7699,
        perfCUIRibbonButtonOnClickStart = 7700,
        perfCUIRibbonButtonOnClickEnd = 7701,
        perfCUIRibbonInsertTableOnClickStart = 7702,
        perfCUIRibbonInsertTableOnClickEnd = 7703,
        perfCUIRibbonToggleButtonOnClickStart = 7704,
        perfCUIRibbonToggleButtonOnClickEnd = 7705,
        perfWSSDialogShow = 7706,
        perfWSSDialogClosed = 7707,
        perfWSSRTEDialogOnLoadEnd = 7708,
        perfWSSRTEDialogOnOkButtonClickStart = 7709,
        perfWSSRTEAutoCompleteSetResultsStart = 7710,
        perfWSSRTEAutoCompleteSetResultsEnd = 7711,
        perfCUIRibbonEditWikiPageStart = 7735,
        perfCUIRibbonEditWikiPageEnd = 7736,
    }
    #endregion
}
