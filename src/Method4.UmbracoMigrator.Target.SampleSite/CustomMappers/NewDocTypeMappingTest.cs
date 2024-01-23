using Method4.UmbracoMigrator.Target.Core.Mappers;
using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Umbraco.Cms.Core.Models;

public class NewDocTypeMapperTest : IDocTypeMapping
{
    public string DocTypeAlias => "newDocTypeTest";

    public bool CanIMap(MigrationContent MigrationNode)
    {
        return MigrationNode.ContentType == "oldDocTypeTest";
    }

    public IContent MapNode(MigrationContent oldNode, IContent newNode, bool overwiteExisitingValues)
    {
        // Map a property with a different name
        var oldText = oldNode.Properties.First(x => x.Alias == "oldText");
        newNode.SetValue("newText", oldText.GetDefaultValue);

        return newNode;
    }
}