using Method4.UmbracoMigrator.Target.Core.Helpers;
using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MySite.Migration.Models;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using MySite.Migration.GridMappers.GridControlMappers;
using MySite.Models.UmbracoModels;

namespace MySite.Migration.GridMappers
{
    public class GridMapper
    {
        private readonly ILogger<GridMapper> _logger;
        private readonly IContentTypeService _contentTypeService;
        private readonly IDataTypeService _dataTypeService;

        private readonly HeroBannerMapper _heroBannerMapper;
        private readonly CallToActionMapper _callToActionMapper;
        private readonly ButtonLinkMapper _buttonLinkMapper;
        private readonly ArticlesHomeMapper _articlesHomeMapper;

        private const string DefaultBlockGridDataTypeName = "Block Grid - Default";

        private readonly JsonSerializerSettings _serializerSettings = new()
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
        };

        public GridMapper(ILogger<GridMapper> logger,
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService,
            HeroBannerMapper heroBannerMapper,
            CallToActionMapper callToActionMapper,
            ButtonLinkMapper buttonLinkMapper,
            ArticlesHomeMapper articlesHomeMapper)
        {
            _logger = logger;
            _contentTypeService = contentTypeService;
            _dataTypeService = dataTypeService;
            _heroBannerMapper = heroBannerMapper;
            _callToActionMapper = callToActionMapper;
            _buttonLinkMapper = buttonLinkMapper;
            _articlesHomeMapper = articlesHomeMapper;
        }


        public IContent MapNodeWithGridComposition(MigrationContent oldNode, IContent newNode, bool overwiteExisitingValues, string blockGridDataTypeName = DefaultBlockGridDataTypeName)
        {
            var newGridAlias = "blockGridBody";

            if (newNode.HasProperty(newGridAlias) == false)
            {
                _logger.LogWarning("{nodeId} does not have property '{propertyAlias}'", newNode.Id, newGridAlias);
                return newNode;
            }

            var oldGrid = oldNode.Properties.FirstOrDefault(x => x.Alias == "body");
            if (oldGrid == null)
            {
                _logger.LogWarning("Old node {nodeId} does not have a grid property", oldNode.Id);
                return newNode;
            }

            // Map EN grid
            if (newNode.GetValue<string>(newGridAlias, "en-GB")?.IsNullOrWhiteSpace() == true || overwiteExisitingValues)
            {
                var oldGridJsonEn = oldGrid.GetValue("en-GB");
                if (!oldGridJsonEn.IsNullOrWhiteSpace())
                {
                    var gridMapResult = TryMapOldGridJsonToBlockGridJson(oldGridJsonEn, out var blockGridJson, blockGridDataTypeName);

                    if (gridMapResult)
                    {
                        newNode.SetValue(newGridAlias, blockGridJson, "en-GB");
                    }
                    else
                    {
                        _logger.LogError("Failed to map EN grid for new {nodeId}", newNode.Id);
                    }
                }
            }

            // Map CY grid
            if (newNode.GetValue<string>(newGridAlias, "cy-GB")?.IsNullOrWhiteSpace() == true || overwiteExisitingValues)
            {
                var oldGridJsonCY = oldGrid.GetValue("cy-GB");
                if (!oldGridJsonCY.IsNullOrWhiteSpace())
                {
                    var gridMapResult = TryMapOldGridJsonToBlockGridJson(oldGridJsonCY, out var blockGridJson, blockGridDataTypeName);

                    if (gridMapResult)
                    {
                        newNode.SetValue("blockGridBody", blockGridJson, "cy-GB");
                    }
                    else
                    {
                        _logger.LogError("Failed to map CY grid for new {nodeId}", newNode.Id);
                    }
                }
            }

            return newNode;
        }

