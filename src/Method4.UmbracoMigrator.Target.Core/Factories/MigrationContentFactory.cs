using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using Umbraco.Extensions;

namespace Method4.UmbracoMigrator.Target.Core.Factories
{
    public class MigrationContentFactory : MigrationFactoryBase, IMigrationContentFactory
    {
        private readonly ILogger<MigrationContentFactory> _logger;

        public MigrationContentFactory(ILogger<MigrationContentFactory> logger)
        {
            _logger = logger;
        }

        public List<MigrationContent> ConvertFromXml(IEnumerable<XElement> contentElements)
        {
            var migrationContent = new List<MigrationContent>();
            foreach (var contentElement in contentElements)
            {
                migrationContent.Add(ConvertFromXml(contentElement));
            }
            return migrationContent;
        }

        public MigrationContent ConvertFromXml(XElement contentElement)
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
            var publishedStatus = new List<MigrationPublishStatus>();
            var scheduluedStatus = new List<MigrationScheduleStatus>();
            MigrationTemplateInfo? template = null;
            List<MigrationProperty> properties;
            MigrationPublicAccess? publicAccess;
            var availableCultures = new List<string>();

            // Get node info (no try parse as any fail here we want to just throw)
            key = Guid.Parse(contentElement.Attribute("Key")!.Value);
            id = contentElement.Attribute("Id")!.Value;
            name = contentElement.Attribute("Name")!.Value;
            level = int.Parse(contentElement.Attribute("Level")!.Value);

            var infoElement = contentElement.Element("Info");
            parentKey = Guid.Parse(infoElement!.Element("Parent")!.Attribute("Key")!.Value);
            trashed = bool.Parse(infoElement.Element("Trashed")!.Value);
            contentType = infoElement.Element("ContentType")!.Value;
            createDate = DateTime.Parse(infoElement.Element("CreateDate")!.Value);
            sortOrder = int.Parse(infoElement.Element("SortOrder")!.Value);

            // Get the parent key from the Trashed attribute, if the node is trashed
            var trashedParentKey = infoElement!.Element("Trashed")!.Attribute("Parent")?.Value;
            if (trashed && trashedParentKey.IsNullOrWhiteSpace() == false)
            {
                parentKey = Guid.Parse(trashedParentKey!);
            }

            // Get available cultures
            var availableCulturesString = contentElement.Attribute("AvailableCultures")?.Value;
            if (availableCulturesString.IsNullOrWhiteSpace() == false)
            {
                availableCultures = availableCulturesString!.Split(',').ToList();
            }

            // Get Default Node Name
            var nodeNameElement = infoElement.Element("NodeName");
            nodeNames.Add(
                new MigrationNodeName("default", nodeNameElement!.Attribute("Default")!.Value)
                );

            // Get Variant Node Names
            var variantNodeNameElements = nodeNameElement!.Elements("Name");
            foreach (var variantNameElement in variantNodeNameElements)
            {
                var culture = variantNameElement.Attribute("Culture")!.Value;
                nodeNames.Add(
                    new MigrationNodeName(culture, variantNameElement.Value)
                    );
            }

            // Get published status
            var publishedElement = infoElement.Element("Published");
            publishedStatus.Add(
                new MigrationPublishStatus("default", bool.Parse(publishedElement!.Attribute("Default")!.Value))
                );

            // Get Variant published statuses
            var variantPublishedElements = publishedElement!.Elements("Published");
            foreach (var variantPublishedElement in variantPublishedElements)
            {
                var culture = variantPublishedElement.Attribute("Culture")!.Value;
                publishedStatus.Add(
                    new MigrationPublishStatus(culture, bool.Parse(variantPublishedElement.Value))
                );
            }

            // Get scheduled status
            // TODO support schedule and variants
            var scheduleElement = infoElement.Element("Schedule");

            // Get template info
            var templateElement = infoElement.Element("Template");
            if (templateElement?.HasAttributes == true)
            {
                template = new MigrationTemplateInfo(templateElement.Attribute("Key")!.Value, templateElement.Value);
            }

            // Get properties
            var xmlProperties = contentElement.Element("Properties")!.Elements();
            properties = GetProperties(xmlProperties);

            // Get Public Access Rules
            var xmlPublicAccess = infoElement.Element("PublicAccess");
            publicAccess = GetPublicAccess(xmlPublicAccess);

            return new MigrationContent(key, id, name, level, parentKey, trashed,
                contentType, createDate, nodeNames, sortOrder, publishedStatus,
                scheduluedStatus, template, properties, publicAccess, availableCultures);
        }

        private MigrationPublicAccess? GetPublicAccess(XElement? xmlPublicAccess)
        {
            if (xmlPublicAccess == null) { return null; }

            var loginNodeGuidString = xmlPublicAccess.Attribute("LoginNode")?.Value;
            var noAccessNodeGuidString = xmlPublicAccess.Attribute("NoAccessNode")?.Value;

            var loginNodeGuidParsed = Guid.TryParse(loginNodeGuidString, out var loginNodeGuid);
            var noAccessNodeGuidParsed = Guid.TryParse(noAccessNodeGuidString, out var noAccessNodeGuid);

            var rules = new List<MigrationPublicAccessRule>();

            foreach (var ruleXml in xmlPublicAccess.Elements())
            {
                var type = ruleXml.Attribute("Type")?.Value;
                var value = ruleXml.Value;

                if (type.IsNullOrWhiteSpace() == false && value.IsNullOrWhiteSpace() == false)
                {
                    rules.Add(new MigrationPublicAccessRule(type, value));
                }
            }

            if (rules.Any() == false) { return null; }

            return new MigrationPublicAccess(
                loginNodeGuidParsed ? loginNodeGuid : null,
                noAccessNodeGuidParsed ? noAccessNodeGuid : null,
                rules);
        }
    }
}
