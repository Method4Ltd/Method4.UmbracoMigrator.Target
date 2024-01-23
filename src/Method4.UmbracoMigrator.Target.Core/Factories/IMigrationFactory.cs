using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Method4.UmbracoMigrator.Target.Core.Factories
{
    public interface IMigrationContentFactory
    {
        public List<MigrationContent> ConvertFromXml(IEnumerable<XElement> contentElements);
        public MigrationContent ConvertFromXml(XElement contentElement);
    }

    public interface IMigrationMediaFactory
    {
        public List<MigrationMedia> ConvertFromXml(IEnumerable<XElement> mediaElements);
        public MigrationMedia ConvertFromXml(XElement mediaElement);
    }
}
