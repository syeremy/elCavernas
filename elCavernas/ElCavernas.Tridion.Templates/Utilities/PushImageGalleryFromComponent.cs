using System.Collections.Generic;
using System.Linq;
using ElCavernas.Tridion.Templates.Common.Base;
using Tridion.ContentManager;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.ContentManagement.Fields;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;
using System;

namespace ElCavernas.Tridion.Templates.Utilities
{
    [TcmTemplateTitle("Push Banner Image From Component")]
    public class PushBannerImageFromComponent : TemplateBase
    {
        private readonly string ImageGalleryField = "imagegallery";
        public override void Transform(Engine engine, Package package)
        {
            this.Initialize(engine, package);

            if (!IsPageTemplate)
            {
                //Add linked components from metadata
                Logger.Info("This Template must be used only in a Page, not Component");
                throw new Exception("This Template must be used only in a Page, not Component");
            }

            Component component;
            var imageTcmId = GetBannerImageTcmId();
            if (imageTcmId == null)
            {
                Logger.Info("No ImageGallery in Context!");
                return;
            }

            m_Package.Remove(m_Package.GetByName("ImageGalleryContext"));
            m_Package.PushItem("ImageGalleryContext", m_Package.CreateStringItem(ContentType.Text, imageTcmId));

 
        }


        private string GetBannerImageTcmId()
        {
            var page = GetPage();
            string imageTcmId = null;
            foreach (var cp in page.ComponentPresentations)
            {
                //ImageGallery
                var fields = new ItemFields(cp.Component.Content, cp.Component.Schema);

                var images = fields.OfType<MultimediaLinkField>();

                if (images == null || images.Count() == 0)
                {
                    //Logger.Info("No Multimedia Link Fields in the context.");
                    continue;
                }

                var multimediaLink = images.FirstOrDefault(img => img.Name.ToLowerInvariant().Equals("imagegallery"));

                if (multimediaLink != null && multimediaLink.Value != null && multimediaLink.Value.ComponentType == ComponentType.Multimedia)
                {
                    imageTcmId = multimediaLink.Value.Id;
                    break;
                }
            }

            return imageTcmId;
        }
    }
}
