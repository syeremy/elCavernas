using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.UI;
using Tridion.ContentDelivery.Meta;
using log4net;
using System.Globalization;
using Tridion.ContentDelivery.DynamicContent.Query;

namespace ElCavernas.Tridion.Website.Utils
{
    public class Util
    {
        public static ILog logger = LogManager.GetLogger(typeof(Util));
        public static CustomMeta GetPageCustomMeta(Page page)
        {
            using (IPageMeta meta = GetPageMeta(page))
            {
                if (meta != null)
                {
                    return meta.CustomMeta;
                }
                return null;
            }
        }

        public static object GetPageCustomMetaValue(Page page, string keyName)
        {
            using (CustomMeta meta = GetPageCustomMeta(page))
            {
                if (meta != null)
                {
                    return meta.GetFirstValue(keyName);
                }
                return null;
            }
        }

        public static IPageMeta GetPageMeta(Page page)
        {
            string url = GetUrl(page);
            int publicatrionId = SiteGlobal.PublicationId;
            using (PageMetaFactory factory = new PageMetaFactory(publicatrionId))
            {
                return factory.GetMetaByUrl(publicatrionId, url);
            }
        }


        public static IPageMeta GetPageMetaFromSEO(HttpRequestBase request, string url)
        {

            PageMetaFactory pageMetaFactory = new PageMetaFactory(SiteGlobal.PublicationId);
            IPageMeta pageMeta = pageMetaFactory.GetMetaByUrl(SiteGlobal.PublicationId, url);

            if (pageMeta == null)
            {
                // Check if it is a Vanity URL
                var publicationCriteria = new PublicationCriteria(SiteGlobal.PublicationId);
                var pageCriteria = new ItemTypeCriteria((int)TridionItemType.Page);
                var vanityUrlCriteria = new CustomMetaValueCriteria(new CustomMetaKeyCriteria("SEOUrl", Criteria.Equal), url);

                // --

                Query query = new Query();
                query.Criteria = CriteriaFactory.And(new Criteria[] { publicationCriteria, pageCriteria, vanityUrlCriteria });
                IEnumerable<string> pages = query.ExecuteQuery();

                //If no result, we try taking the ApplicationPath ['/en'] fro the url
                if (!pages.Any() && url.ToLower().StartsWith(request.ApplicationPath.ToLower()))
                {
                    var urlWithoutAppPath = url.Substring(request.ApplicationPath.Length);
                    var vanityUrlCriteria2 = new CustomMetaValueCriteria(new CustomMetaKeyCriteria("SEOUrl", Criteria.Equal), urlWithoutAppPath);
                    query.Criteria = CriteriaFactory.And(new Criteria[] { publicationCriteria, pageCriteria, vanityUrlCriteria2 });
                    pages = query.ExecuteQuery();

                    //If no result, we try taking the '/' from the begining of the url.
                    if (!pages.Any())
                    {
                        urlWithoutAppPath = urlWithoutAppPath.TrimStart('/');
                        vanityUrlCriteria2 = new CustomMetaValueCriteria(new CustomMetaKeyCriteria("SEOUrl", Criteria.Equal), urlWithoutAppPath);
                        query.Criteria = CriteriaFactory.And(new Criteria[] { publicationCriteria, pageCriteria, vanityUrlCriteria2 });
                        pages = query.ExecuteQuery();
                    }
                }

                if (pages.Any())
                {
                    pageMeta = pageMetaFactory.GetMeta(pages.First());
                }
            }
            return pageMeta;
        }

        public static string GetUrl(Page page)
        {
            return GetUrl(page.Request, page);
        }

        public static string GetUrl(HttpRequest request, Page page)
        {
            string absolutePath = request.Url.AbsolutePath;
            if ((page.RouteData != null) && (page.RouteData.DataTokens["PhysicalUrl"] != null))
            {
                absolutePath = Convert.ToString(page.RouteData.DataTokens["PhysicalUrl"]);
                if (!(string.IsNullOrEmpty(absolutePath) || !absolutePath.StartsWith("~/")))
                {
                    absolutePath = absolutePath.Substring(1, absolutePath.Length - 1);
                }
            }
            return absolutePath;
        }


        //TODO: Replace this code referencing FindControlsByType and taking the first item.
        public static T FindControlByType<T>(Control control) where T : Control
        {
            Control result = null;
            if (!control.HasControls())
                return null;

            result = control.Controls.OfType<T>().FirstOrDefault();

            if (result != null)
                return result as T;

            //foreach (var item in control.Controls)
            for (int i = 0; i < control.Controls.Count; i++)
            {
                result = FindControlByType<T>(control.Controls[i]);

                if (result != null)
                    break;
            }

            return result as T;
        }

        public static IEnumerable<T> FindControlsByType<T>(Control control) where T : Control
        {
            var controls = new List<Control>();
            IEnumerable<Control> result = null;
            if (!control.HasControls())
                return null;

            result = control.Controls.OfType<T>();

            if (result != null && result.Count() > 0)
            { 
                return result.Cast<T>();
            }

            //foreach (var item in control.Controls)
            for (int i = 0; i < control.Controls.Count; i++)
            {
                result = FindControlsByType<T>(control.Controls[i]);

                if (result != null && result.Count() > 0)
                {
                    controls.AddRange(result);
                }
            }

            //return controls != null ? controls.Cast<T>() : new List<T>();
            return controls.Cast<T>();
        }




        public static U GetObjectProperties<T, U>(T source) where U : new()
        {
            PropertyInfo targetProperty;
            object value;

            U newObject = new U();
            var sourceType = typeof(T);
            var targetType = typeof(U);

            
            var propertyId = sourceType.GetProperty("ID");
            logger.DebugFormat("Properties of [{0}] [{1}]", propertyId != null? propertyId.GetValue(source) : "NULL", sourceType.ToString());

            foreach (var property in sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            { 
                try
                {
                    if (!IsPrimitive(property.PropertyType))
                        continue;

                    value = property.GetValue(source);
                    targetProperty = targetType.GetProperty(property.Name);

                    if (targetProperty == null)
                        continue;

                    targetProperty.SetValue(newObject, value, null);
                    if (value != null && ((value is string && !string.IsNullOrEmpty(value.ToString())) || value is int))
                    { 
                        logger.DebugFormat("Name:{0}\t\t\t|| Value: {1}", property.Name, value);
                    }
                }
                catch (Exception ex)
                {
                    //TODO: Handle Error.
                }
            }

            return newObject;
        }

        private static bool IsPrimitive(Type t)
        {
            return new[] { 
            typeof(string), 
            typeof(char),
            typeof(byte),
            typeof(sbyte),
            typeof(ushort),
            typeof(short),
            typeof(uint),
            typeof(int),
            typeof(ulong),
            typeof(long),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(DateTime),
        }.Contains(t);
        }

        public static string GetHeaderMonth(DateTime date)
        {
            CultureInfo pubCulture = null;

            //TODO : Refactor: USe the metadata of the publication!!!
            var appPath = HttpContext.Current.Request.ApplicationPath;
            var pubLanguage = appPath.Trim('/');
            switch (pubLanguage.ToLower())
            {
                case "en":
                    pubCulture = CultureInfo.CreateSpecificCulture("en");
                    break;
                case "fr":
                    pubCulture = CultureInfo.CreateSpecificCulture("fr");
                    break;
                default:
                    pubCulture = CultureInfo.CreateSpecificCulture("en");
                    break;
            }


            return date.ToString("MMMM yyyy", pubCulture);
        }

        public static string GetHeaderMonth(string date)
        {
            return String.Format("{0:MMMM yyyy}", date);
        }

    }
}