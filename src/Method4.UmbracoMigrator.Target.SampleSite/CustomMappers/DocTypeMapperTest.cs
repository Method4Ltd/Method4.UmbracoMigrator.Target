using Method4.UmbracoMigrator.Target.Core.Helpers;
using Method4.UmbracoMigrator.Target.Core.Mappers;
using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Umbraco.Cms.Core.Models;
using uSync;

public class DocTypeMapperTest : IDocTypeMapping
{
    private readonly IPropertyEditorConverter _propertyEditorConverter;
    private readonly IKeyTransformer _keyTransformer;

    public DocTypeMapperTest(IPropertyEditorConverter propertyEditorConverter, IKeyTransformer keyTransformer)
    {
        _propertyEditorConverter = propertyEditorConverter;
        _keyTransformer = keyTransformer;
    }

    public string DocTypeAlias => "migratorTest"; // The doctype I want to map to

    public bool CanIMap(MigrationContent MigrationNode)
    {
        return MigrationNode.ContentType == "migratorTest"; // I can map old nodes of this doctype
    }

    public IContent MapNode(MigrationContent oldNode, IContent newNode, bool overwiteExisitingValues)
    {
        // Map a property with a different name
        var oldRenamedPropertyTest = oldNode.Properties.First(x => x.Alias == "oldRenamedPropertyTest");
        newNode.SetValue("newRenamedPropertyTest", oldRenamedPropertyTest.GetDefaultValue);

        // Map a property that varies by culture
        //var oldCultureProperty = oldNode.Properties.FirstOrDefault(x => x.Alias == "oldCultureProperty");
        //if (oldCultureProperty != null)
        //{
        //    foreach (var value in oldCultureProperty.Values)
        //    {
        //        if (value.Value == null) { continue; }

        //        if (value.Culture == "default")
        //        {
        //            newNode.SetValue("newCultureProperty", value.Value);
        //        }
        //        else
        //        {
        //            newNode.SetValue("newCultureProperty", value.Value, value.Culture);
        //        }
        //    }
        //}

        // Map a property that varies by culture - Alternative Method
        var oldCulturePropertyEn = oldNode.Properties.First(x => x.Alias == "oldCultureProperty").GetValue("en-GB");
        var oldCulturePropertyCy = oldNode.Properties.First(x => x.Alias == "oldCultureProperty").GetValue("cy-GB");
        newNode.SetValue("newCultureProperty", oldCulturePropertyEn, "en-GB");
        newNode.SetValue("newCultureProperty", oldCulturePropertyCy, "cy-GB");

        // Convert old legacy MediaPicker to new MediaPicker3 test
        var multipleMediaPickerLegacy = oldNode.Properties.First(x => x.Alias == "multipleMediaPickerLegacy");
        if (multipleMediaPickerLegacy.GetDefaultValue.IsNullOrWhiteSpace() == false)
        {
            var oldMediaPickerValue = _keyTransformer.TransformOldKeyReferences("Umbraco.MediaPicker", multipleMediaPickerLegacy.GetDefaultValue!);
            var newMediaPickerValue = _propertyEditorConverter.ConvertMediaPickerValueToMediaPicker3Value(oldMediaPickerValue);
            newNode.SetValue("newMediaPickerConvertTest", newMediaPickerValue);
        }

        return newNode;
    }
}