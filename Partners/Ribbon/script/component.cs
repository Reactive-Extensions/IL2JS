using System;
using System.Collections.Generic;
using Microsoft.LiveLabs.Html;

using RootType = Ribbon.Root;

namespace Ribbon
{
    delegate Component DelayedInitHandler(Component component, object data, object options);
    /// <summary>
    /// Component is the basic building block for the in-memory Root hierarchical structure.  
    /// All the pieces in the hierarchical structure are Components.  This class provides some functionality like adding/removing children, registering with the Root, refreshing, adding DOMElements of subcomponents etc.
    /// </summary>
    public abstract class Component : IMenuItem, IDisposable
    {
        string _id;
        HtmlElement _elmDOM;
        Component _parent;
        Root _root;
        List<Component> _children;

        bool _dirty = true;
        bool _visible = true;
        bool _enabled = true;
        bool _enabledHasBeenSet = false;
        string _description;
        string _title;

        /// <summary>
        /// Constructs a command UI Component.
        /// </summary>
        /// <param name="root">the Root Component that this Component is under</param>
        /// <param name="id">the id of this Component</param>
        /// <param name="title">the title of this Component</param>
        /// <param name="description">the description of this Component</param>
        protected Component(Root root,
                            string id,
                            string title,
                            string description)
        {
            _id = id;
            _root = root;
            _title = title;
            _description = description;
            CreateChildArray();
        }

        /// <summary>
        /// By default all Components has an array of children.  However, there are some Components that never or rarely ever have children.  For these Components, we can save on performance cost by not creating the child array.  This method can be overriden and made a no-op in subclasses that do not need a child array created in the constructor.
        /// </summary>
        protected virtual void CreateChildArray()
        {
            EnsureChildren();
        }

        /// <summary>
        /// Ensure that the internal array of children has been created.
        /// </summary>
        internal void EnsureChildren()
        {
            if (CUIUtility.IsNullOrUndefined(_children))
                _children = new List<Component>();
        }

        /// <summary>
        /// The string identifier of this Component.  This uniquely identifies it within the Root.
        /// </summary>
        public string Id
        {
            get
            {
                return _id;
            }
            internal set
            {
                _id = value;
            }
        }

        /// <summary>
        /// The DOMElement of this Component.  This is used in a recursive manner when  building up the DOM structure for the Root.
        /// </summary>
        internal virtual HtmlElement ElementInternal
        {
            get
            {
                return _elmDOM;
            }
            set
            {
                _elmDOM = value;
            }
        }
        #region Component Hierarchy Manipulation

        /// <summary>
        /// Return the Root that this Component was created by.
        /// </summary>
        public RootType Root
        {
            get
            {
                return _root;
            }
        }

        /// <summary>
        /// The Component that is the parent of this Component in the Root hierarchy.
        /// </summary>
        public Component Parent
        {
            get
            {
                return _parent;
            }

            set
            {
                _parent = value;
            }
        }

        /// <summary>
        /// The List that contains the child Components of this Component.
        /// </summary>
        internal List<Component> Children
        {
            get
            {
                return _children;
            }
            set
            {
                _children = value;
            }
        }

        /// <summary>
        /// Resets the focus index variable
        /// </summary>
        internal virtual void ResetFocusedIndex()
        {
        }

        /// <summary>
        /// Focuses on the component before the currently selected one.
        /// </summary>
        /// <returns>
        /// true if there is a previous component and it is focused, false otherwise.
        /// </returns>
        internal virtual bool FocusPrevious(HtmlEvent evt)
        {
            return false;
        }

        /// <summary>
        /// Focuses on the component after the currently selected one.
        /// </summary>
        /// <returns>
        /// true if there is a next component and it is focused, false otherwise.
        /// </returns>
        internal virtual bool FocusNext(HtmlEvent evt)
        {
            return false;
        }

        /// <summary>
        /// Sets internal focus index to the first item.
        /// This item is not necessarily a valid item, so FocusNext must be run at the Menu level after this method.
        /// </summary>
        internal virtual void FocusOnFirstItem(HtmlEvent evt)
        {
        }

