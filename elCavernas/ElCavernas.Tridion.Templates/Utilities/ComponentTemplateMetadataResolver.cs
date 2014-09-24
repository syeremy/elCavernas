using System;
using System.Collections.Generic;

using System;
using ElCavernas.Tridion.Templates.Common.Base;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;
using System.Text.RegularExpressions;
using Tridion.ContentManager.ContentManagement.Fields;

namespace ElCavernas.Tridion.Templates.Utilities
{
    [TcmTemplateTitle("Component Template Metadata Resolver")]
    class ComponentTemplateMetadataResolver : TemplateBase
    {
        public override void Transform(Engine engine, Package package)
        {
            Initialize(engine, package);
            var template = engine.PublishingContext.ResolvedItem.Template;

            if (template.Metadata == null || template.MetadataSchema == null)
                return;

            var metaFields = new ItemFields(template.Metadata, template.MetadataSchema);
            //PushFieldsToPackage(metaFields, "CT");

            foreach (ItemField field in metaFields)
            {
                //One Value
                if (field.Definition.MaxOccurs == 1 && field.StringValue() != null)
                {
                    var name = string.Format("ComponentTemplate.Metadata.{0}", field.Name);
                    m_Package.Remove(m_Package.GetByName(name));
                    m_Package.PushItem(name, m_Package.CreateStringItem(ContentType.Text, field.StringValue()));
                }
                else // -- Multi Value
                {
                    //TODO: No supported.
                }
            }
        }
    }
}
