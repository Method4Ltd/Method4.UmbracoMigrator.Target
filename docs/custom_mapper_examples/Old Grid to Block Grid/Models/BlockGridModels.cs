using Newtonsoft.Json;

namespace MySite.Migration.Models;
////////// Block List //////////

public class BlockList
{
    [JsonProperty(PropertyName = "layout")]
    public BlockListLayout Layout { get; set; }

    [JsonProperty(PropertyName = "contentData")]
    public List<object> ContentData { get; set; }

    [JsonProperty(PropertyName = "settingsData")]
    public List<object> SettingsData { get; set; }

    public BlockList()
    {
        Layout = new BlockListLayout();
        ContentData = new List<object>();
        SettingsData = new List<object>();
    }
}

public class BlockListLayout
{
    [JsonProperty(PropertyName = "Umbraco.BlockList")]
    public List<Dictionary<string, string>> UmbracoBlockList { get; set; }

    public BlockListLayout()
    {
        UmbracoBlockList = new List<Dictionary<string, string>>();
    }
}

////////// Block Grid //////////

public class BlockGrid
{
    [JsonProperty(PropertyName = "layout")]
    public BlockGridLayout Layout { get; set; }

    [JsonProperty(PropertyName = "contentData")]
    public List<object> ContentData { get; set; }
    
    [JsonProperty(PropertyName = "settingsData")]
    public List<object> SettingsData { get; set; }

    public BlockGrid()
    {
        Layout = new BlockGridLayout();
        ContentData = new List<object>();
        SettingsData= new List<object>();
    }
}

public class BlockGridLayout
{
    [JsonProperty(PropertyName = "Umbraco.BlockGrid")]
    public List<BlockGridLayoutObject> UmbracoBlockGrid { get; set; }

    public BlockGridLayout()
    {
        UmbracoBlockGrid = new List<BlockGridLayoutObject>();
    }
}

public class BlockGridLayoutObject
{
    [JsonProperty(PropertyName = "contentUdi")]
    public string ContentUdi { get; set; }

    [JsonProperty(PropertyName = "areas")]
    public List<BlockGridLayoutArea> Areas { get; set; }

    [JsonProperty(PropertyName = "columnSpan")]
    public int ColumnSpan { get; set; }

    [JsonProperty(PropertyName = "rowSpan")]
    public int RowSpan { get; set; }

    public BlockGridLayoutObject()
    {
        ContentUdi = "";
        Areas = new List<BlockGridLayoutArea>();
    }
}

public class BlockGridLayoutArea
{
    [JsonProperty(PropertyName = "key")]
    public string Key { get; set; }

    [JsonProperty(PropertyName = "items")]
    public List<BlockGridLayoutObject> Items { get; set; }

    public BlockGridLayoutArea()
    {
        Key = "";
        Items = new List<BlockGridLayoutObject>();
    }
}