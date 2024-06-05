using Method4.UmbracoMigrator.Target.Core.CustomDbTables.NPoco;
using Method4.UmbracoMigrator.Target.Core.Models.DataModels;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Infrastructure.Scoping;

namespace Method4.UmbracoMigrator.Target.Core.Services
{
    public class RelationLookupService : IRelationLookupService
    {
        private readonly IUmbracoMapper _umbracoMapper;
        private readonly IScopeProvider _scopeProvider;
        private readonly ILogger<RelationLookupService> _logger;
        private const string TableName = "MigrationLookups";

        public RelationLookupService(IUmbracoMapper umbracoMapper, IScopeProvider scopeProvider, ILogger<RelationLookupService> logger)
        {
            _umbracoMapper = umbracoMapper;
            _scopeProvider = scopeProvider;
            _logger = logger;
        }

        public NodeRelation? GetRelationByOldId(string oldId)
        {
            return GetRelation("OldId", oldId);
        }

        public NodeRelation? GetRelationByNewId(string newId)
        {
            return GetRelation("NewId", newId);
        }

        public NodeRelation? GetRelationByOldKey(Guid oldKey)
        {
            return GetRelationByOldKey(oldKey.ToString());
        }

        public NodeRelation? GetRelationByOldKey(string oldKey)
        {
            return GetRelation("OldKey", oldKey);
        }

        public NodeRelation? GetRelationByNewKey(Guid newKey)
        {
            return GetRelationByNewKey(newKey.ToString());
        }

        public NodeRelation? GetRelationByNewKey(string newKey)
        {
            return GetRelation("NewKey", newKey);
        }

        public void StoreNewRelation(string newId, string oldId, Guid newKey, Guid oldKey)
        {
            var relationLookup = new NodeRelationLookupPoco()
            {
                NewId = newId,
                OldId = oldId,
                NewKey = newKey.ToString(),
                OldKey = oldKey.ToString()
            };

            try
            {
                using var scope = _scopeProvider.CreateScope();
                scope.Database.Insert<NodeRelationLookupPoco>(relationLookup);
                scope.Complete();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store new migration node relation");
                throw;
            }
        }

        public void StoreNewRelation(string newId, string oldId, string newKey, string oldKey)
        {
            if (Guid.TryParse(newKey, out var parsedNewKey) == false)
            {
                _logger.LogError("Failed to parse '{newKey}'", newKey);
                throw new Exception($"Failed to parse '{newKey}'");
            }

            if (Guid.TryParse(oldKey, out var parsedOldKey) == false)
            {
                _logger.LogError("Failed to parse '{oldKey}'", oldKey);
                throw new Exception($"Failed to parse '{oldKey}'");
            }

            StoreNewRelation(newId, oldId, parsedNewKey, parsedOldKey);
        }

        public int CountRelations()
        {
            using var scope = _scopeProvider.CreateScope();
            var queryResult = scope.Database.Fetch<NodeRelationLookupPoco>($"SELECT * From {TableName}");
            scope.Complete();

            return queryResult.Count;
        }

        public void DeleteAllRelations()
        {
            _logger.LogInformation("Deleting all Key relations");
            try
            {
                using var scope = _scopeProvider.CreateScope();
                var queryResult = scope.Database.Execute($"DELETE FROM {TableName}");
                scope.Complete();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete all relations from the {tableName} table", TableName);
                throw;
            }
        }

        private NodeRelation? GetRelation(string columnName, string value)
        {
            using var scope = _scopeProvider.CreateScope();
            var queryResult = scope.Database.Fetch<NodeRelationLookupPoco>($"SELECT * From {TableName} WHERE {columnName} = @0", value);
            scope.Complete();

            var foundLookup = queryResult.FirstOrDefault(); // There should only ever be 1
            if (foundLookup == null)
            {
                _logger.LogDebug("Lookup Not found for {lookupType}: {lookupValue}", columnName, value);
                return null;
            }

            var relation = _umbracoMapper.Map<NodeRelation>(foundLookup);
            if (relation == null)
            {
                _logger.LogError("Unable to map NodeRelationLookupPoco to NodeRelation for {lookupType}: {value}", columnName, value);
                throw new Exception($"Lookup for {columnName}: '{value}' could not be mapped, result from the Umbraco Mapper was null");
            }

            return relation;
        }
    }
}