        /// <summary>
        /// Sets internal focus index to the last item.
        /// This item is not necessarily a valid item, so FocusPrevious must be run at the Menu level after this method.
        /// </summary>
        internal virtual void FocusOnLastItem(HtmlEvent evt)
        {
        }

        /// <summary>
        /// Finds a menu item with the given id and gives it focus.
        /// </summary>
        /// <param name="menuItemId">
        /// The unique menu item identifier string.
        /// </param>
        /// <returns>
        /// true if the menu item is found, false otherwise.
        /// </returns>
        internal virtual bool FocusOnItemById(string menuItemId)
        {
            return false;
        }

        /// <summary>
        /// Gets the child Component of this Component that has the passed in id.
        /// </summary>
        /// <param name="id">The id of the child that is to be returned.</param>
        /// <returns>A child Component with the passed in id.  Returns null if there is no such child Component.</returns>
        /// <seealso cref="GetChild"/>
        internal Component GetChildInternal(string id)
        {
            foreach (Component c in _children)
            {
                if (c.Id == id)
                    return c;
            }
            return null;
        }

        /// <summary>
        /// Gets the child Component of this Component that has the passed in id.
        /// </summary>
        /// <param name="id">The id of the child that is to be returned.</param>
        /// <returns>A child Component with the passed in id.  Returns null if there is no such child Component.</returns>
        public Component GetChild(string id)
        {
            return GetChildInternal(id);
        }

        /// <summary>
        /// Returns a child Component by Title.
        /// </summary>
        /// <param name="title">The Title of the child Component that is sought.</param>
        /// <returns>A Component with the passed in title.  Returns null if no such child exists.</returns>
        /// <seealso cref="Title"/>
        public Component GetChildByTitle(string title)
        {
            foreach (Component c in _children)
            {
                if (c.Title == title)
                    return c;
            }
            return null;
        }

        /// <summary>
        /// Adds a child Component to this Component.  The Component will be added at the end of the list of children.
        /// </summary>
        /// <param name="child">The Component that should be added as a child to this Component.</param>
        /// <seealso cref="AddChildAtIndex"/>
        public virtual void AddChild(Component child)
        {
            // If the Root is in the process of initializing then we 
            // do not validate the children.
            // This is for performance reasons.
            // TODO(josefl): visit this
            // AddChildInternal(child, !Root.Initializing);
            AddChildInternal(child, true);
        }

        /// <summary>
        /// <see cref="AddChild"/>
        /// </summary>
        /// <param name="child"></param>
        /// <param name="validateChild"></param>
        internal void AddChildInternal(Component child, bool validateChild)
        {
            AddChildAtIndexInternal(child, -1, validateChild);
        }

        /// <summary>
        /// Adds a child Component at a specific index within the list of children of this Component.
        /// </summary>
        /// <param name="child">The child Component to be added.</param>
        /// <param name="index">The index in the list of child Components where this Component should be added.</param>
        /// <seealso cref="AddChild"/>
        public virtual void AddChildAtIndex(Component child, int index)
        {
            AddChildAtIndexInternal(child, index, true);
        }

        /// <summary>
        /// <see cref="AddChildAtIndex"/>
        /// <see cref="EnsureCorrectChildType"></see>
        /// </summary>
        /// <param name="child"></param>
        /// <param name="index"></param>
        /// <param name="validateChild">Whether the child type should be validated or not.  The reflective operations like Type.isInstanceOfObject() are slow so for performance reasons this can be turned off during Root initialization.  This parameter controls whether EnsureValidChild() is called or not.</param>
        internal void AddChildAtIndexInternal(Component child, int index, bool validateChild)
        {
            // Make sure that it is legal to add this kind of child
            // to this Component
            if (validateChild)
                EnsureCorrectChildType(child);

            // Make sure that this child doesn't already live somewhere else in the hierarchy
            if (!CUIUtility.IsNullOrUndefined(child.Parent))
            {
                throw new InvalidOperationException("This child cannot be added because it has already been added \n " +
                                       "to another Component in the hierarchy.  \n You must first call child.Parent.RemoveChild(child)");
            }

            if (index == -1)
                _children.Add(child);
            else
                _children.Insert(index, child);

            child.Parent = this;
            OnDirtyingChange();
        }

