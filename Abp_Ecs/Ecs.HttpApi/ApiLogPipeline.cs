using Microsoft.AspNetCore.Builder;

namespace Ecs;

public class ApiLogPipeline
{
    public void Configure(IApplicationBuilder app)
    {
        app.UseMiddleware<ApiLogMidware>();
    }
}