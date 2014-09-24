using System;
using System.Web;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Tridion.ContentManager;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;
using System.Text.RegularExpressions;
using Tridion.ContentManager.ContentManagement.Fields;
using System.Xml.Linq;
using Tridion.ContentManager.CommunicationManagement;
using System.Text;
using System.Xml.Serialization;
using Tridion.ContentManager.Templating.Configuration;
using System.Xml;
using ElCavernas.Tridion.Templates.Common.Base;

namespace ElCavernas.Tridion.Templates.Utilities
{
    [TcmTemplateTitle("Render Component Presentation By Template Name")]
    public class RenderCPbyTemplateName : TemplateBase
    {
        //private const string ComponentTemplateNamePlaceHolderPattern = @"GetTemplateTcmIdByName\['(.*?)']";
        private const string ComponentTemplateNamePlaceHolderPattern = @"@RenderComponentPresentationByTemplateName\('(.*?)'.*'(.*?)'\)@";
        private const string RenderComponentPresenationPlaceHolder = "@RenderComponentPresentationByTemplateName('{0}','{1}')@";
        private const string XmlNs = "http://www.tridion.com/ContentManager/5.0";

        private XmlElement _templates = null;

        public override void Transform(Engine engine, Package package)
        {
            base.Initialize(engine, package);

            Item outputItem = package.GetByName(Package.OutputName);
            string output = outputItem.GetAsString();

            var templateNames = GetTemplateNames(output);

            var filter = new ComponentTemplatesFilter(engine.GetSession());
            _templates = GetPublication().GetListComponentTemplates(filter);

            if (_templates == null)
                Logger.Info("there were not Component Templates.");

            foreach (var item in templateNames)
            {
                var templateTcmid = GetTemplateTcmId(item.Item2);

                if (string.IsNullOrEmpty(templateTcmid))
                    return;

                var cpContent = m_Engine.RenderComponentPresentation(new TcmUri(item.Item1), new TcmUri(templateTcmid));
                output = output.Replace(item.Item3, cpContent);
                Logger.Info(string.Format("replaced {0} by {1}", item.Item2, templateTcmid));
            }

            //Save the changes in output.
            outputItem.SetAsString(output);
        }


        //<tcm:Item ID=""tcm:1089-4687-32"" Title=""Carousel Banner"" Icon=""T32L0P0S4"" Type=""32"" OwningPublicationID=""1083"" IsNew=""false"" />
        private string GetTemplateTcmId(string templateName)
        {
            var xns = new XmlNamespaceManager(new NameTable());
            xns.AddNamespace("tcm", XmlNs);

            var xpathSearch = string.Format("//tcm:Item[@Title='{0}']", templateName);
            var template = _templates.SelectSingleNode(xpathSearch, xns);
            return template != null ? template.Attributes["ID"].Value : null;
        }

        private IEnumerable<Tuple<string, string, string>> GetTemplateNames(string output)
        {
            var matches = Regex.Matches(output, ComponentTemplateNamePlaceHolderPattern);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 2)
                {
                    var textToReplace = match.Groups[0].Value;
                    var componentTcmId = match.Groups[1].Value;
                    var tempalteName = match.Groups[2].Value;
                    yield return new Tuple<string, string, string>(componentTcmId, tempalteName, textToReplace);
                }
            }
        }
    }
}