        /// <summary>
        /// Removes a child Component with the passed in id from this Component.
        /// </summary>
        /// <param name="id">The id of the child Component that should be removed from this Component.</param>
        public virtual void RemoveChild(string id)
        {
            // TODO: We could save an iteration over the list by grouping
            // GetChildInternal below with remove
            Component child = GetChildInternal(id);
            if (CUIUtility.IsNullOrUndefined(child))
            {
                throw new InvalidOperationException("The child with id: " + id +
                                    " is not as child of this Component");
            }

            _children.Remove(child);

            child.Parent = null;
            OnDirtyingChange();
        }

        /// <summary>
        /// Remove all the children from this Component
        /// </summary>
        public virtual void RemoveChildren()
        {
            // TODO: We could save an iteration over the list by grouping
            // Clear below with the iteration
            foreach (Component child in _children)
            {
                if (child != null)
                {
                    child.Parent = null;
                }
            }
            _children.Clear();
            OnDirtyingChange();
        }

        // Override this to enforce child type restirctions
        /// <summary>
        /// This is called by AddChild and AddChildAtIndex to ensure that the child is of a legal type for this component type.  This should be overriden in each subclass of Component to ensure that the child is of the correct type.  This method should throw an exception if the child is not of the correct type.
        /// </summary>
        /// <param name="child">The child whose type should be checked to make sure that it is a valid child for this Component.</param>
        protected virtual void EnsureCorrectChildType(Component child)
        {
            return;
        }

        /// <summary>
        /// Initializes the Root member of this Component.  Normally this is set via the Component constructor.  This is just used in the Root constructor since the Root reference can not be passed in to it.
        /// </summary>
        /// <param name="root">The Root that this Component's Root member should be set to.</param>
        protected void InitRootMember(Root root)
        {
            if (!CUIUtility.IsNullOrUndefined(_root))
                throw new ArgumentNullException("Root member has already been set for this Component.");
            _root = root;
        }
        #endregion

        /// <summary>
        /// If this Component is visible or not.  Can be overriden by subclasses.
        /// </summary>
        public virtual bool Visible
        {
            get
            {
                return _visible;
            }
            set
            {
                VisibleInternal = value;
            }
        }

        /// <summary>
        /// <see cref="Visible"/>
        /// </summary>
        internal bool VisibleInternal
        {
            get
            {
                return Visible;
            }
            set
            {
                bool oldValue = _visible;
                _visible = value;
                if (oldValue != _visible)
                    OnDirtyingChange();
            }
        }

        /// <summary>
        /// Whether this Component is enabled or not.
        /// </summary>
        public virtual bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                // If the value is not changing and this is not the first time that
                // it is being set, then we don't need to do anything
                if (_enabled == value && _enabledHasBeenSet)
                    return;

                // Components cannot be enabled if their parents are disabled
                if (!CUIUtility.IsNullOrUndefined(Parent) && !Parent.Enabled && value)
                    throw new ArgumentNullException("This Component with id: " + Id + " cannot be Enabled because its parent is Disabled");

                // OnDirtyingChange() is not called here
                // Enabling and disabling controls will happen very frequently
                // in the Root so Components and Controls need to be able to
                // enable and disable themselves without dirtying all their 
                // ancestors and needing a full Root.Refresh() to reflect it.
                _enabled = value;
                _enabledHasBeenSet = true;

                foreach (Component c in _children)
                    c.Enabled = value;

