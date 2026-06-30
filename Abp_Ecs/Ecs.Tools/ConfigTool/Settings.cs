using Microsoft.Extensions.Configuration;
using System;

namespace Ecs.ConfigTool;

public static class Settings
{
    public static ConfigOptions Options = new ConfigOptions();

    static Settings()
    {
        IConfigurationRoot root = new ConfigurationBuilder()
                .AddJsonFile($@"{AppDomain.CurrentDomain.BaseDirectory}appsettings.json", optional: false).Build();

        root.GetSection("Ecs").Bind(Options);
    }
}
