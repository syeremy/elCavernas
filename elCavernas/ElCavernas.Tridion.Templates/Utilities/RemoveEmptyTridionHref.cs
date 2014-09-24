
using System;
using ElCavernas.Tridion.Templates.Common.Base;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;
using System.Text.RegularExpressions;

namespace ElCavernas.Tridion.Templates
{
    /// <summary>
    /// </summary>
    [TcmTemplateTitle("Remove Empty Tridion Href")]
    public class RemoveEmptyTridionHref : TemplateBase
    {
        public override void Transform(Engine engine, Package package)
        {
            Initialize(engine, package);
            var outputItem = package.GetByName(Package.OutputName);

            //if not  Output in package, return
            if (outputItem == null) return;

            var output = outputItem.GetAsString();

            var pattern = "tridion:href=\\s*\"\"";
            output = Regex.Replace(output, pattern, string.Empty);

            outputItem.SetAsString(output);
        }


    }
}