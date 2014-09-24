using System.Web;
using ElCavernas.Tridion.Templates.Common.Base;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;

namespace ElCavernas.Tridion.Templates.Utilities
{
    [TcmTemplateTitle("Output Decoder")]
    internal class OutputDecoder : TemplateBase
    {
        public override void Transform(Engine engine, Package package)
        {
            Initialize(engine, package);
            Item outputItem = package.GetByName("Output");

            //if not  Output in package, return
            if (outputItem == null)
                return;

            string outputText = outputItem.GetAsString();
            outputText = HttpUtility.HtmlDecode(outputText);

            string findCharactersCsv = package.GetValue("FindCharactersCSV");
            string replaceCharactersCsv = package.GetValue("ReplaceCharactersCSV");

            if (!string.IsNullOrWhiteSpace(findCharactersCsv))
            {
                string[] findStrings = findCharactersCsv.Split(',');
                string[] replaceStrings = replaceCharactersCsv.Split(',');
                Logger.Debug("FindStrings[] length: " + findStrings.Length);
                Logger.Debug("ReplaceStrings[] length: " + replaceStrings.Length);

                for (int i = 0; i < findStrings.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(findStrings[i]))
                    {
                        Logger.Debug(string.Format("Looking to replace: {0} with {1}", findStrings[i], replaceStrings[i]));
                        outputText = outputText.Replace(findStrings[i].Trim(), replaceStrings[i].Trim());
                    }
                }
            }

            //outputText = FixTcdlAttributeEcoding(outputText);

            outputItem.SetAsString(outputText);
            package.Remove(outputItem);
            package.PushItem("Output", outputItem);
            Logger.Debug("Output successfully decoded");
        }

//        private string FixTcdlAttributeEcoding(string outputText)
//        {
//            var doc = new HtmlDocument();
//            doc.LoadHtml(outputText);
//            if (doc.ParseErrors != null && doc.ParseErrors.Any())
//            {
//                Logger.Info("Could not parse html");
//                return outputText;
//            }
//            if (doc.DocumentNode == null) return outputText;
//
//            foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//tcdl:Link"))
//            {
//                string attributeValue = node.GetAttributeValue("linkAttributes", "");
//                if (!string.IsNullOrWhiteSpace(attributeValue))
//                {
//                    attributeValue = HttpUtility.HtmlEncode(attributeValue);
//                }
//                node.SetAttributeValue("linkAttributes", attributeValue);
//            }
//            return doc.ToString();
//        }
    }
}