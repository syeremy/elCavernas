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

namespace ElCavernas.Tridion.Templates
{
    [TcmTemplateTitle("Navigation Sitemap Generation")]
    public class SitemapGeneration : TemplateBase
    {
        private const string AddToTopNavigationKeyword = "Add To Top Navigation";
        private const string RemoveFromBreadcrumbKeyword = "Remove From BreadCrumb";

        private const string NavigationTop = "Top";
        private const string NavigationBreadscrum = "Breadcrumb";

        private const string StructureGroupValidationPattern = @"^\d{2}-(.*)";
        private const string DefaultPage = "index.aspx";

        private string _navigationType;
        private ItemType[] _itemTypeFilter;
        private List<string> pageTemplatesToIgnore;
        private Publication publication;
        private bool OnlyFirstLevel = false;

        public override void Transform(Engine engine, Package package)
        {
            base.Initialize(engine, package);
            _navigationType = GetNavigationType();

            if (string.IsNullOrEmpty(_navigationType))
            {
                Logger.Info("There was not navigation information in page metadata.");
                return;
            }

            publication  = GetPublication();
            var structureGroupRoot = publication.RootStructureGroup;

            pageTemplatesToIgnore = new List<string>();
            pageTemplatesToIgnore.Add("Parallax Page");

            XElement elements;
            switch (_navigationType)
            {
                case NavigationTop:
                    elements = BuildMainNavigation(structureGroupRoot);
                    break;
                case NavigationBreadscrum:
                    elements = BuildBreadcrumbNavigation(structureGroupRoot);
                    break;
                default:
                    elements = new XElement("");
                    break;
            }


            var sitemap = new XElement("siteMap", elements);
            package.Remove(package.GetByName(Package.OutputName));
            package.PushItem(Package.OutputName, package.CreateStringItem(ContentType.Xml, sitemap.ToString()));
        }

        private XElement BuildBreadcrumbNavigation(RepositoryLocalObject structureGroupRoot)
        {
            _itemTypeFilter = new[] { ItemType.StructureGroup, ItemType.Page };
            return CreateNavigationStructure(structureGroupRoot);
        }

        private XElement BuildMainNavigation(RepositoryLocalObject structureGroupRoot)
        {
            OnlyFirstLevel = true;
            _itemTypeFilter = new[] { ItemType.StructureGroup, ItemType.Page, };
            return CreateNavigationStructure(structureGroupRoot);
        }


        #region Internal Methods

        

        private XElement CreateNavigationStructure(RepositoryLocalObject current)
        {
            //Validate Structure group name, only for the first level
            if (current is StructureGroup && !ValidateStructureGroupName(current as StructureGroup))
                return null;

            //Ignore some pages based in their page template.
            if (current is Page && IgnorePageTemplate(current as Page))
                return null;



            var item = BuildStrucutreItem(current);

            if (item == null)
                return null;

            if (current is Page)
                return item;
            
            //validation to only list the children of root (home) strucutu group.
            if (OnlyFirstLevel && !current.Title.Equals(publication.RootStructureGroup.Title))
                return item;

            

            var children = GetStructureGroupItems(current as StructureGroup);
            if (children != null && !children.Any())
                return item;

            foreach (var child in children)
            {
                var childItem = CreateNavigationStructure(child as RepositoryLocalObject);

                if (childItem != null)
                    item.Add(childItem);
            }
            return item;
        }


        private XElement BuildStrucutreItem(RepositoryLocalObject item)
        {
            string friendlyUrl = null;
            string realUrl = null;
            // We dont't list Index.aspx pages because, they are represented as structureGroup.
            if (item is Page && GetPageName((item as Page).PublishLocationUrl).ToLower().Equals(DefaultPage))
            {
                return null;
            }

            Page indexPage = null;
            if (item is StructureGroup)
            {
                indexPage = GetIndexPageFromStrucuteGroup(item as StructureGroup);

                if (indexPage == null)
                    return null;

                if (!ValidateNavigationKeywordsMetadata(indexPage))
                    return null;

                //if the index page doesn't have a seo url in metadata, we take the url of the structure group.
                friendlyUrl = indexPage.PublishLocationUrl.ToLower().Replace("index.aspx", ""); //GetFriendlyPageUrl(indexPage);
                realUrl = indexPage.PublishLocationUrl;

                //External Page Validation
                var externalUrl = GetExternalUrl(indexPage);
                if (!string.IsNullOrEmpty(externalUrl))
                {
                    realUrl = friendlyUrl = externalUrl;
                }
            }
            else if (item is Page)
            {
                friendlyUrl = GetFriendlyPageUrl(item);


                //publication.PublicationUrl -> /en | /fr
                if (!string.IsNullOrEmpty(friendlyUrl) && !friendlyUrl.ToLower().StartsWith(publication.PublicationUrl.ToLower()))
                {
                    friendlyUrl = string.Format("{0}/{1}", publication.PublicationUrl, friendlyUrl.TrimStart('/'));
                }
                realUrl = (item as Page).PublishLocationUrl;

                if (!ValidateNavigationKeywordsMetadata(item as Page))
                    return null;


            }



            //if there is not friendly url, we use the real url.
            friendlyUrl = !string.IsNullOrEmpty(friendlyUrl) ? friendlyUrl : realUrl;

            var title = GetTitle(item);//item.Title.ToLower() != "root" ? item.Title.Substring(item.Title.LastIndexOf('-') + 1) : item.Title;

            //
            if (title.Equals(publication.RootStructureGroup.Title))
            {
                title = GetRootName();
                friendlyUrl = "/";
            }

            var xmlItem = new XElement("siteMapNode",
                    new XAttribute("title", title),
                    new XAttribute("description", title),
                    new XAttribute("url", friendlyUrl),
                    new XAttribute("rawUrl", realUrl),
                    new XAttribute("type", item.GetType().Name)
                );


            return xmlItem;
        }

