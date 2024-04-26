// Configure Builder
var builder = WebApplication.CreateBuilder(args);
ConfigureBuilder(builder);

// Configure App
var app = builder.Build();
await app.BootUmbracoAsync();
ConfigureApp(app, app.Environment);
await app.RunAsync();
return;

static void ConfigureBuilder(WebApplicationBuilder builder)
{
    builder.CreateUmbracoBuilder()
        .AddBackOffice()
        .AddWebsite()
        .AddDeliveryApi()
        .AddComposers()
        //.AddAzureBlobMediaFileSystem() // This configures the required services for Media
        //.AddAzureBlobImageSharpCache() // This configures the required services for the Image Sharp cache
        .Build();
}

static void ConfigureApp(WebApplication app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseHsts();
    }

    app.Use(async (context, next) =>
    {
        // Click Jacking protection
        context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
        // Content/MIME Sniffing Protection
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        await next();
    });

    // Configure Umbraco Services
    app.UseUmbraco()
        .WithMiddleware(u =>
        {
            u.UseBackOffice();
            u.UseWebsite();
        })
        .WithEndpoints(u =>
        {
            u.UseInstallerEndpoints();
            u.UseBackOfficeEndpoints();
            u.UseWebsiteEndpoints();
            u.EndpointRouteBuilder.MapControllers();
        });

    // Add static files to the request pipeline.
    app.UseStaticFiles();
}