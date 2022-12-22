using Microsoft.Extensions.Options;

namespace NET6.Microservice.Core.PathBases
{
    public class PathBaseStartupFilter : IStartupFilter
    {
        private readonly string _pathBase;

        // Takes an IOptions<PathBaseSettings> instead of a string directly
        public PathBaseStartupFilter(IOptions<PathBaseSettings> options)
        {
            PathBaseSettings value = options.Value;
            if (value != null)
            {
                _pathBase = value.ApplicationPathBase;
            }
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.UsePathBase(_pathBase);
                next(app);
            };
        }
    }

}