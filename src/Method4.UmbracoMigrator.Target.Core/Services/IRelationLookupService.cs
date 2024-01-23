using Method4.UmbracoMigrator.Target.Core.Models.DataModels;

namespace Method4.UmbracoMigrator.Target.Core.Services
{
    public interface IRelationLookupService
    {
        NodeRelation? GetRelationByOldId(string oldId);
        NodeRelation? GetRelationByNewId(string newId);

        NodeRelation? GetRelationByOldKey(Guid oldKey);
        NodeRelation? GetRelationByOldKey(string oldKey);
        NodeRelation? GetRelationByNewKey(Guid newKey);
        NodeRelation? GetRelationByNewKey(string newKey);

        void StoreNewRelation(string newId, string oldId, Guid newKey, Guid oldKey);
        void StoreNewRelation(string newId, string oldId, string newKey, string oldKey);

        int CountRelations();
        void DeleteAllRelations();
    }
}