using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using System.Xml.Linq;
using Umbraco.Extensions;

namespace Method4.UmbracoMigrator.Target.Core.Factories
{
    public class MigrationFactoryBase
    {
        protected List<MigrationProperty> GetProperties(IEnumerable<XElement> xmlProperties)
        {
            var properties = new List<MigrationProperty>();

            foreach (var xmlProperty in xmlProperties)
            {
                var propertyAlias = xmlProperty.Name.LocalName;
                var propertyEditorAlias = xmlProperty.Attribute("PropertyEditorAlias")!.Value;
                var valuesXml = xmlProperty.Elements("Value");
                var values = new List<MigrationPropertyValue>();

                foreach (var valueXml in valuesXml)
                {
                    var value = valueXml.Value.IsNullOrWhiteSpace() ? null : valueXml.Value;
                    var cultureAttr = valueXml.Attribute("Culture");
                    if (cultureAttr == null) // does not support variants
                    {
                        values.Add(new MigrationPropertyValue(value));
                    }
                    else
                    {
                        values.Add(new MigrationPropertyValue(value, cultureAttr.Value));
                    }
                }

                properties.Add(new MigrationProperty(propertyAlias, propertyEditorAlias, values));
            }

            return properties;
        }
    }
}