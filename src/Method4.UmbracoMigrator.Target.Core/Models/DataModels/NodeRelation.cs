using Method4.UmbracoMigrator.Target.Core.CustomDbTables.NPoco;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Mapping;

namespace Method4.UmbracoMigrator.Target.Core.Models.DataModels
{
    public class NodeRelation
    {
        public NodeRelation(string oldId, string newId, string oldKeyAsString, string newKeyAsString, Guid oldKeyAsGuid, Guid newKeyAsGuid)
        {
            OldId = oldId;
            NewId = newId;
            OldKeyAsString = oldKeyAsString;
            NewKeyAsString = newKeyAsString;
            OldKeyAsGuid = oldKeyAsGuid;
            NewKeyAsGuid = newKeyAsGuid;
        }

        public string OldId { get; }

        public string NewId { get; }

        public string OldKeyAsString { get; }

        public string NewKeyAsString { get; }

        public Guid OldKeyAsGuid { get; }

        public Guid NewKeyAsGuid { get; }
    }

    public class NodeRelationMapper : IMapDefinition
    {
        private readonly ILogger<NodeRelationMapper> _logger;

        public NodeRelationMapper(ILogger<NodeRelationMapper> logger)
        {
            _logger = logger;
        }

        public void DefineMaps(IUmbracoMapper mapper)
        {
            mapper.Define<NodeRelationLookupPoco, NodeRelation>((source, context) =>
                new NodeRelation(
                    source.OldId,
                    source.NewId,
                    source.OldKey,
                    source.NewKey,
                    ParseKeyAsGuid(source.OldKey),
                    ParseKeyAsGuid(source.NewKey))
            );
        }

        private Guid ParseKeyAsGuid(string key)
        {
            var parsed = Guid.TryParse(key, out var guidKey);
            if (parsed == false)
            {
                _logger.LogError("Failed to parse '{key}' as a GUID'", key);
                throw new Exception($"Failed to parse '{key}', as a GUID");
            }
            return guidKey;
        }
    }
}