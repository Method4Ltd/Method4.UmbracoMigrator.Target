using Method4.UmbracoMigrator.Source.Core.Services;
using Method4.UmbracoMigrator.Target.Core.Extensions;
using Method4.UmbracoMigrator.Target.Core.Factories;
using Method4.UmbracoMigrator.Target.Core.Helpers;
using Method4.UmbracoMigrator.Target.Core.Hubs;
using Method4.UmbracoMigrator.Target.Core.Mappers;
using Method4.UmbracoMigrator.Target.Core.MediaPathSchemes;
using Method4.UmbracoMigrator.Target.Core.MigrationSteps;
using Method4.UmbracoMigrator.Target.Core.MigrationSteps.Tasks;
using Method4.UmbracoMigrator.Target.Core.Models.DataModels;
using Method4.UmbracoMigrator.Target.Core.Options;
using Method4.UmbracoMigrator.Target.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Extensions;

namespace Method4.UmbracoMigrator.Target.Core;
public class Composer : IComposer
{

    public void Compose(IUmbracoBuilder builder)
    {
        // App Settings
        var options = builder.Config
            .GetSection("Method4")
            .GetSection("UmbracoMigrator")
            .GetSection("Target");
        builder.Services.Configure<MigratorTargetSettings>(options);

        // Factories
        builder.Services.AddTransient<IPreviewFactory, PreviewFactory>();
        builder.Services.AddTransient<IMigrationContentFactory, MigrationContentFactory>();
        builder.Services.AddTransient<IMigrationMediaFactory, MigrationMediaFactory>();

        // Services
        builder.Services.AddTransient<IMigratorBlobService, MigratorBlobService>();
        builder.Services.AddTransient<IMigratorFileService, MigratorFileService>();
        builder.Services.AddTransient<IRelationLookupService, RelationLookupService>();

        // Helpers
        builder.Services.AddTransient<IKeyTransformer, KeyTransformer>();
        builder.Services.AddTransient<IPropertyEditorConverter, PropertyEditorConverter>();

        // Umbraco Mapper
        builder.WithCollectionBuilder<MapDefinitionCollectionBuilder>()
            .Add<NodeRelationMapper>();

        // Custom Mapper implementations
        LoadCustomDocTypeMappings(builder);
        LoadCustomMediaTypeMappings(builder);

        // Default mappers
        builder.Services.AddTransient<IInternalDocTypeMapping, DefaultDocTypeMapper>();
        builder.Services.AddTransient<IInternalMediaTypeMapping, DefaultMediaTypeMapper>();

        // Mapping Collection Service
        builder.Services.AddSingleton<IMappingCollectionService, MappingCollectionService>();

        // Migration Tasks
        builder.Services.AddScoped<IContentStructureTask, ContentStructureTask>();
        builder.Services.AddScoped<IMediaStructureTask, MediaStructureTask>();

        // Migration Phases
        builder.Services.AddScoped<IMigrationPhase1, MigrationPhase1>();
        builder.Services.AddScoped<IMigrationPhase2, MigrationPhase2>();
        builder.Services.AddScoped<IMigrationPhase3, MigrationPhase3>();
        builder.Services.AddScoped<IMigrationPhase4, MigrationPhase4>();

        // SignalR
        builder.Services.AddSignalR();
        builder.Services.AddMigratorSignalR();

        // Media Path Schemes
        builder.Services.AddUnique<IMediaPathScheme, MigratedUrlRedirectMediaPathScheme>();
    }

    private static void LoadCustomDocTypeMappings(IUmbracoBuilder builder)
    {
        var customMappingTypes = builder.TypeLoader.GetTypes<IDocTypeMapping>(false).ToList();
        if (customMappingTypes.Any())
        {
            builder.DocTypeMappers().Append(customMappingTypes);
        }
    }

    private static void LoadCustomMediaTypeMappings(IUmbracoBuilder builder)
    {
        var customMappingTypes = builder.TypeLoader.GetTypes<IMediaTypeMapping>(false).ToList();
        if (customMappingTypes.Any())
        {
            builder.MediaTypeMappers().Append(customMappingTypes);
        }
    }
}

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///  Adds the signalR hub route
    /// </summary>
    public static IServiceCollection AddMigratorSignalR(this IServiceCollection services)
    {
        services.Configure<UmbracoPipelineOptions>(options =>
        {
            options.AddFilter(new UmbracoPipelineFilter(
                "migratorTarget",
                applicationBuilder => { },
                applicationBuilder => { },
                applicationBuilder =>
                {
                    applicationBuilder.UseEndpoints(e =>
                    {
                        //if (runtimeState.Level == Umbraco.Cms.Core.RuntimeLevel.Run)
                        //{
                        e.MapHub<MigrationHub>("/migratorTarget/hub");
                        //}
                    });
                }
            ));
        });

        return services;
    }
}