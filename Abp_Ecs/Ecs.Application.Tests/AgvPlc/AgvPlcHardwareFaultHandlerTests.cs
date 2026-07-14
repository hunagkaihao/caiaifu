using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Ecs.AgvPlc;
using Ecs.AgvPlcTcp;
using Ecs.Rcs;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Ecs.Application.Tests.AgvPlc;

public class AgvPlcHardwareFaultHandlerTests
{
    [Fact]
    public async Task Fault_freezes_both_zones_without_changing_task_status()
    {
        var task = CreateTask(AgvTransportTaskStatuses.Submitted);
        var repository = CreateRepository(task);
        var rcs = Substitute.For<IRcsApiClient>();
        rcs.ControlZonePauseAsync(Arg.Any<RcsZonePauseRequest>(), Arg.Any<CancellationToken>())
            .Returns(Success());
        var resolver = Substitute.For<IAgvTaskZoneResolver>();
        resolver.ResolvePointAsync("O1A", Arg.Any<CancellationToken>()).Returns("6061");
        resolver.ResolvePointAsync("O1B", Arg.Any<CancellationToken>()).Returns("6060");
        var handler = CreateHandler(repository, rcs, resolver);

        var handled = await handler.HandleAsync(
            "O1A",
            AgvPlcHardwareStatus.FromStatusByte(0x18));

        Assert.True(handled);
        Assert.Equal(AgvTransportTaskStatuses.Submitted, task.Status);
        await rcs.Received(1).ControlZonePauseAsync(
            Arg.Is<RcsZonePauseRequest>(x =>
                x.ZoneCode == "6061" && x.MapCode == "AA" && x.Invoke == "FREEZE"),
            Arg.Any<CancellationToken>());
        await rcs.Received(1).ControlZonePauseAsync(
            Arg.Is<RcsZonePauseRequest>(x =>
                x.ZoneCode == "6060" && x.MapCode == "AA" && x.Invoke == "FREEZE"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fault_attempts_target_zone_when_source_zone_pause_fails()
    {
        var task = CreateTask(AgvTransportTaskStatuses.ArriveDockStation1);
        var repository = CreateRepository(task);
        var rcs = Substitute.For<IRcsApiClient>();
        rcs.ControlZonePauseAsync(
                Arg.Is<RcsZonePauseRequest>(x => x.ZoneCode == "6061"),
                Arg.Any<CancellationToken>())
            .Returns(new RcsApiResponse<object> { Code = "FAILED", Success = false });
        rcs.ControlZonePauseAsync(
                Arg.Is<RcsZonePauseRequest>(x => x.ZoneCode == "6060"),
                Arg.Any<CancellationToken>())
            .Returns(Success());
        var resolver = Substitute.For<IAgvTaskZoneResolver>();
        resolver.ResolvePointAsync("O1A", Arg.Any<CancellationToken>()).Returns("6061");
        resolver.ResolvePointAsync("O1B", Arg.Any<CancellationToken>()).Returns("6060");
        var handler = CreateHandler(repository, rcs, resolver);

        var handled = await handler.HandleAsync(
            "O1A",
            AgvPlcHardwareStatus.FromStatusByte(0x11));

        Assert.False(handled);
        await rcs.Received(1).ControlZonePauseAsync(
            Arg.Is<RcsZonePauseRequest>(x => x.ZoneCode == "6061"),
            Arg.Any<CancellationToken>());
        await rcs.Received(1).ControlZonePauseAsync(
            Arg.Is<RcsZonePauseRequest>(x => x.ZoneCode == "6060"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fault_pauses_target_zone_when_source_zone_mapping_fails()
    {
        var task = CreateTask(AgvTransportTaskStatuses.Submitted);
        var repository = CreateRepository(task);
        var rcs = Substitute.For<IRcsApiClient>();
        rcs.ControlZonePauseAsync(Arg.Any<RcsZonePauseRequest>(), Arg.Any<CancellationToken>())
            .Returns(Success());
        var resolver = Substitute.For<IAgvTaskZoneResolver>();
        resolver.ResolvePointAsync("O1A", Arg.Any<CancellationToken>())
            .Returns<Task<string>>(_ => throw new InvalidOperationException("missing source mapping"));
        resolver.ResolvePointAsync("O1B", Arg.Any<CancellationToken>()).Returns("6060");
        var handler = CreateHandler(repository, rcs, resolver);

        var handled = await handler.HandleAsync(
            "O1A",
            AgvPlcHardwareStatus.FromStatusByte(0x18));

        Assert.False(handled);
        await rcs.DidNotReceive().ControlZonePauseAsync(
            Arg.Is<RcsZonePauseRequest>(x => x.ZoneCode == "6061"),
            Arg.Any<CancellationToken>());
        await rcs.Received(1).ControlZonePauseAsync(
            Arg.Is<RcsZonePauseRequest>(x => x.ZoneCode == "6060"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fault_does_not_pause_task_for_an_unrelated_point()
    {
        var task = CreateTask(AgvTransportTaskStatuses.Submitted);
        var repository = CreateRepository(task);
        var rcs = Substitute.For<IRcsApiClient>();
        var resolver = Substitute.For<IAgvTaskZoneResolver>();
        var handler = CreateHandler(repository, rcs, resolver);

        var handled = await handler.HandleAsync(
            "O2A",
            AgvPlcHardwareStatus.FromStatusByte(0x18));

        Assert.False(handled);
        await resolver.DidNotReceive().ResolvePointAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await rcs.DidNotReceive().ControlZonePauseAsync(
            Arg.Any<RcsZonePauseRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(AgvTransportTaskStatuses.Completed)]
    [InlineData(AgvTransportTaskStatuses.Cancelled)]
    [InlineData(AgvTransportTaskStatuses.RcsFailed)]
    [InlineData("Cancelling")]
    [InlineData("CancelRecoveryRequired")]
    public async Task Fault_ignores_tasks_that_are_not_executing(string status)
    {
        var task = CreateTask(status);
        var repository = CreateRepository(task);
        var rcs = Substitute.For<IRcsApiClient>();
        var resolver = Substitute.For<IAgvTaskZoneResolver>();
        var handler = CreateHandler(repository, rcs, resolver);

        var handled = await handler.HandleAsync(
            "O1A",
            AgvPlcHardwareStatus.FromStatusByte(0x09));

        Assert.False(handled);
        await resolver.DidNotReceive().ResolvePointAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await rcs.DidNotReceive().ControlZonePauseAsync(
            Arg.Any<RcsZonePauseRequest>(),
            Arg.Any<CancellationToken>());
    }

    private static IRepository<AgvTransportTask, Guid> CreateRepository(params AgvTransportTask[] tasks)
    {
        var repository = Substitute.For<IRepository<AgvTransportTask, Guid>>();
        repository.GetListAsync(
                Arg.Any<Expression<Func<AgvTransportTask, bool>>>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var predicate = call.Arg<Expression<Func<AgvTransportTask, bool>>>().Compile();
                return new List<AgvTransportTask>(tasks.Where(predicate));
            });
        return repository;
    }

    private static AgvPlcHardwareFaultHandler CreateHandler(
        IRepository<AgvTransportTask, Guid> repository,
        IRcsApiClient rcs,
        IAgvTaskZoneResolver resolver)
    {
        return new AgvPlcHardwareFaultHandler(
            repository,
            rcs,
            resolver,
            NullLogger<AgvPlcHardwareFaultHandler>.Instance);
    }

    private static AgvTransportTask CreateTask(string status)
    {
        return new AgvTransportTask(Guid.NewGuid())
        {
            SourcePointCode = "O1A",
            TargetPointCode = "O1B",
            EdgeCode = "A-B",
            Status = status
        };
    }

    private static RcsApiResponse<object> Success()
    {
        return new RcsApiResponse<object>
        {
            Code = "SUCCESS",
            Success = true,
            Data = new object()
        };
    }
}