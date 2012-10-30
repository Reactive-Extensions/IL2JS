using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Ribbon.Controls
{
    [Import(MemberNameCasing = Casing.Exact)]
    public class SeparatorProperties : ControlProperties
    {
        extern public SeparatorProperties();
        extern public string Image { get; }
        extern public string ImageClass { get; }
        extern public string ImageTop { get; }
        extern public string ImageLeft { get; }
    }

    /// <summary>
    /// A simple separator control, used in Toolbars
    /// </summary>
    internal class Separator : Control
    {
        Image _elmImage;
        Span _elmImageCont;
        Span _elmSmall;

        public Separator(Root root, string id, SeparatorProperties properties)
            : base(root, id, properties)
        {
            AddDisplayMode("Small");
        }

        protected override HtmlElement CreateDOMElementForDisplayMode(string displayMode)
        {
            switch (displayMode)
            {
                case "Small":
                    _elmImage = new Image();
                    _elmImage.Style.Cursor = "default";
                    _elmImageCont = Utility.CreateClusteredImageContainerNew(
                                                                    ImgContainerSize.Size2by16,
                                                                    Properties.Image,
                                                                    Properties.ImageClass,
                                                                    _elmImage,
                                                                    true,
                                                                    false,
                                                                    Properties.ImageTop,
                                                                    Properties.ImageLeft
                                                                    );

                    _elmSmall = new Span();
                    _elmSmall.ClassName = "ms-cui-ctl ms-cui-ctl-small ms-cui-separator";
                    _elmSmall.AppendChild(_elmImageCont);
                    return _elmSmall;
                default:
                    EnsureValidDisplayMode(displayMode);
                    break;
            }
            return null;
        }

        internal override void AttachDOMElementsForDisplayMode(string displayMode)
        {
            Span elm = (Span)Browser.Document.GetById(Id + "-" + displayMode);
            this.StoreElementForDisplayMode(elm, displayMode);

            switch (displayMode)
            {
                case "Small":
                    _elmImageCont = elm;
                    break;
            }
        }

        public override void OnEnabledChanged(bool enabled)
        {
        }

        internal override string ControlType
        {
            get
            {
                return "Separator";
            }
        }

        internal override void PollForStateAndUpdate()
        {
        }

        public override void Dispose()
        {
            base.Dispose();
            _elmImage = null;
            _elmImageCont = null;
            _elmSmall = null;
        }

        private SeparatorProperties Properties
        {
            get
            {
                return (SeparatorProperties)base.ControlProperties;
            }
        }
    }
}
