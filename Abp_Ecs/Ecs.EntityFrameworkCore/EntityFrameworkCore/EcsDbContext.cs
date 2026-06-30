using Ecs.AgvPlcTcp;
using Ecs.Stations;
using Ecs.WorkPositions;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Ecs.EntityFrameworkCore;

[ConnectionStringName("Default")]
public class EcsDbContext :
    AbpDbContext<EcsDbContext>
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */
    public DbSet<Station> Stations { get; set; }
    public DbSet<StationStep> StationSteps { get; set; }
    public DbSet<StationStepParaLink> StationStepParaLinks { get; set; }
    public DbSet<Variable> Variables { get; set; }
    public DbSet<Step> Steps { get; set; }
    public DbSet<StepPara> StepParas { get; set; }

    public DbSet<AgvTransportTask> AgvTransportTasks { get; set; }
    public DbSet<WorkPosition> WorkPositions { get; set; }


    public EcsDbContext(DbContextOptions<EcsDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Configure your own tables/entities inside here */

        builder.Entity<AgvTransportTask>(b =>
        {
            b.ToTable("AgvTransportTasks");
            b.ConfigureByConvention();
        });

        builder.Entity<WorkPosition>(b =>
        {
            b.ToTable("WorkPositions");
            b.ConfigureByConvention();
        });

        //builder.Entity<YourEntity>(b =>
        //{
        //    b.ToTable(EcsConsts.DbTablePrefix + "YourEntities", EcsConsts.DbSchema);
        //    b.ConfigureByConvention(); //auto configure for the base class props
        //    //...
        //});

    }
}
