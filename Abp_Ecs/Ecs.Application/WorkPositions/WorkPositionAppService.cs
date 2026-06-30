using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace Ecs.WorkPositions;

public class WorkPositionAppService : EcsAppService, IWorkPositionAppService
{
    private readonly IRepository<WorkPosition, int> _repository;
    private readonly ILogger<WorkPositionAppService> _logger;

    public WorkPositionAppService(
        IRepository<WorkPosition, int> repository,
        ILogger<WorkPositionAppService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ResponseDto> AddAsync(WorkPositionDto input)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input.DeviceName))
            {
                return Fail("设备名称不能为空");
            }

            if (string.IsNullOrWhiteSpace(input.SiteName))
            {
                return Fail("站点名称不能为空");
            }

            var status = NormalizeStatus(input.Status);
            if (status == null)
            {
                return Fail("状态只能为「可用」或「禁用」");
            }

            var entity = new WorkPosition
            {
                DeviceName = input.DeviceName.Trim(),
                SiteName = input.SiteName.Trim(),
                Status = status
            };

            await _repository.InsertAsync(entity, autoSave: true).ConfigureAwait(false);
            return Success("添加成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加工位失败");
            return Fail(ex.Message);
        }
    }

    public async Task<ResponseDto> UpdateAsync(WorkPositionDto input)
    {
        try
        {
            if (input.Id <= 0)
            {
                return Fail("Id 无效");
            }

            if (string.IsNullOrWhiteSpace(input.DeviceName))
            {
                return Fail("设备名称不能为空");
            }

            if (string.IsNullOrWhiteSpace(input.SiteName))
            {
                return Fail("站点名称不能为空");
            }

            var status = NormalizeStatus(input.Status);
            if (status == null)
            {
                return Fail("状态只能为「可用」或「禁用」");
            }

            var entity = await _repository.GetAsync(input.Id).ConfigureAwait(false);
            entity.DeviceName = input.DeviceName.Trim();
            entity.SiteName = input.SiteName.Trim();
            entity.Status = status;

            await _repository.UpdateAsync(entity, autoSave: true).ConfigureAwait(false);
            return Success("更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新工位失败，Id={Id}", input.Id);
            return Fail(ex.Message);
        }
    }

    public async Task<ResponseDto> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Fail("Id 无效");
            }

            await _repository.DeleteAsync(id, autoSave: true).ConfigureAwait(false);
            return Success("删除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除工位失败，Id={Id}", id);
            return Fail(ex.Message);
        }
    }

    public async Task<List<WorkPositionDto>> GetAllAsync()
    {
        try
        {
            var entities = await _repository.GetListAsync().ConfigureAwait(false);
            return ObjectMapper.Map<List<WorkPosition>, List<WorkPositionDto>>(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询工位列表失败");
            return new List<WorkPositionDto>();
        }
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return WorkPositionStatus.Available;
        }

        var normalized = status.Trim();
        if (normalized == WorkPositionStatus.Available || normalized == WorkPositionStatus.Disabled)
        {
            return normalized;
        }

        return null;
    }

    private static ResponseDto Success(string message) =>
        new() { success = true, message = message };

    private static ResponseDto Fail(string message) =>
        new() { success = false, message = message };
}
