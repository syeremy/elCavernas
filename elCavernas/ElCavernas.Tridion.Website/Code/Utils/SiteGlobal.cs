using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace ElCavernas.Tridion.Website.Utils
{
    public static class SiteGlobal
    {
        static public int PublicationId { get; set; }
        static public string PublicationTcmId { get; set; }

        static public int CacheDurationInMinutes { get; set; }

        static SiteGlobal()
        {
            PublicationId = int.Parse(WebConfigurationManager.AppSettings["publicationId"]);
            PublicationTcmId = string.Format("tcm:0-{0}-1", PublicationId);

            CacheDurationInMinutes = int.Parse(WebConfigurationManager.AppSettings["cacheDurationInMinutes"]);
        }
    }
}