        public bool TryMapOldGridJsonToBlockGridJson(string oldGridJson, out string blockGridJson, string blockGridDataTypeName = DefaultBlockGridDataTypeName)
        {
            try
            {
                blockGridJson = MapOldGridJsonToBlockGridJson(oldGridJson, blockGridDataTypeName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to map old grid json to new block grid json. {errorMessage}", ex.Message);
                blockGridJson = "";
                return false;
            }
        }

        public string MapOldGridJsonToBlockGridJson(string oldGridJson, string blockGridDataTypeName = DefaultBlockGridDataTypeName)
        {
            var oldGrid = JObject.Parse(oldGridJson);
            var oldGridSections = (JArray)oldGrid!["sections"];

            var blockGrid = new BlockGrid();

            foreach (var oldGridSection in oldGridSections)
            {
                var oldGridRows = (JArray)oldGridSection["rows"];
                if (oldGridRows == null) { continue; }

                foreach (var oldGridRow in oldGridRows)
                {
                    var oldGridRowType = (string)oldGridRow["name"];
                    var oldGridAreas = (JArray)oldGridRow["areas"];
                    if (oldGridAreas == null) { continue; }

                    var blockGridLayout = new BlockGridLayoutObject()
                    {
                        ContentUdi = "",
                        ColumnSpan = 12,
                        RowSpan = 1
                    };

                    // Get our Block Grid data type's configuration
                    var blockGridDataType = (BlockGridConfiguration)_dataTypeService.GetDataType(blockGridDataTypeName)!.Configuration!;

                    // Create layout block
                    switch (oldGridRowType)
                    {
                        ////// Six Columns //////
                        case "Links":
                            CreateLayoutBlock(SixColumns.ModelTypeAlias, 6, blockGridDataType, ref blockGrid, ref blockGridLayout);
                            break;

                        ////// Two Columns //////
                        case "TwoColumn":
                            CreateLayoutBlock(TwoColumns.ModelTypeAlias, 2, blockGridDataType, ref blockGrid, ref blockGridLayout);
                            break;

                        ////// One Column //////
                        case "Primary Banner":
                        case "Call To Action":
                        case "Articles":
                        default:
                            CreateLayoutBlock(OneColumn.ModelTypeAlias, 1, blockGridDataType, ref blockGrid, ref blockGridLayout);
                            break;
                    }

                    var areaIndex = 0;
                    foreach (var oldGridArea in oldGridAreas)
                    {
                        var oldGridControls = (JArray)oldGridArea["controls"];
                        foreach (var oldGridControl in oldGridControls)
                        {
                            try
                            {
                                var newGridBlock = MapGridControlJson(oldGridControl);
                                if (newGridBlock == null) { continue; }

                                blockGrid.ContentData.Add(newGridBlock);

                                switch (oldGridRowType)
                                {
                                    ////// Six Columns //////
                                    case "Links":
                                        blockGridLayout.Areas[areaIndex].Items.Add(new BlockGridLayoutObject()
                                        {
                                            ContentUdi = newGridBlock.udi,
                                            Areas = new List<BlockGridLayoutArea>(),
                                            ColumnSpan = 2,
                                            RowSpan = 1
                                        });
                                        break;

                                    ////// Two Columns //////
                                    case "TwoColumn":
                                        blockGridLayout.Areas[areaIndex].Items.Add(new BlockGridLayoutObject()
                                        {
                                            ContentUdi = newGridBlock.udi,
                                            Areas = new List<BlockGridLayoutArea>(),
                                            ColumnSpan = 6,
                                            RowSpan = 1
                                        });
                                        break;

                                    ////// One Column //////
                                    case "Primary Banner":
                                    case "Call To Action":
                                    case "Articles":
                                    default:
                                        blockGridLayout.Areas[0].Items.Add(new BlockGridLayoutObject()
                                        {
                                            ContentUdi = newGridBlock.udi,
                                            Areas = new List<BlockGridLayoutArea>(),
                                            ColumnSpan = 12,
                                            RowSpan = 1
                                        });
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to map grid control");
                                throw;
                            }
                        }
                        areaIndex++;
                    }

                    blockGrid.Layout.UmbracoBlockGrid.Add(blockGridLayout);
                }
            }

            return JsonConvert.SerializeObject(blockGrid);
        }

        private void CreateLayoutBlock(string layoutModelTypeAlias, int areaCount, BlockGridConfiguration blockGridDataType, ref BlockGrid blockGrid, ref BlockGridLayoutObject blockGridLayout)
        {
            // Get the Layout's DocType
            var layoutType = _contentTypeService.Get(layoutModelTypeAlias)!;

            // Get the block configuration from our Block Grid
            var layoutBlockConfig = blockGridDataType.Blocks.FirstOrDefault(x => x.ContentElementTypeKey == layoutType.Key);

            // Create Layout block
            var layoutBlock = new
            {
                contentTypeKey = layoutType.Key.ToString(),
                udi = new GuidUdi("element", Guid.NewGuid()).ToString(),
            };

            // Add to the ContentData list
            blockGrid.ContentData.Add(layoutBlock);

            // Update Layout data
            blockGridLayout.ContentUdi = layoutBlock.udi;
            for (int i = 0; i < areaCount; i++)
            {
                blockGridLayout.Areas.Add(new BlockGridLayoutArea()
                {
                    Key = layoutBlockConfig!.Areas[i].Key.ToString(),
                    Items = new List<BlockGridLayoutObject>()
                });
            }
        }

        /// <summary>
        /// Old grid json example:
        ///{
        /// "value": {
        ///     "dtgeContentTypeAlias": "shieldEditor",
        ///     "value": {
        ///         "name": "Shield",
        ///         "shield": "umb://document/4bd3b1176c6745a781e0d99567a8a261"
        ///     },
        ///     "id": "dc07f6bd-a61c-16bb-a682-40cf8965990e"
        /// },
        /// "editor": {
        ///     "alias": "shieldEditorGridControl",
        ///     "view": "/App_Plugins/DocTypeGridEditor/Views/doctypegrideditor.html"
        /// },
        /// "styles": null,
        /// "config": null
        ///}
        /// </summary>
        /// <param name="oldGridControl"></param>
        /// <returns></returns>
        private dynamic MapGridControlJson(JToken oldGridControl)
        {
            var oldEditorAlias = (oldGridControl["editor"])["alias"]!.ToString();
            var oldValue = oldGridControl["value"]!;
            dynamic newBlockJson = null;

            if (oldValue.Type == JTokenType.Null)
            {
                return newBlockJson;
            }

            switch (oldEditorAlias)
            {
                case "macro":
                    var oldMacroAlias = oldValue["macroAlias"]!.ToString();
                    var macroParamsDictionary = oldValue["macroParamsDictionary"];

                    switch (oldMacroAlias)
                    {
                        case "ButtonLink":
                            newBlockJson = _buttonLinkMapper.Map(JObject.Parse(macroParamsDictionary.ToString()));
                            break;

                        case "ArticlesHome":
                            newBlockJson = _articlesHomeMapper.Map(JObject.Parse(macroParamsDictionary.ToString()));
                            break;

                        default:
                            throw new NotImplementedException();
                    }

                    break;

                case "CallToAction":
                    newBlockJson = _callToActionMapper.Map(JObject.Parse(oldValue.ToString()));
                    break;

                case "HeroBanner":
                    newBlockJson = _heroBannerMapper.Map(JObject.Parse(oldValue.ToString()));
                    break;

                default:
                    throw new NotImplementedException();
            }

            return newBlockJson!;
        }
    }
}
