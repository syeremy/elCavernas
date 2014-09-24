using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml.Linq;

namespace ElCavernas.Tridion.Website.system.userControls
{
    public partial class Breadcrumb : System.Web.UI.UserControl
    {
        public static ILog logger = LogManager.GetLogger(typeof(Breadcrumb));

        #region Properties
        [Bindable(true),
        Category("Appearance"),
        DefaultValue(""),
        Description("Sitemap File"),]
        public string RelativeSourceFilePath { get; set; }

        [Bindable(true),
        Category("Appearance"),
        DefaultValue(""),
        Description("Show the current Page link of Breadcrumb"),]
        public bool ShowCurrentPage { get; set; }
        #endregion



        //private const string _separator = "<span><img src=\"{0}/files/images/img-list-chevron.png\" /></span>";
        private const string _itemHtml = "<li><a href=\"{0}\">{1}</a></li>";
        // Fields
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                if (!this.Page.IsPostBack)
                {
                    var sb = new StringBuilder();
                    string url = Utils.Util.GetUrl(this.Page);
                    string path = base.Server.MapPath(RelativeSourceFilePath);

                    if (!File.Exists(path))
                    {
                        //LOG
                        return;
                    }

                    XElement element = XElement.Load(path);
                    var existItemInSitemap = element.Descendants("siteMapNode").Any<XElement>(a => a.Attribute("rawUrl").Value.Equals(url));

                    if (!existItemInSitemap) return;

                    //Gets First Node
                    var xnode = element.Descendants("siteMapNode").FirstOrDefault<XElement>(descendant => descendant.Attribute("url").Value.Equals("/"));
                    if (xnode != null)
                    {
                        sb.AppendLine(string.Format(_itemHtml, this.Page.Request.ApplicationPath, xnode.Attribute("title").Value));
                        this.BuildBreadcrumb(xnode, url, sb);
                        this.breadcrumb.Text = sb.ToString();
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }


        // Methods
        private void BuildBreadcrumb(XElement xNode, string url, StringBuilder sb)
        {
            XElement element;
            bool lastLeaf = false;
            element = xNode.Elements("siteMapNode").FirstOrDefault(item => item.Descendants("siteMapNode")
                    .Any(a => a.Attribute("rawUrl").Value.Equals(url)));

            if (element == null)// Validate if the direct child is the last leaf.
            {
                element = xNode.Elements("siteMapNode").FirstOrDefault(item => item.Attribute("rawUrl").Value.Equals(url));
                lastLeaf = true;
            }

            if (element != null)
            {
                if (lastLeaf)
                {
                    if (ShowCurrentPage)
                    {
                        //sb.AppendFormat(_separator, this.Page.Request.ApplicationPath);
                        //TODO: No class active, ask for it.
                        sb.AppendLine(string.Format(_itemHtml, element.Attribute("url").Value, element.Attribute("title").Value));
                    }
                }
                else
                {
                    //sb.AppendFormat(_separator, this.Page.Request.ApplicationPath);
                    sb.AppendLine(string.Format(_itemHtml, element.Attribute("url").Value, element.Attribute("title").Value));
                }

                this.BuildBreadcrumb(element, url, sb);
            }
        }


        protected override void Render(HtmlTextWriter writer)
        {
            base.Render(writer);
        }

    }
}