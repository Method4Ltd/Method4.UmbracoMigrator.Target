using System.Text.Json.Serialization;

namespace MySite.Migration.Models;

public class RichText 
{
    public string Markup { get; set; }
    public RichTextBlocks Blocks { get; set; }
}

public class RichTextBlocks 
{
    public UmbracoTinyMCERichText Layout { get; set; }
    public List<Dictionary<string, string>> ContentData { get; set; }
    public List<Dictionary<string, string>> SettingsData { get; set; }
}

public class UmbracoTinyMCERichText 
{
    /// JsonPropertyName endures that when the object is serialized it is 
    /// given the name defined in the attribute. This allows for naming the
    /// properties with '.' as below. Can also be used for all props to ensure
    /// camelCase as is standard with JSON however it is not required as far as
    /// this author is aware 
    [JsonPropertyName("Umbraco.TinyMCE")]
    public List<Dictionary<string, string>> ContentUdis { get; set; } = new List<Dictionary<string, string>>();
}

public class ImageBlock 
{
        [JsonPropertyName("contentTypeKey")]
        public string ContentTypeKey { get; set; }
        [JsonPropertyName("udi")]
        public string Udi { get; set; }
        [JsonPropertyName("image")]
        public string Image { get; set; }
}

public class ImageMacro 
{
    public string Image { get; set; } 
}