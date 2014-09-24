using System.Text.RegularExpressions;
using ElCavernas.Tridion.Templates.Common.Base;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;

namespace ElCavernas.Tridion.Templates.Utilities
{
    [TcmTemplateTitle("Remove HTML whitespace from output")]
    internal class RemoveHtmlWhitespace : TemplateBase
    {
        public override void Transform(Engine engine, Package package)
        {
            Initialize(engine, package);

            //get output item from the package
            const string outputName = Package.OutputName;



            Item outputItem = package.GetByName(outputName);

            //if not  Output in package, return
            if (outputItem == null)
                return;

            // if the output is text based, lets clean it up
            if ((outputItem.Type == PackageItemType.String))
            {
                Logger.Debug("Output exists, cleaning empty lines");
                string uglyOutput = outputItem.GetAsString();

                //Gets the converted output string
                string fixedOutput = Regex.Replace(uglyOutput, @"^\s*$\n", string.Empty, RegexOptions.Multiline);
                //Remove the old output string, and put the new one in place
                Logger.Debug("Output cleaned, updating item in package");
                package.Remove(outputItem);
                outputItem.SetAsString(fixedOutput);
                package.PushItem(outputName, outputItem);
            }
        }
    }
}