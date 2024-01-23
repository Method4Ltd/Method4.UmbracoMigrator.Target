using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Trees;
using Umbraco.Cms.Web.BackOffice.Trees;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Common.ModelBinders;

namespace Method4.UmbracoMigrator.Target.Core.Controllers.Trees
{
    [Tree("Settings", "migratorTarget",
        TreeGroup = "migration",
        TreeTitle = "migratorTarget",
        SortOrder = 40)]
    [PluginController("Method4UmbracoMigratorTarget")]
    public class MigratorTreeController : TreeController
    {
        public MigratorTreeController(
            ILocalizedTextService localizedTextService,
            UmbracoApiControllerTypeCollection umbracoApiControllerTypeCollection,
            IEventAggregator eventAggregator)
            : base(localizedTextService, umbracoApiControllerTypeCollection, eventAggregator)
        { }

        protected override ActionResult<TreeNode> CreateRootNode(FormCollection queryStrings)
        {
            var result = base.CreateRootNode(queryStrings);

            result.Value.RoutePath = $"{this.SectionAlias}/migratorTarget/dashboard";
            result.Value.Icon = "icon-truck";
            result.Value.HasChildren = false;
            result.Value.MenuUrl = null;

            return result.Value;
        }

        protected override ActionResult<MenuItemCollection> GetMenuForNode(string id, [ModelBinder(typeof(HttpQueryStringModelBinder))] FormCollection queryStrings)
        {
            return null;
        }

        protected override ActionResult<TreeNodeCollection> GetTreeNodes(string id, [ModelBinder(typeof(HttpQueryStringModelBinder))] FormCollection queryStrings)
        {
            return new TreeNodeCollection();
        }
    }
}