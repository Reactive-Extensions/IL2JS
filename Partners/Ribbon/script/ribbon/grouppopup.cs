using System;
using Microsoft.LiveLabs.Html;

namespace Ribbon
{
    internal class GroupPopup : Component
    {
        Group _group;
        internal GroupPopup(SPRibbon ribbon, string id, Group group)
            : base(ribbon, id, "", "")
        {
            _group = group;
        }

        Div _elmTitle;
        Div _elmBody;
        internal override void RefreshInternal()
        {
            EnsureDOMElementAndEmpty();

            if (CUIUtility.IsNullOrUndefined(_elmTitle))
            {
                _elmTitle = new Div();
                _elmTitle.ClassName = "ms-cui-groupTitle";
            }
            else
            {
                _elmTitle = (Div)Utility.RemoveChildNodes(_elmTitle);
            }

            if (CUIUtility.IsNullOrUndefined(_elmBody))
            {
                _elmBody = new Div();
                _elmBody.ClassName = "ms-cui-groupBody";
            }
            else
            {
                _elmBody = (Div)Utility.RemoveChildNodes(_elmBody);
            }

            // Refresh the text of the group name
            UIUtility.SetInnerText(_elmTitle, _group.Title);

            ElementInternal.AppendChild(_elmBody);
            ElementInternal.AppendChild(_elmTitle);

            Layout layout = (Layout)_group.GetChildByTitle(_layoutTitle);
            if (CUIUtility.IsNullOrUndefined(layout))
            {
                throw new InvalidOperationException("Cannot find Layout with title: " + _layoutTitle +
                    " for this GroupPopup to use from the Group with id: " + _group.Id);
            }

            // TODO(josefl): fix this.  This cloning for popup groups does not scale.
            // it should only be recloned if the master layout has been dirtied and in 
            // this case, somehow the control should not have to keep track of all the old 
            // stale ControlComponents.
            Layout clonedLayout = (Layout)layout.Clone(true);
            this.RemoveChildren();
            this.AddChild(clonedLayout);
            AppendChildrenToElement(_elmBody);
            base.RefreshInternal();
        }

        string _layoutTitle = "";
        public string LayoutTitle
        {
            get 
            { 
                return _layoutTitle; 
            }
            set
            {
                // If there is not change in the layout title, then just return
                if (CUIUtility.SafeString(_layoutTitle) == value)
                    return;
                _layoutTitle = value;
                OnDirtyingChange();
            }
        }

        protected override string CssClass
        {
            get 
            { 
                return "ms-cui-groupPopup"; 
            }
        }

        MenuLauncher _currentlyOpenedMenu = null;
        internal override bool OnPreBubbleCommand(CommandEventArgs command)
        {
            if (command.Type == CommandType.MenuCreation)
            {
                // We can assume that any control sending a MenuCreation event must be a MenuLauncher
                MenuLauncher source = (MenuLauncher)command.SourceControl;

                // If there's already an open menu, we can just ignore this creation event
                // since it is likely a submenu launching
                if (_currentlyOpenedMenu != null)
                    return base.OnPreBubbleCommand(command);

                _currentlyOpenedMenu = source;

                ShowGlass();
            }
            else if (command.Type == CommandType.MenuClose)
            {
                HideGlass();
                _currentlyOpenedMenu = null;
            }

            return base.OnPreBubbleCommand(command);
        }

        private Span _elmGlass;
        private bool _glassIsShown = false;

        private void ShowGlass()
        {
            if (_glassIsShown)
                return;

            if (_elmGlass == null)
            {
                _elmGlass = Utility.CreateGlassElement();
                _elmGlass.Click += OnGlassClick;
                ElementInternal.AppendChild(_elmGlass);
            }

            _elmGlass.Style.Display = "";
            _glassIsShown = true;
        }

        private void HideGlass()
        {
            if (!_glassIsShown)
                return;

            _elmGlass.Style.Display = "none";
            _glassIsShown = false;
        }

        private void OnGlassClick(HtmlEvent args)
        {
            if (_currentlyOpenedMenu != null)
                _currentlyOpenedMenu.CloseMenu();
            // The MenuClose event handler above will take care of removing the glass and resetting _currentlyOpenedMenu
        }

        public override void Dispose()
        {
            if (!CUIUtility.IsNullOrUndefined(_elmGlass))
                _elmGlass.Click -= OnGlassClick;

            base.Dispose();
        }
    }
}
