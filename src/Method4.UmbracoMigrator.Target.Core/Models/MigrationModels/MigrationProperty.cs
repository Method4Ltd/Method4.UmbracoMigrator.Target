namespace Method4.UmbracoMigrator.Target.Core.Models.MigrationModels
{
    public class MigrationProperty
    {
        public string Alias { get; set; }
        public string PropertyEditorAlias { get; set; }
        public List<MigrationPropertyValue> Values { get; set; }

        /// <summary>
        /// Returns the default value. If a property has no value, or it varies by culture, then this will return null.
        /// </summary>
        public string? GetDefaultValue => Values.FirstOrDefault(x => x.Culture == "default")?.Value;

        /// <summary>
        /// Returns the value of the given culture. Returns null if no value for that culture exists.
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
        public string? GetValue(string culture) => Values.FirstOrDefault(x => x.Culture.ToLower() == culture.ToLower())?.Value;

        public MigrationProperty(string alias, string propertyEditorAlias, List<MigrationPropertyValue> values)
        {
            Alias = alias;
            PropertyEditorAlias = propertyEditorAlias;
            Values = values;
        }

        public MigrationProperty(string alias, string propertyEditorAlias, string value)
        {
            Alias = alias;
            PropertyEditorAlias = propertyEditorAlias;
            Values = new List<MigrationPropertyValue>()
            {
                new MigrationPropertyValue(value)
            };
        }
    }

    public class MigrationPropertyValue
    {
        public string? Value { get; set; }
        public string Culture { get; set; }

        public MigrationPropertyValue(string? value, string culture = "default")
        {
            Value = value;
            Culture = culture;
        }
    }
}