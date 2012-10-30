using System;
using System.Collections.Generic;
using Microsoft.LiveLabs.JavaScript;

namespace Ribbon
{
    internal sealed class DeclarativeTemplateBuildContext
    {
        public SPRibbon Ribbon;
        public Dictionary<string, List<Control>> Controls;
        public Dictionary<string, string> Parameters;
    }

    /// <summary>
    /// A template from a declarative xml/json representation.
    /// </summary>
    internal class DeclarativeTemplate : Template
    {
        object _data;
        public DeclarativeTemplate(object data)
        {
            _data = data;
        }

        public override Group CreateGroup(SPRibbon ribbon,
                                          string id,
                                          GroupProperties properties,
                                          string title,
                                          string description,
                                          string command,
                                          Dictionary<string, List<Control>> controls,
                                          Dictionary<string, string> pars)
        {
            DeclarativeTemplateBuildContext bc = new DeclarativeTemplateBuildContext();
            bc.Ribbon = ribbon;
            bc.Controls = controls;
            bc.Parameters = pars;

            Group group = ribbon.CreateGroup(id, properties, title, description, command);

            // Loop through the Layouts for this group and create them.
            JSObject[] children = DataNodeWrapper.GetNodeChildren(_data);
            for (int i = 0; i < children.Length; i++)
            {
                Layout layout = CreateLayoutFromData(children[i], group, bc);
                if (!CUIUtility.IsNullOrUndefined(layout))
                    group.AddChild(layout);
            }
            return group;
        }

        private Layout CreateLayoutFromData(object data,
                                            Group group,
                                            DeclarativeTemplateBuildContext bc)
        {
            string title = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.TITLE);
            if (title == DataNodeWrapper.POPUP)
            {
                string layoutTitle = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.LAYOUTTITLE);

                group.PopupLayoutTitle = layoutTitle;
                return null;
            }

            Layout layout = bc.Ribbon.CreateLayout(group.Id + "-" + title, title);

