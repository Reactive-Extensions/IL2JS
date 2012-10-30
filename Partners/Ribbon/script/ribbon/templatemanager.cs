using System.Collections.Generic;
using Microsoft.LiveLabs.JavaScript;

namespace Ribbon
{
    internal class TemplateManager
    {
        Dictionary<string, Template> _templates;

        static TemplateManager _instance;
        public static TemplateManager Instance
        {
            get
            {
                if (CUIUtility.IsNullOrUndefined(_instance))
                    _instance = new TemplateManager();
                return _instance;
            }
        }

        public TemplateManager()
        {
            _templates = new Dictionary<string, Template>();
        }

        public void AddTemplate(Template template, string id)
        {
            _templates[id] = template;
        }

        public void RemoveTemplate(string id)
        {
            _templates[id] = null;
        }

        public Template GetTemplate(string id)
        {
            if (!_templates.ContainsKey(id))
                return null;

            return _templates[id];
        }

        public void LoadTemplates(object data)
        {
            JSObject templatesNode = DataNodeWrapper.GetFirstChildNodeWithName(data, DataNodeWrapper.RIBBONTEMPLATES);
            JSObject[] children = DataNodeWrapper.GetNodeChildren(templatesNode);
            for (int i = 0; i < children.Length; i++)
                LoadGroupTemplate(children[i]);
        }

        private void LoadGroupTemplate(object data)
        {
            string id = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.ID);
            string className = DataNodeWrapper.GetAttribute(data, DataNodeWrapper.CLASSNAME);

            // If the template is already loaded, then we don't load it again
            if (!CUIUtility.IsNullOrUndefined(GetTemplate(id)))
                return;

            if (string.IsNullOrEmpty(className))
                AddTemplate(new DeclarativeTemplate(data), id);
        }
    }
}
