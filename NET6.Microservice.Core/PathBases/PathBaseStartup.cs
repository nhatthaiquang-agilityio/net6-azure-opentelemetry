namespace NET6.Microservice.Core.PathBases
{
    public static class PathBaseStartup
    {
        public static void AddPathBaseFilter(WebApplicationBuilder builder)
        {
            // Fetch the PathBaseSettings section from configuration
            var config = builder.Configuration.GetSection("PathBaseSettings");

            // Bind the config section to PathBaseSettings using IOptions
            builder.Services.Configure<PathBaseSettings>(config);

            // Register the startup filter
            builder.Services.AddTransient<IStartupFilter, PathBaseStartupFilter>();
        }
    }
}