        //Language | Root Name
        private string GetRootName()
        {
            // PublicationUri | Languge | Publication Url | Root Name
            var pubInfo = GetPublicationInfo();
            return string.IsNullOrEmpty(pubInfo.Item4) ? "Home" : pubInfo.Item4;
        }

        private string GetExternalUrl(Page page)
        {
            if (page.Metadata == null || page.MetadataSchema == null)
                return null;

            var meta = new ItemFields(page.Metadata, page.MetadataSchema);
            //Get the Embedded navigation Metadata
            var url = meta.Text("ExternalUrl");


            return url;
        }

        private bool ValidateNavigationKeywordsMetadata(Page page)
        {
            //If page is Index.aspx and it is in the root SG.
            if (page.OrganizationalItem.Title.Equals(publication.RootStructureGroup.Title) && GetPageName(page.PublishLocationUrl).ToLower().Equals(DefaultPage))
                return true;

            bool result = false;
            //
            if (page.Metadata == null || page.MetadataSchema == null)
            { 
                Logger.Info(string.Format("Page {0} removed from navigation, this page does not have metadata.", page.Title));
                return result;
            }
            
            var meta = new ItemFields(page.Metadata, page.MetadataSchema);
            var values = meta.Texts("NavigationKeywords");

            switch (_navigationType)
            {
                case NavigationTop:
                    result = values.Contains(AddToTopNavigationKeyword);
                    break;
                case NavigationBreadscrum:
                    result = !values.Contains(RemoveFromBreadcrumbKeyword);
                    break;
            }

            //For External PAges
            if (!result)
            {
                var externalUrl = meta.Text("ExternalUrl");
                result = !string.IsNullOrEmpty(externalUrl);
            }

            if (!result)
                Logger.Info(string.Format("Page {0} excluded from {1} Navigation", page.PublishLocationUrl, _navigationType));

            return result;
        }


        private string GetPageName(string namePage)
        {
            return namePage.Substring(namePage.LastIndexOf('/') + 1);
        }


        private bool IgnorePageTemplate(Page current)
        {
            return pageTemplatesToIgnore.Contains(current.PageTemplate.Title);
        }

        private IEnumerable<RepositoryLocalObject> GetStructureGroupItems(StructureGroup structureGroup,
                IEnumerable<ItemType> itemTypeFilter = null)
        {
            var filter = new OrganizationalItemItemsFilter(structureGroup.Session);
            filter.ItemTypes = itemTypeFilter ?? _itemTypeFilter;

            var items = structureGroup.GetItems(filter);
            return items;
        }

        private Page GetIndexPageFromStrucuteGroup(StructureGroup structureGroup)
        {
            string tcmidIndexPage = string.Empty;
            var items = GetStructureGroupItems(structureGroup, new[] { ItemType.Page });

            var indexPage = items.OfType<Page>().FirstOrDefault(item => GetPageName(item.PublishLocationUrl).ToLower().Equals(DefaultPage));

            //tcmidIndexPage = indexPage != null ? indexPage.Id : string.Empty;
            return indexPage;
        }


        private string GetTitle(RepositoryLocalObject item)
        {
            var match = Regex.Match(item.Title, StructureGroupValidationPattern);

            if (match.Groups.Count > 1)//if it has the patter 01-1-xxxx, it will remove the 01-1-
            {
                return match.Groups[1].Value;
            }
            else
            { 
                return item.Title;
            }
        }

        private string GetFriendlyPageUrl(RepositoryLocalObject current)
        {
            //Only for pages
            if (current is StructureGroup)
                return null;


            if (current.Metadata == null || current.MetadataSchema == null)
                return null;

            var meta = new ItemFields(current.Metadata, current.MetadataSchema);
            //Get the Embedded navigation Metadata
            var friendlyUrl = meta.Text("SEOUrl");


            return !string.IsNullOrEmpty(friendlyUrl) ? friendlyUrl : null ;
        }


        private string GetNavigationType()
        {
            var page = GetPage();
            if (page.Metadata == null || page.MetadataSchema == null)
                return null;

            var metaFields = new ItemFields(page.Metadata, page.MetadataSchema);


            var navigationType = metaFields.Text("navigationType");
            return navigationType;
        }



        //dd-[text] -> 03-Projects
        private bool ValidateStructureGroupName(StructureGroup current)
        {
            var title = current.Title;

            //validation for first level Strucutre Groups 
            if (current.OwningRepository != null && current.OwningRepository.Title.Equals(publication.RootStructureGroup.Title))
            {
                if (!Regex.IsMatch(title, StructureGroupValidationPattern))
                    return false;
            }

            return true;
        }


        #endregion
    }
}
