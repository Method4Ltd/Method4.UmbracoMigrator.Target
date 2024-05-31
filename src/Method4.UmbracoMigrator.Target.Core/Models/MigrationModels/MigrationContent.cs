namespace Method4.UmbracoMigrator.Target.Core.Models.MigrationModels
{
    public class MigrationContent : MigrationBase
    {
        public List<MigrationPublishStatus> PublishedStatus { get; set; }
        public List<MigrationScheduleStatus> ScheduledStatus { get; set; }
        public MigrationTemplateInfo? Template { get; set; }
        public List<MigrationProperty> Properties { get; set; }
        public MigrationPublicAccess? PublicAccess { get; set; }
        public List<string> AvailableCultures { get; set; }

        public bool VariesByCulture => AvailableCultures.Count > 1;

        public MigrationContent(Guid key,
            string id,
            string name,
            int level,
            Guid parentKey,
            bool trashed,
            string contentType,
            DateTime createDate,
            List<MigrationNodeName> nodeNames,
            int sortOrder,
            List<MigrationPublishStatus> publishedStatus,
            List<MigrationScheduleStatus> scheduledStatus,
            MigrationTemplateInfo? template,
            List<MigrationProperty> properties, MigrationPublicAccess? publicAccess, List<string> availableCultures)
            : base(key, id, name, level, parentKey, trashed, contentType, createDate, nodeNames, sortOrder)
        {
            PublishedStatus = publishedStatus;
            ScheduledStatus = scheduledStatus;
            Template = template;
            Properties = properties;
            PublicAccess = publicAccess;
            AvailableCultures = availableCultures;
        }

        /// <summary>
        /// Returns the default culture of this old node
        /// </summary>
        /// <returns></returns>
        public string? GetDefaultCulture()
        {
            if (VariesByCulture == false)
            {
                return AvailableCultures.FirstOrDefault();
            }

            var defaultName = NodeNames.FirstOrDefault(x => x.Name == Name && x.Culture != "default");
            return defaultName?.Culture;
        }
    }

    public class MigrationPublishStatus
    {
        public string Culture { get; set; }
        public bool Published { get; set; }

        public MigrationPublishStatus(string culture, bool published)
        {
            Culture = culture;
            Published = published;
        }
    }

    public class MigrationScheduleStatus
    {
        public string Culture { get; set; }
        public MigrationScheduleStatusAction Action { get; set; }
        public DateTime Date { get; set; }

        public MigrationScheduleStatus(string culture, MigrationScheduleStatusAction action, DateTime date)
        {
            Culture = culture;
            Action = action;
            Date = date;
        }
    }

    public enum MigrationScheduleStatusAction
    {
        Publish = 0,
        Expire = 1
    }

    public class MigrationTemplateInfo
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public MigrationTemplateInfo(string key, string name)
        {
            Key = key;
            Name = name;
        }
    }

    public class MigrationPublicAccess
    {
        public Guid? LoginNodeKey { get; set; }
        public Guid? NoAccessNodeKey { get; set; }
        public List<MigrationPublicAccessRule> Rules { get; set; }

        public MigrationPublicAccess(Guid? loginNodeKey, Guid? noAccessNodeKey, List<MigrationPublicAccessRule> rules)
        {
            LoginNodeKey = loginNodeKey;
            NoAccessNodeKey = noAccessNodeKey;
            Rules = rules;
        }
    }

    public class MigrationPublicAccessRule
    {
        public string Type { get; set; }
        public string Value { get; set; }

        public MigrationPublicAccessRule(string type, string value)
        {
            Type = type;
            Value = value;
        }
    }
}