                OnEnabledChanged(value);
            }
        }

        /// <summary>
        /// Called by parent Components when their Enable value changes.  Can be overriden in subclasses to handle Enabling and Disabling of the Component.
        /// </summary>
        /// <param name="enabled"></param>
        protected virtual void OnEnabledChanged(bool enabled)
        {
        }

        /// <summary>
        /// Sets Enabled recursively on all this component and all the components below it
        /// This is used when a subtree needs to be completely disabled but some of the components
        /// have already been set to Enabled=false so they will not call it on their children 
        /// because the value is not changing.  This function will force the whole subtree
        /// to become disabled regardless of the Enabled state of the Components in it.
        /// </summary>
        /// <param name="enabled">the value of Enabled that should be set on the components of this subtree</param>
        internal virtual void SetEnabledRecursively(bool enabled)
        {
            bool changed = _enabled != enabled;
            _enabled = enabled;

            // Set Enabled for its children if there are any
            if (!CUIUtility.IsNullOrUndefined(_children))
            {
                foreach (Component c in _children)
                    c.SetEnabledRecursively(enabled);
            }

            // If the Enabled value was changed for this component then we let it know
            if (changed)
                OnEnabledChanged(enabled);
        }

        /// <summary>
        /// The Title of this Component.
        /// </summary>
        public string Title
        {
            get
            {
                return TitleInternal;
            }
            set
            {
                TitleInternal = value;
                OnDirtyingChange();
            }
        }

        internal string TitleInternal
        {
            get
            {
                return CUIUtility.SafeString(_title);
            }
            set
            {
                _title = value;
            }
        }

        /// <summary>
        /// The Description of this Component.
        /// </summary>
        public string Description
        {
            get
            {
                return CUIUtility.SafeString(_description);
            }
            set
            {
                _description = value;
                OnDirtyingChange();
            }
        }

        /// <summary>
        /// Whether this Component is dirty or not.  If a Component is dirty, it means that its state its in memory state may not be properly represented in its DOMElementInternal.
        /// </summary>
        internal bool Dirty
        {
            get
            {
                return _dirty;
            }
            set
            {
                _dirty = value;

            }
        }

        #region Position & Size
        protected int _componentWidth = -1;
        protected DateTime _lastWidthUpdate = DateTime.Now;
        /// <summary>
        /// The width of this component
        /// </summary>
        internal virtual int ComponentWidth
        {
            get
            {
                return _componentWidth;
            }
        }

        protected int _componentHeight = -1;
        protected DateTime _lastHeightUpdate = DateTime.Now;
        /// <summary>
        /// The height of this component
        /// </summary>
        internal virtual int ComponentHeight
        {
            get
            {
                return _componentHeight;
            }
        }

        protected int _componentTopPosition = -1;
        protected DateTime _lastTopUpdate = DateTime.Now;
        /// <summary>
        /// The position of the top pixel in this component
        /// </summary>
        internal virtual int ComponentTopPosition
        {
            get
            {
                return _componentTopPosition;
            }
        }

        protected int _componentLeftPosition = -1;
        protected DateTime _lastLeftUpdate = DateTime.Now;
        /// <summary>
        /// The position of the left pixel in this component
        /// </summary>
        internal virtual int ComponentLeftPosition
        {
            get
            {
                return _componentLeftPosition;
            }
        }

        protected bool ValueIsDirty(DateTime lastUpdate)
        {
#if !CUI_NORIBBON
            if (Root is SPRibbon)
            {
                SPRibbon rib = (SPRibbon)Root;
                return lastUpdate < rib.LastScaleTime;
            }
#endif
            return false;
        }
        #endregion

        /// <summary>
        /// Dirty or Undirty all Components that are underneath this Component in the Root hierarchy.
        /// </summary>
        /// <param name="dirty">The value that all descendant's dirty properties should be set to.</param>
        internal void SetDirtyRecursively(bool dirty)
        {
            Dirty = dirty;
            if (CUIUtility.IsNullOrUndefined(_children))
                return;

            // Set Dirty for its children
            foreach (Component c in _children)
                c.SetDirtyRecursively(dirty);
        }

        /// <summary>
        /// Should be called on a component when a change has
        /// been made to its in memory structure that invalidates its current
        /// DOM structure/color/layout/style/etc.  By default, calling this also
        /// calls onDirtyingChange on all of this components ancestors.
        /// </summary>
        internal virtual void OnDirtyingChange()
        {
            // If this Component is already dirty, then we do not need to call this
            if (Dirty || _ignoreDirtyingEvents)
                return;

            Dirty = true;
            if (!CUIUtility.IsNullOrUndefined(_parent))
                _parent.OnDirtyingChange();
        }

        /// <summary>
        /// Used for internal operations where we have complete understanding of the code
        /// and we know that we should not cause any dirtying in the hierarchy.
        /// </summary>
        bool _ignoreDirtyingEvents = false;
        internal bool IgnoreDirtyingEvents
        {
            get
            {
                return _ignoreDirtyingEvents;
            }
            set
            {
                _ignoreDirtyingEvents = value;
            }
        }

        /// <summary>
        /// Should be overriden in child Components.
        /// The job of RefreshInternal is to synchronize the in-memory component
        /// hierarchy with the DOM.  In other words, refresh()'s job is to 
        /// cause the DOM to reflect the in-memory hierarchy.
        /// </summary>
        internal virtual void RefreshInternal()
        {
            Dirty = false;
        }

        /// <summary>
        /// This is called by a parent component who is in the process of 
        /// refreshing itself and wants to make sure that all of its 
        /// children are also refreshed.
        /// </summary>
        internal virtual void EnsureRefreshed()
        {
            if (Dirty)
                RefreshInternal();
        }

        /// <summary>
        /// Attach this Component to an already existing DOM Element.
        /// The default behavior is to attach to Document.GetElementById(this.Id)
        /// </summary>
        internal virtual void AttachInternal(bool recursive)
        {
            AttachDOMElements();
            AttachEvents();
            _dirty = false;

            if (recursive)
            {
                if (!CUIUtility.IsNullOrUndefined(_children))
                {
                    foreach (Component c in _children)
                        c.AttachInternal(recursive);
                }
            }
        }

        internal virtual void AttachDOMElements()
        {
            HtmlElement elm = Browser.Document.GetById(Id);
            if (!CUIUtility.IsNullOrUndefined(elm))
            {
                ElementInternal = elm;
            }
            else
            {
                throw new ArgumentNullException("Attempting to attach to Id: " + Id +
                                       " but this id is not present in the DOM");
            }
        }

        internal virtual void AttachEvents()
        {
        }

        /// <summary>
        /// This can be overriden in components where the outer DOM element
        /// of the component is not a div tag and/or the outer structure of
        /// the component is more complex than just one div tag.  For example,
        /// <table/><tbody/><tr/> etc.
        /// </summary>
        internal virtual void EnsureDOMElement()
        {
            // Make sure that the DOM element for this Component is created
            if (_elmDOM == null)
            {
                _elmDOM = GetElementFromTagName(DOMElementTagName);
                _elmDOM.ClassName = CssClass;
                // REVIEW(josefl): do we need these ids only for debug?
                _elmDOM.Id = Id;
            }
        }

        private HtmlElement GetElementFromTagName(string tagName)
        {
            switch (tagName)
            {
                case "li":
                    return new ListItem();
                case "ul":
                    return new UnorderedList();
                case "div":
                    return new Div();
                case "span":
                    return new Span();
                case "table":
                    return new Table();
            }

            return null;
        }

        /// <summary>
        /// The DOMElement type of this Component (div, span...etc).
        /// </summary>
        /// <seealso cref="EnsureDOMElement"/>
        protected virtual string DOMElementTagName
        {
            get
            {
                return "span";
            }
        }

        /// <summary>
        /// The css class name that the outer element of this Component will use.  Can be overriden in subclasses.
        /// </summary>
        protected virtual string CssClass
        {
            get
            {
                return "";
            }
        }

        /// <summary>
        /// Whether this Component is a decendant of the selected Layout.
        /// </summary>
        public virtual bool VisibleInDOM
        {
            get
            {
                if (CUIUtility.IsNullOrUndefined(Parent))
                    return false;

                return Parent.VisibleInDOM;
            }
        }

        internal virtual bool SetFocusOnFirstControl()
        {
            if (!Visible)
                return false;

            foreach (Component c in Children)
            {
                if (c.SetFocusOnFirstControl())
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Ensure that the DOMElement of this Component has been created and that it has been emptied of all child DOMElements.  Used in conjunction with RefreshInternal().
        /// </summary>
        /// <seealso cref="RefreshInternal"/>
        protected virtual void EnsureDOMElementAndEmpty()
        {
            if (CUIUtility.IsNullOrUndefined(ElementInternal))
            {
                // Do these things if the Layout is being refreshed for the first time
                EnsureDOMElement();
            }
            else
            {
                // Do these things if this tab is being rerendered
                ElementInternal = Utility.RemoveChildNodes(ElementInternal);
            }
        }

        /// <summary>
        /// Helper function to append the DOMElements of child Components to the passed in DOMElementInternal.  Used in conjunction with RefreshInternal().
        /// </summary>
        /// <seealso cref="RefreshInternal"/>
        /// <param name="elm">The DOMElement that the child Component's(the children of this Component) DOMElements should be appended to.</param>
        protected virtual void AppendChildrenToElement(HtmlElement elm)
        {
            // The document fragment trick is known to have twice as much
            // throughput as appending directly to the DOM tree
            List<HtmlElement> elts = new List<HtmlElement>();
            foreach (Component child in _children)
            {
                // This is very important for performance
                // We should always try to append an empty child DOM Element
                // first and then call EnsureRefreshed() where the child fills it
                child.EnsureDOMElement();
                child.EnsureRefreshed();
                elts.Add(child.ElementInternal);
            }

            // Now append those elements to the parent
            foreach (HtmlElement elt in elts)
                elm.AppendChild(elt);
        }


        /// <summary>
        /// virtual method for cloning.  this will throw if this subclass of Component has not implemented it.
        /// </summary>
        /// <param name="deep"></param>
        /// <returns></returns>
        internal virtual Component Clone(bool deep)
        {
            throw new InvalidOperationException("This Component type does not support cloning.");
        }

        /// <summary>
        /// When Command events take place in Controls, they call RaiseCommandEvent() on the ControlComponent whose DOMElement had the Command issued in it.  This method then calls PropagateCommandEvent() which bubbles the Command all the way up the Component hierarchy until it gets to the top level Root object where it is dispatched to the appropriate listeners.
        /// </summary>
        /// <param name="commandId">The id of the Command that is to be raised.  For example: "paste".</param>
        /// <param name="type">The CommandType of the Command that is to be raised.</param>
        /// <param name="properties">A Dictionary of additional parameters that should be part of this Command.</param>
        /// <seealso cref="PropagateCommandEvent"/>
        public void RaiseCommandEvent(string commandId,
                                      CommandType type,
                                      Dictionary<string, string> properties)
        {
            CommandEventArgs command = Root.CreateCommandEventArgs(commandId, type, this, properties);
            PropagateCommandEvent(command);
        }

        /// <summary>
        /// Sends a Command that was raised in a decendant Component up to this Component's parent Component.  Also calls OnPreBubbleCommand() and OnPostBubbleCommand() on this Component before and after (respectively) the Command event is propagated to the parent.
        /// </summary>
        /// <param name="command">The Command that is to be propagated up the Component hierarchy.</param>
        /// <seealso cref="RaiseCommandEvent"/>
        /// <seealso cref="OnPreBubbleCommand"/>
        /// <seealso cref="OnPostBubbleCommand"/>
        internal void PropagateCommandEvent(CommandEventArgs command)
        {
            // Let this component have a chance to do something before the command is bubbled up
            // Also let it have a chance to cancel the command event from being bubbled.
            if (OnPreBubbleCommand(command) && !CUIUtility.IsNullOrUndefined(_parent))
                _parent.PropagateCommandEvent(command);

            // Do things after the command has been bubbled
            OnPostBubbleCommand(command);
        }

        /// <summary>
        /// Can be overriden in subclassed.  Called before a Command is propagated to this Component's parent Component.  The bubbling of the Command can be stopped by return false.
        /// </summary>
        /// <param name="command">The Command that is being bubbled.</param>
        /// <returns>true to allow the upward bubbling of the Command to continue and false to stop the Command from bubbling any farther.</returns>
        /// <seealso cref="RaiseCommandEvent"/>
        /// <seealso cref="PropagateCommandEvent"/>
        /// <seealso cref="OnPostBubbleCommand"/>
        internal virtual bool OnPreBubbleCommand(CommandEventArgs command)
        {
            return true;
        }

        /// <summary>
        /// Can be overriden in subclasses.  Called after a Command is propagated to this Component's parent Component.
        /// </summary>
        /// <param name="command">The Command that has been bubbled.</param>
        /// <seealso cref="RaiseCommandEvent"/>
        /// <seealso cref="PropagateCommandEvent"/>
        /// <seealso cref="OnPreBubbleCommand"/>
        internal virtual void OnPostBubbleCommand(CommandEventArgs command)
        {
        }

        /// <summary>
        /// Get the text that is used to match a string with a menu item.  This could be used for a DropDown that is typed in to find the matching MenuItem.
        /// </summary>
        public virtual string GetTextValue()
        {
            return null;
        }

        /// <summary>
        /// UNDER DEVELOPMENT AND MAY CHANGE
        /// </summary>
        public virtual void ReceiveFocus()
        {
        }

        // TODO: revisit, should this be public to allow more extensibility?
        public virtual void OnMenuClosed()
        {
            if (CUIUtility.IsNullOrUndefined(_children))
                return;

            foreach (Component c in _children)
                c.OnMenuClosed();
        }

        DelayedInitHandler _delayedInitHandler = null;
        object _delayedInitData = null;
        object _delayedInitOptions = null;
        internal void SetDelayedInitData(DelayedInitHandler handler, object data, object options)
        {
            _delayedInitHandler = handler;
            _delayedInitData = data;
            _delayedInitOptions = options;
        }

        bool _delayedInitInProgress = false;
        protected void DoDelayedInit()
        {
            if (_delayedInitInProgress)
                return;
            if (CUIUtility.IsNullOrUndefined(_delayedInitHandler))
                throw new ArgumentNullException("No delayedinit handler present in this component: " + Id);

            _delayedInitInProgress = true;
            _delayedInitHandler(this, _delayedInitData, _delayedInitOptions);
        }

        internal virtual void OnDelayedInitFinished(bool success)
        {
            if (success)
            {
                // Clear the delayed init variables since we are done with them.         
                _delayedInitHandler = null;
                _delayedInitData = null;
                _delayedInitOptions = null;
                OnDirtyingChange();
            }
            _delayedInitInProgress = false;
        }

        /// <summary>
        /// If this Component is inited yet.  If Inited is false, then it means that 
        /// this Component will get fully initilized through a delayed init callback.
        /// </summary>
        public bool NeedsDelayIniting
        {
            get
            {
                return !CUIUtility.IsNullOrUndefined(_delayedInitHandler);
            }
        }

        /// <summary>
        /// Updates this Component from the application context.
        /// </summary>
        internal virtual void PollForStateAndUpdateInternal()
        {
            foreach (Component c in _children)
                c.PollForStateAndUpdateInternal();
        }

        DateTime _lastPollTime;
        internal DateTime LastPollTime
        {
            get
            {
                // Initialize the date to the oldest date
                if (CUIUtility.IsNullOrUndefined(_lastPollTime))
                {
                    _lastPollTime = DateTime.Now;
                    _lastPollTime.Subtract(_lastPollTime);
                }
                return _lastPollTime;
            }
            set
            {
                _lastPollTime = value;
            }
        }

        internal bool RootPolledSinceLastPoll
        {
            get
            {
                return LastPollTime < Root.LastPollTime;
            }
        }

        /// <summary>
        /// Poll if PollForStateAndUpdate() was called on the root since this component
        /// Polled for state.
        /// </summary>
        internal void PollIfRootPolledSinceLastPoll()
        {
            // Tabs and Menus only poll for command state when they need to.
            if (RootPolledSinceLastPoll)
                PollForStateAndUpdateInternal();
        }

        /// <summary>
        /// Called to clean up anything that this component needs to
        /// Usually releasing event handlers and cleaning up any circular references.
        /// </summary>
        public virtual void Dispose()
        {
            if (!CUIUtility.IsNullOrUndefined(_children))
            {
                foreach (Component c in _children)
                    c.Dispose();
                _children = null;
            }
            _parent = null;
            _root = null;
            _delayedInitData = null;
            _delayedInitHandler = null;
            _delayedInitOptions = null;
            _elmDOM = null;
        }
    }
}
