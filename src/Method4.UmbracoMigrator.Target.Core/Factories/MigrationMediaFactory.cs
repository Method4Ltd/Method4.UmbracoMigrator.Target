using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;

namespace Method4.UmbracoMigrator.Target.Core.Factories
{
    public class MigrationMediaFactory : MigrationFactoryBase, IMigrationMediaFactory
    {
        private readonly ILogger<MigrationMediaFactory> _logger;

        public MigrationMediaFactory(ILogger<MigrationMediaFactory> logger)
        {
            _logger = logger;
        }

        public List<MigrationMedia> ConvertFromXml(IEnumerable<XElement> mediaElements)
        {
            var migrationContent = new List<MigrationMedia>();
            foreach (var mediaElement in mediaElements)
            {
                migrationContent.Add(ConvertFromXml(mediaElement));
            }
            return migrationContent;
        }

        public MigrationMedia ConvertFromXml(XElement mediaElement)
        {
            Guid key;
            string id;
            string name;
            int level;
            Guid parentKey;
            bool trashed;
            string contentType;
            DateTime createDate;
            var nodeNames = new List<MigrationNodeName>();
            int sortOrder;
            List<MigrationProperty> properties;

            /// Get node info (no try parse as any fail here we want to just throw)
            key = Guid.Parse(mediaElement.Attribute("Key")!.Value);
            id = mediaElement.Attribute("Id")!.Value;
            name = mediaElement.Attribute("Name")!.Value;
            level = int.Parse(mediaElement.Attribute("Level")!.Value);

            var infoElement = mediaElement.Element("Info");
            parentKey = Guid.Parse(infoElement!.Element("Parent")!.Attribute("Key")!.Value);
            trashed = bool.Parse(infoElement.Element("Trashed")!.Value);
            contentType = infoElement.Element("ContentType")!.Value;
            createDate = DateTime.Parse(infoElement.Element("CreateDate")!.Value);
            sortOrder = int.Parse(infoElement.Element("SortOrder")!.Value);

            var nodeNameElement = infoElement.Element("NodeName");
            nodeNames.Add(new MigrationNodeName("default", nodeNameElement!.Attribute("Default")!.Value));

            /// Get properties
            var xmlProperties = mediaElement.Element("Properties")!.Elements();
            properties = GetProperties(xmlProperties);

            return new MigrationMedia(key, id, name, level, parentKey,
                trashed, contentType, createDate, nodeNames, sortOrder, properties);
        }
    }
}