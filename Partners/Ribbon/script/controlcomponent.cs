using System;
using Microsoft.LiveLabs.Html;

using ControlType = Ribbon.Control;

namespace Ribbon
{
    internal class ControlComponent : Component
    {
        // The Control that this Component represents in the ui
        Control _control;

        public ControlComponent(Root root,
                                string id,
                                string displayMode,
                                Control control)
            : base(root, id, displayMode, "")
        {
            _control = control;
        }

        public string DisplayMode
        {
            get
            {
                return Title;
            }
        }

        // Control Components do not have children except in rare circumstances
        // Avoid creating the _children Array for these to save performance
        protected override void CreateChildArray()
        {
        }

        internal override void RefreshInternal()
        {
            Dirty = false;
        }

        internal override void AttachDOMElements()
        {
            Control.AttachDOMElementsForDisplayMode(DisplayMode);
        }

        internal override void AttachEvents()
        {
            Control.AttachEventsForDisplayMode(DisplayMode);
        }

        internal override HtmlElement ElementInternal
        {
            get
            {
                return _control.GetDOMElementForDisplayMode(Title);
            }
            set
            {
                throw new ArgumentException("Cannot set the DOM Element of ControlComponents.  They get their DOM Elements from the Control.");
            }
        }

        // Because ControlComponent overrides Element, this is needed so that
        // subclasses can get at the real Element for the Component
        protected HtmlElement ComponentElement
        {
            get
            {
                return base.ElementInternal;
            }
        }

        internal ControlType Control
        {
            get
            {
                return _control;
            }
        }

        public override bool Enabled
        {
            get
            {
                return _control.Enabled;
            }
            set
            {
                _control.Enabled = value;
            }
        }

        #region Position & Size
        // We don't cache these values since they can change often due to scaling and we don't want to
        // iterate through all components to invalidate them
        internal override int ComponentWidth
        {
            get
            {
                if ((_componentWidth == -1 || ValueIsDirty(_lastWidthUpdate)) && !CUIUtility.IsNullOrUndefined(ElementInternal))
                {
                    _componentWidth = ElementInternal.OffsetWidth;

                }
                return _componentWidth;
            }
        }

        internal override int ComponentHeight
        {
            get
            {
                if ((_componentHeight == -1 || ValueIsDirty(_lastHeightUpdate)) && !CUIUtility.IsNullOrUndefined(ElementInternal))
                {
                    _componentHeight = ElementInternal.OffsetHeight;

                }
                return _componentHeight;
            }
        }

        internal override int ComponentTopPosition
        {
            get
            {
                if ((_componentTopPosition == -1 || ValueIsDirty(_lastTopUpdate)) && !CUIUtility.IsNullOrUndefined(ElementInternal))
                {
                    _componentTopPosition = UIUtility.CalculateOffsetTop(ElementInternal);

                }
                return _componentTopPosition;
            }
        }

        internal override int ComponentLeftPosition
        {
            get
            {
                if ((_componentLeftPosition == -1 || ValueIsDirty(_lastLeftUpdate)) && !CUIUtility.IsNullOrUndefined(ElementInternal))
                {
                    _componentLeftPosition = UIUtility.CalculateOffsetLeft(ElementInternal);

                }
                return _componentLeftPosition;
            }
        }
        #endregion

        internal override void SetEnabledRecursively(bool enabled)
        {
            _control.SetEnabledAndForceUpdate(enabled);
        }

        internal override Component Clone(bool deep)
        {
            return Control.CreateComponentForDisplayMode(DisplayMode);
        }

        protected override void OnEnabledChanged(bool enabled)
        {
            _control.OnEnabledChanged(enabled);
        }

        protected override void EnsureCorrectChildType(Component child)
        {
            _control.EnsureCorrectChildType(child);
        }

        internal override bool OnPreBubbleCommand(CommandEventArgs command)
        {
            return _control.OnPreBubbleCommand(command);
        }
        internal override void OnPostBubbleCommand(CommandEventArgs command)
        {
            _control.OnPostBubbleCommand(command);
        }

        public string TextValue
        {
            get
            {
                return ((IMenuItem)Control).GetTextValue();
            }
        }

        internal override void PollForStateAndUpdateInternal()
        {
            Control.PollForStateAndUpdate();
        }

        /// <summary>
        /// UNDER DEVELOPMENT AND MAY CHANGE
        /// </summary>
        public override void ReceiveFocus()
        {
            ((IMenuItem)Control).ReceiveFocus();
        }

        // TODO: revisit, should this be public or internal to allow more extensibility?
        public override void OnMenuClosed()
        {
            ((IMenuItem)Control).OnMenuClosed();
        }

        internal override bool SetFocusOnFirstControl()
        {
            return Control.SetFocusOnControl();
        }

        public override void Dispose()
        {
            Control.Dispose();
            _control = null;
            base.Dispose();
        }
    }
}