            layout.SetDelayedInitData(new DelayedInitHandler(this.DelayInitLayout), data, bc);
            // Comment out the SetDelayedInitData() line and uncomment this one to disable
            // on-demand construction of layouts.
            // FillLayout(data, layout, bc);
            return layout;
        }

        private Component DelayInitLayout(Component component,
                                          object data,
                                          object bc)
        {
            Layout layout = (Layout)component;
            DeclarativeTemplateBuildContext buildContext = (DeclarativeTemplateBuildContext)bc;
            FillLayout(data, layout, buildContext);
            layout.OnDelayedInitFinished(true);
            return layout;
        }

        internal void FillLayout(object data,
                                 Layout layout,
                                 DeclarativeTemplateBuildContext bc)
        {
            JSObject[] children = DataNodeWrapper.GetNodeChildren(data);
            int sectionCounter = 0;
            for (int i = 0; i < children.Length; i++)
            {
                string name = DataNodeWrapper.GetNodeName(children[i]);
                if (name == DataNodeWrapper.SECTION)
                {
                    Section section = CreateSectionFromData(children[i], bc, layout, sectionCounter++);
                    layout.AddChild(section);
                }
                else
                {
                    // This must be an <OverflowSection>
                    sectionCounter = HandleOverflow(children[i], bc, layout, sectionCounter);
                }
            }
        }

        private Section CreateSectionFromData(object data, DeclarativeTemplateBuildContext bc, Layout layout, int sectionNumber)
        {
            SectionType type;
            string strType = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.TYPE);
            string strAlignment = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.ALIGNMENT);
            switch (strType)
            {
                case DataNodeWrapper.ONEROW:
                    type = SectionType.OneRow;
                    break;
                case DataNodeWrapper.TWOROW:
                    type = SectionType.TwoRow;
                    break;
                case DataNodeWrapper.THREEROW:
                    type = SectionType.ThreeRow;
                    break;
                case DataNodeWrapper.DIVIDER:
                    type = SectionType.Divider;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Invalid Section attribute \"Type\" found in XML: " + strType);

            }

            SectionAlignment alignment = SectionAlignment.Top;
            if (strAlignment == "Middle")
                alignment = SectionAlignment.Middle;

            Section section = bc.Ribbon.CreateSection(layout.Id + "-" + sectionNumber, type, alignment);

            if (type != SectionType.Divider)
            {
                HandleRow(section.GetRow(1), 
                          DataNodeWrapper.GetNodeChildren(data)[0], 
                          bc);

                if (section.Type == SectionType.TwoRow ||
                    section.Type == SectionType.ThreeRow)
                {
                    HandleRow(section.GetRow(2), 
                              DataNodeWrapper.GetNodeChildren(data)[1], 
                              bc);
                }

                if (section.Type == SectionType.ThreeRow)
                {
                    HandleRow(section.GetRow(3), 
                              DataNodeWrapper.GetNodeChildren(data)[2], 
                              bc);
                }
            }

            return section;
        }

        private void HandleRow(Row row, JSObject data, DeclarativeTemplateBuildContext bc)
        {
            JSObject[] children = DataNodeWrapper.GetNodeChildren(data);
            for (int i = 0; i < children.Length; i++)
            {
                string name = DataNodeWrapper.GetNodeName(children[i]);
                Component comp = null;

                if (name == DataNodeWrapper.CONTROL)
                {
                    comp = CreateControlComponentFromData(children[i], bc);
                }
                else if (name == DataNodeWrapper.OVERFLOWAREA)
                {
                    HandleOverflow(children[i], bc, row, i);
                }
                else
                {
                    comp = CreateStripFromData(children[i], bc, row, i);
                }

                if (!CUIUtility.IsNullOrUndefined(comp))
                    row.AddChild(comp);
            }
        }

        private Strip CreateStripFromData(object data,
                                          DeclarativeTemplateBuildContext bc,
                                          Component parent,
                                          int rowComponentNumber)
        {
            JSObject[] children = DataNodeWrapper.GetNodeChildren(data);
            Strip strip = bc.Ribbon.CreateStrip(parent.Id + "-" + rowComponentNumber);

            for (int i = 0; i < children.Length; i++)
            {
                string name = DataNodeWrapper.GetNodeName(children[i]);
                if (name == DataNodeWrapper.CONTROL)
                {
                    ControlComponent comp = CreateControlComponentFromData(children[i], bc);
                    if (!CUIUtility.IsNullOrUndefined(comp))
                        strip.AddChild(comp);
                }
                else
                {
                    HandleOverflow(children[i], bc, strip, i);
                }
            }

            // If there are no children in the strip then there is no reason to add it
            // If we ever support dynamically adding and removing components out of the ribbon
            // then this will need to be revisitited.
            if (strip.Children.Count == 0)
                return null;

            return strip;
        }

        private ControlComponent CreateControlComponentFromData(object data, DeclarativeTemplateBuildContext bc)
        {
            string alias =  DataNodeWrapper.GetAttribute(data, DataNodeWrapper.TEMPLATEALIAS);
            string displayMode = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.DISPLAYMODE);

            ControlComponent comp = null;
            List<Control> control = bc.Controls.ContainsKey(alias) ? bc.Controls[alias] : null;

            // If there is more than one control that is using the same TemplateAlias and the template
            // slot is a ControlRef, then the slot remains empty so that the problem can be detected and resolved.
            if (!CUIUtility.IsNullOrUndefined(control) && control.Count > 1)
                comp = control[0].CreateComponentForDisplayMode(displayMode);
            return comp;
        }

        private int HandleOverflow(object data, DeclarativeTemplateBuildContext bc, Component parent, int sectionCounter)
        {
            string alias = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.TEMPLATEALIAS);

            string name = DataNodeWrapper.GetNodeName(data);

            List<Control> rec = bc.Controls.ContainsKey(alias) ? bc.Controls[alias] : null;

            // No Controls need to be added to this overflowarea so we return without doing anything
            if (CUIUtility.IsNullOrUndefined(rec))
                return sectionCounter;

            bool dividerBefore = false;
            bool dividerAfter = false;

            SectionType sectionType = SectionType.OneRow;
            if (name == DataNodeWrapper.OVERFLOWSECTION)
            {
                dividerBefore = Utility.IsTrue(DataNodeWrapper.GetAttribute(data, DataNodeWrapper.DIVIDERBEFORE));
                dividerAfter = Utility.IsTrue(DataNodeWrapper.GetAttribute(data, DataNodeWrapper.DIVIDERAFTER));
                if (dividerBefore)
                {
                    Section section = bc.Ribbon.CreateSection(parent.Id + "-" + sectionCounter++, SectionType.Divider, SectionAlignment.Top);
                    parent.AddChild(section);
                }

                string secType = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.TYPE);

                switch (secType)
                {
                    case DataNodeWrapper.ONEROW:
                        sectionType = SectionType.OneRow;
                        break;
                    case DataNodeWrapper.TWOROW:
                        sectionType = SectionType.TwoRow;
                        break;
                    case DataNodeWrapper.THREEROW:
                        sectionType = SectionType.ThreeRow;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Invalid Section attribute \"Type\" found in XML: " + secType);
                }
            }

            string displayMode = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.DISPLAYMODE);

            if (rec.Count > 1)
            {
                Section currentSection = null;
                for (int i = 0; i < rec.Count; i++)
                {
                    Control control = rec[i];
                    if (name == DataNodeWrapper.OVERFLOWSECTION)
                    {
                        if (sectionType == SectionType.OneRow)
                        {
                            if (CUIUtility.IsNullOrUndefined(currentSection))
                            {
                                currentSection = bc.Ribbon.CreateSection(parent.Id + "-" + sectionCounter++, SectionType.OneRow, SectionAlignment.Top);
                                parent.AddChild(currentSection);
                            }
                            currentSection.GetRow(1).AddChild(control.CreateComponentForDisplayMode(displayMode));
                        }
                        else if (sectionType == SectionType.ThreeRow)
                        {
                            // ThreeRow Sections
                            if (CUIUtility.IsNullOrUndefined(currentSection))
                            {
                                currentSection = bc.Ribbon.CreateSection(parent.Id + "-" + sectionCounter++, SectionType.ThreeRow, SectionAlignment.Top);
                                parent.AddChild(currentSection);
                            }
                            currentSection.GetRow((i % 3) + 1).AddChild(control.CreateComponentForDisplayMode(displayMode));

                            // If we have just filled the third row of a section with a ControlComponent, then 
                            // we need to signal that we need to start a new section the next time through the loop
                            if (i % 3 == 2)
                                currentSection = null;
                        }
                        else
                        {
                            // Two Row Sections
                            if (CUIUtility.IsNullOrUndefined(currentSection))
                            {
                                currentSection = bc.Ribbon.CreateSection(parent.Id + "-" + sectionCounter++, SectionType.TwoRow, SectionAlignment.Top);
                                parent.AddChild(currentSection);
                            }
                            currentSection.GetRow((i % 2) + 1).AddChild(control.CreateComponentForDisplayMode(displayMode));

                            // If we have just filled the third row of a section with a ControlComponent, then 
                            // we need to signal that we need to start a new section the next time through the loop
                            if (i % 2 == 1)
                                currentSection = null;
                        }
                    }
                    else
                    {
                        // <OverflowArea> tag
                        parent.AddChild(control.CreateComponentForDisplayMode(displayMode));
                    }
                }
            }
            else
            {
                Control control = rec[0];

                if (name == DataNodeWrapper.OVERFLOWSECTION)
                {
                    Section section;
                    if (sectionType == SectionType.OneRow)
                    {
                        section = bc.Ribbon.CreateSection(parent.Id + "-" + sectionCounter++, SectionType.OneRow, SectionAlignment.Top);
                        section.GetRow(1).AddChild(control.CreateComponentForDisplayMode(displayMode));
                    }
                    else if (sectionType == SectionType.ThreeRow)
                    {
                        // Three Row Section
                        section = bc.Ribbon.CreateSection(parent.Id + "-" + sectionCounter++, SectionType.ThreeRow, SectionAlignment.Top);
                        section.GetRow(1).AddChild(control.CreateComponentForDisplayMode(displayMode));
                    }
                    else
                    {
                        // Two Row Section
                        section = bc.Ribbon.CreateSection(parent.Id + "-" + sectionCounter++, SectionType.TwoRow, SectionAlignment.Top);
                        section.GetRow(1).AddChild(control.CreateComponentForDisplayMode(displayMode));
                    }
                    parent.AddChild(section);
                }
                else
                {
                    // <OverflowArea> tag
                    parent.AddChild(control.CreateComponentForDisplayMode(displayMode));
                }
            }

            if (dividerAfter)
            {
                Section section = bc.Ribbon.CreateSection(parent.Id + "-" + sectionCounter++, SectionType.Divider, SectionAlignment.Top);
                parent.AddChild(section);
            }

            return sectionCounter;
        }
    }

}
