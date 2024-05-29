using System.Collections.Generic;
using Enterspeed.Migrator.Models;
using Umbraco.Cms.Core.Models;

namespace Umbraco.Migrator.Content
{
    public interface IContentBuilder
    {
        void BuildContentPages(List<PageData> pageEntityTypes, IContent parent = null); 
        IContent FindUmbracoStartParentNode(int startParentId);
    }
}