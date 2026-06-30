using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace Ecs;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);      
            builder.Host.UseAutofac();
            await builder.AddApplicationAsync<EcsHttpApiHostModule>();

            var configuration = builder.Services.GetConfiguration();
            string[] urls = configuration["Ecs:BaseUrl"].Split(",", System.StringSplitOptions.RemoveEmptyEntries);
            builder.WebHost.UseUrls(urls);
            
            var app = builder.Build();
            // app.UseMiddleware<ApiLogMidware>();
            await app.InitializeApplicationAsync();
            app.MapHub<EcsSignalHub>("/hub");

            await app.RunAsync();
            return 0;
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
            return 1;
        }
    }
}
