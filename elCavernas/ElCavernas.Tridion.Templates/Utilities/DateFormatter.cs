using System;
using System.Web;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using ElCavernas.Tridion.Templates.Common.Base;
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
using ElCavernas.Tridion.Templates.Common.Extensions;
using System.Xml.Serialization;
using Tridion.ContentManager.Templating.Configuration;
using System.Xml;
using System.Globalization;

namespace ElCavernas.Tridion.Templates
{
    [TcmTemplateTitle("Date Formatter")]
    public class DateFormatter : TemplateBase
    {
        private List<string> _dateTimePatterns = null;
        private const string DateTimeFormatEn = @"MMMM d, yyyy";
        private const string DateTimeFormatFr = @"d MMMM yyyy";
  
        public override void Transform(Engine engine, Package package)
        {
            base.Initialize(engine, package);
            InitializeConfiguration();
            Item outputItem = package.GetByName(Package.OutputName);
            string output = outputItem.GetAsString();

            var items = GetDates(output);

            foreach (var item in items)
            {
                var olValue = item.Item1;
                var newVaue = item.Item2;
                output = output.Replace(olValue, newVaue);
                Logger.Info(string.Format("replaced {0} by {1}", olValue, newVaue));
            }

            //Save the changes in output.
            outputItem.SetAsString(output);
        }

        private IEnumerable<Tuple<string, string>> GetDates(string output)
        {
            var pubLanguage = GetPublicationInfo(GetPublication()).Item2;

            foreach (var dateTimePatter in _dateTimePatterns)
            {
                var matches = Regex.Matches(output, dateTimePatter);

                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        var date = match.Groups[0].Value;
                        var newDate = ParseDate(pubLanguage, date);
                        yield return new Tuple<string, string>(date, newDate);
                    }
                }
            }
        }

        private string ParseDate(string pubLanguage, string strDate)
        {
            string dateTimeFormat = null;
            CultureInfo pubCulture = null;
            string result = null;

            switch (pubLanguage.ToLower())
            {
                case "en":
                    // MMMM d, yyyy
                    dateTimeFormat = DateTimeFormatEn;
                    pubCulture = CultureInfo.CreateSpecificCulture("en");
                    break;
                case "fr":
                    // d MMMM, yyyy
                    dateTimeFormat = DateTimeFormatFr;
                    pubCulture = CultureInfo.CreateSpecificCulture("fr");
                    break;
                default:
                    dateTimeFormat = DateTimeFormatEn;
                    pubCulture = CultureInfo.CreateSpecificCulture("en");
                    break;
            }

            result = Convert.ToDateTime(strDate).ToString(dateTimeFormat, pubCulture);
            return result;
        }

        private void InitializeConfiguration()
        {
            // -- 12/14/2013 10:58:19 AM
            // -- 2014-08-13 00:00:00
            _dateTimePatterns = new List<string> 
            { 
                @"[0,1]?\d\/(([0-2]?\d)|([3][01]))\/((199\d)|([2-9]\d{3}))\s[0-2]?[0-9]:[0-5][0-9]:[0-5][0-9]\s?(PM|AM)?",
                @"(199\d|[2-9]\d{3})-([0-2]?\d|[3][01])-([0-3]?\d)\s([0-2]?[0-9]:[0-5][0-9]:[0-5][0-9])\s?"
            };
        }
    }

}
