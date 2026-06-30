using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ecs.LogTool;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace Ecs.Stations;

public class StationManager : ISingletonDependency
{
    private IRepository<Station> _stationRepository;
    private ILogger<StationManager> _logger;
    private IServiceProvider _serviceProvider;

    public StationManager(
        IRepository<Station> stationRepository,
        IServiceProvider serviceProvider,
        ILogger<StationManager> logger)
    {
        _stationRepository = stationRepository;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 添加站点
    /// </summary>
    /// <param name="station"></param>
    /// <returns>添加失败，返回null</returns>
    public async Task<Station> AddStationAsync(Station station)
    {
        try
        {
            var stations = await _stationRepository.GetListAsync(
                o => o.StationCode == station.StationCode ||
                o.StationName == station.StationName)
                .ConfigureAwait(false);
            if(stations.Count > 0)
                throw new Exception($"StationCode为{station.StationCode}或StationName为{station.StationName}的站点已经存在");
            
            return await _stationRepository.InsertAsync(station).ConfigureAwait(false);
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// 记录多个站点
    /// </summary>
    /// <param name="stations"></param>
    /// <returns></returns>
    [UnitOfWork]
    public async Task<bool> AddStationsAsync(List<Station> stations)
    {
        try
        {
            //本身是否存在重复
            foreach(var s in stations)
            {
                var ss = stations.Where(o => o.StationCode == s.StationCode ||
                    o.StationName == s.StationName).ToList();
                if(ss.Count > 1)
                    throw new Exception($"StationCode为{s.StationCode}的站点重复");
            }
            //是否已记录过
            var stationsSaved = await _stationRepository.GetListAsync().ConfigureAwait(false);
            foreach(var s in stations)
            {
                var ss = stationsSaved.Where(o => o.StationCode == s.StationCode ||
                    o.StationName == s.StationName).ToList();
                if(ss.Count > 0)
                    throw new Exception($"StationCode为{s.StationCode}的站点已经存在");
            }
            //逐一记录
            foreach(var s in stations)
            {
                await _stationRepository.InsertAsync(s).ConfigureAwait(false);
            }
            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 根据站点编号查询站点
    /// </summary>
    /// <param name="stationCode"></param>
    /// <returns>未查询到或发生错误返回null</returns>
    public async Task<Station> GetStationByCodeAsync(string stationCode)
    {
        try
        {
            var stations = await _stationRepository.GetListAsync(o => o.StationCode == stationCode).ConfigureAwait(false);
            if(stations.Count == 0)
                return null;
            if(stations.Count > 1)
                throw new Exception($"StationCode为{stationCode}的站点数据不止1个");
            return stations[0];
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// 查询所有的站点
    /// </summary>
    /// <returns>按Id升序返回，发生错误时，返回空集合</returns>
    public async Task<List<Station>> GetAllStationsAsync()
    {
        try
        {
            var stations = await _stationRepository.GetListAsync().ConfigureAwait(false);
            return stations.OrderBy(o => o.Id).ToList();
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<Station>();
        }
    }

    /// <summary>
    /// 根据站点编号删除对应的站点
    /// </summary>
    /// <param name="stationCode"></param>
    /// <returns></returns>
    public async Task<bool> RemoveStationWithCodeAsync(string stationCode)
    {
        try
        {
            await _stationRepository.DeleteAsync(o => o.StationCode == stationCode).ConfigureAwait(false);
            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 删除所有的站点
    /// </summary>
    /// <returns></returns>
    public async Task<bool> RemoveAllStationsAsync()
    {
        try
        {
            await _stationRepository.DeleteAsync(o => o.Id > 0).ConfigureAwait(false);
            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    
    public StationExecutor CreateExecutor(string stationCode)
    {
        try
        {
            Assembly ass = Assembly.Load("Ecs.Domain");
            Type classType = ass.GetType($"Ecs.Stations.StationExecutor");
            if (classType == null)
            {
                return null;
            }

            var executor = (StationExecutor)_serviceProvider.GetService(classType);
            executor.StationCode = stationCode;
            return executor;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
        }
    }
}