using System;
using System.Collections.Generic;
using Microsoft.LiveLabs.Html;

namespace Ribbon
{
    /// <summary>
    /// The type of a Section
    /// </summary>
    internal enum SectionType
    {
        Divider = 1,
        OneRow = 2,
        TwoRow = 3,
        ThreeRow = 4
    }

    internal enum SectionAlignment
    {
        Top = 1,
        Middle = 2
    }

    internal class Section : RibbonComponent
    {
        SectionType _type;
        SectionAlignment _alignment;

        /// <summary>
        /// Section Constructor
        /// </summary>
        /// <param name="ribbon">the Ribbon that this Section was created by and is a part of</param>
        /// <param name="id">Component id of the Secton</param>
        /// <param name="type">type of the section</param>
        internal Section(SPRibbon ribbon, string id, SectionType type, SectionAlignment alignment)
            : base(ribbon, id, "", "")
        {
            _type = type;
            _alignment = alignment;

            switch (type)
            {
                case SectionType.ThreeRow:
                    AddChildInternal(new Row(ribbon, id + "-0"), false);
                    AddChildInternal(new Row(ribbon, id + "-1"), false);
                    AddChildInternal(new Row(ribbon, id + "-2"), false);
                    break;
                case SectionType.TwoRow:
                    AddChildInternal(new Row(ribbon, id + "-0"), false);
                    AddChildInternal(new Row(ribbon, id + "-1"), false);
                    break;
                case SectionType.OneRow:
                    AddChildInternal(new Row(ribbon, id + "-0"), false);
                    break;
                case SectionType.Divider:
                    break;
                default:
                    throw new ArgumentException("Invalid SectionType");
            }
        }

        internal override void RefreshInternal()
        {
            EnsureDOMElementAndEmpty();
            AppendChildrenToElement(ElementInternal);

            EnsureDOMElement();
            if (Type != SectionType.Divider)
            {
                List<Component> children = Children;
                AppendRow(children, 1);
                if (Type == SectionType.TwoRow || Type == SectionType.ThreeRow)
                    AppendRow(children, 2);
                if (Type == SectionType.ThreeRow)
                    AppendRow(children, 3);
            }
            Dirty = false;
        }

        internal override void AttachDOMElements()
        {
            // Sections should be named in the following way:  PARENTLAYOUTID-SECTIONNUMBER
            // For example: Ribbon.Edit.Clipboard-Large-3 (section number 3 of the clipboard Large Layout)
            ElementInternal = Browser.Document.GetById(Parent.Id + "-" + Parent.Children.IndexOf(this).ToString());
        }

        private void AppendRow(List<Component> children, int rowNumber)
        {
            Row row = (Row)children[rowNumber - 1];
            row.EnsureDOMElement();

            if (this.Type == SectionType.TwoRow)
                row.ElementInternal.ClassName = "ms-cui-row-tworow";
            ElementInternal.AppendChild(row.ElementInternal);
            row.EnsureRefreshed();
        }

        protected override string CssClass
        {
            get
            {
                if (_alignment == SectionAlignment.Middle)
                    return "ms-cui-section-alignmiddle";
                else
                    return "ms-cui-section";
            }
        }

        /// <summary>
        /// The type of Section that this is.
        /// </summary>
        public SectionType Type
        {
            get 
            { 
                return _type; 
            }
        }

        public SectionAlignment Alignment
        {
            get 
            { 
                return _alignment; 
            }
        }

        public override void RemoveChild(string id)
        {
            throw new InvalidOperationException("Cannot directly add and remove children from Section Components");
        }

        public override void AddChildAtIndex(Component child, int index)
        {
            throw new InvalidOperationException("Cannot directly add and remove children from Section Components");
        }

        /// <summary>
        /// Gets a Row Component that is a child of this Section.
        /// </summary>
        /// <param name="rowNum">valid values 1, 2 or 3</param>
        /// <returns>the Row Component that was requested</returns>
        public Row GetRow(int rowNum)
        {
            switch (_type)
            {
                case SectionType.ThreeRow:
                    if (rowNum < 1 || rowNum > 3)
                        throw new ArgumentOutOfRangeException("This Section type only has Row numbers 1, 2 and 3.");
                    break;
                case SectionType.TwoRow:
                    if (rowNum < 1 || rowNum > 2)
                        throw new ArgumentOutOfRangeException("This Section type only has Row numbers 1 and 2");
                    break;
                case SectionType.OneRow:
                    if (rowNum != 1)
                        throw new ArgumentOutOfRangeException("This Section type only has Row number 1.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException("This Section type does not have any rows");
            }
            return (Row)Children[rowNum - 1];
        }

        internal override void EnsureDOMElement()
        {
            if (!CUIUtility.IsNullOrUndefined(ElementInternal))
                return;

            if (Type == SectionType.Divider)
            {
                Span elmDivider = new Span();
                elmDivider.ClassName = "ms-cui-section-divider";
                ElementInternal = elmDivider;
                return;
            }

            base.EnsureDOMElement();
        }

        protected override void EnsureCorrectChildType(Component child)
        {
            int count = Children.Count;
            if ((this.Type == SectionType.OneRow && count > 0) ||
                (this.Type == SectionType.TwoRow && count > 1) ||
                (this.Type == SectionType.ThreeRow && count > 2))
            {
                throw new InvalidOperationException("No more children can be added to a Section of this type.");
            }

            if (this.Type == SectionType.Divider)
                throw new InvalidOperationException("Cannot add child components to Divider Section types.");

            if (!typeof(Row).IsInstanceOfType(child))
                throw new InvalidCastException("Only children of type Row can be added to Section Components.");
        }

        internal override Component Clone(bool deep)
        {
            Section section = Ribbon.CreateSection("clonedSection-" + Ribbon.GetUniqueNumber(), Type, _alignment);
            if (!deep)
                return section;

            int i = 0;
            foreach (Row row in Children)
            {
                foreach (object obj in row.Children)
                {
                    Component clonedComp = null;
                    if (obj is ControlComponent)
                    {
                        ControlComponent comp = (ControlComponent)obj;
                        clonedComp = comp.Clone(deep);
                    }
                    else if (obj is Strip)
                    {
                        clonedComp = ((Strip)obj).Clone(deep);
                    }
                    section.GetRow(i + 1).AddChild(clonedComp);
                }
                i++;
            }
            return section;
        }

        protected override string DOMElementTagName
        {
            get 
            { 
                return "span"; 
            }
        }

    }
}
