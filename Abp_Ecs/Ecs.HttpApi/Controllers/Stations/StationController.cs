using System.Collections.Generic;
using System.Threading.Tasks;
using Ecs.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Ecs.Stations;

[Route("ecs/station")]
[ApiController]
public class StationController : EcsController, IStationMonitorService, IStationService, IStepService, IVariableService, IStationStepService
{
    private readonly IStationMonitorService _stationMonitorService;
    private readonly IStationService _stationService;
    private readonly IStepService _stepService;
    private readonly IVariableService _variableService;
    private readonly IStationStepService _stationStepService;

    public StationController(
        IStationMonitorService stationMonitorService,
        IStationService stationService,
        IStepService stepService,
        IVariableService variableService,
        IStationStepService stationStepService)
    {
        _stationMonitorService = stationMonitorService;
        _stationService = stationService;
        _stepService = stepService;
        _variableService = variableService;
        _stationStepService = stationStepService;
    }

    #region Station
    [HttpPost("stationAdd")]
    public async Task<ResponseDto> AddOneStationAsync(StationDto station)
    {
        return await _stationService.AddOneStationAsync(station);
    }

    [HttpPost("stationsAdd")]
    public async Task<ResponseDto> AddStationsAsync(List<StationDto> stations)
    {
        return await _stationService.AddStationsAsync(stations);
    }

    [HttpPost("stationAllDel")]
    public async Task<ResponseDto> DelAllStationsAsync()
    {
        return await _stationService.DelAllStationsAsync();
    }

    [HttpPost("stationDel")]
    public async Task<ResponseDto> DelOneStationAsync(string stationCode)
    {
        return await _stationService.DelOneStationAsync(stationCode);
    }

    [HttpGet("allStations")]
    public async Task<List<StationDto>> GetAllStationsAsync()
    {
        return await _stationService.GetAllStationsAsync();
    }

    [HttpGet("stationInfo")]
    public async Task<List<StationInfoDto>> GetInformationsAsync(string stationCode)
    {
        return await _stationMonitorService.GetInformationsAsync(stationCode).ConfigureAwait(false);
    }
    #endregion


    #region Step
    [HttpGet("step/allSteps")]
    public async Task<List<StepDto>> GetAllStepsAsync()
    {
        return await _stepService.GetAllStepsAsync().ConfigureAwait(false);
    }

    [HttpPost("step/stepAdd")]
    public async Task<ResponseDto> AddStepAsync(StepDto step)
    {
        return await _stepService.AddStepAsync(step).ConfigureAwait(false);
    }

    [HttpPost("step/stepsAdd")]
    public async Task<ResponseDto> AddStepsAsync(List<StepDto> steps)
    {
        return await _stepService.AddStepsAsync(steps).ConfigureAwait(false);
    }

    [HttpPost("step/stepDel")]
    public async Task<ResponseDto> DelStepAsync(string stepClsName)
    {
        return await _stepService.DelStepAsync(stepClsName).ConfigureAwait(false);
    }

    [HttpPost("step/stepAllDel")]
    public async Task<ResponseDto> DelAllStepAsync()
    {
        return await _stepService.DelAllStepAsync().ConfigureAwait(false);
    }

    [HttpPost("step/paraAdd")]
    public async Task<ResponseDto> AddStepParaAsync(StepParaDto para)
    {
        return await _stepService.AddStepParaAsync(para).ConfigureAwait(false);
    }

    [HttpPost("step/parasAdd")]
    public async Task<ResponseDto> AddStepParasAsync(List<StepParaDto> paras)
    {
        return await _stepService.AddStepParasAsync(paras).ConfigureAwait(false);
    }

    [HttpPost("step/paraDel")]
    public async Task<ResponseDto> DelOneStepParaAsync(DelStepParaDto para)
    {
        return await _stepService.DelOneStepParaAsync(para).ConfigureAwait(false);
    }

    [HttpPost("step/parasOfStepDel")]
    public async Task<ResponseDto> DelParasOfStepAsync(string stepClsName)
    {
        return await _stepService.DelParasOfStepAsync(stepClsName).ConfigureAwait(false);
    }

    [HttpPost("step/allParasDel")]
    public async Task<ResponseDto> DelAllParasAsync()
    {
        return await _stepService.DelAllParasAsync().ConfigureAwait(false);
    }

    [HttpPost("step/parasOfStep")]
    public async Task<List<StepParaDto>> GetStepParasOfStepAsync(string stepClsName)
    {
        return await _stepService.GetStepParasOfStepAsync(stepClsName).ConfigureAwait(false);
    }

    [HttpPost("step/allParas")]
    public async Task<List<StepParaDto>> GetAllStepParasAsync()
    {
        return await _stepService.GetAllStepParasAsync().ConfigureAwait(false);
    }
    #endregion


    #region Variable
    [HttpPost("variable/variableAdd")]
    public async Task<ResponseDto> AddOneVariableAsync(VariableDto v)
    {
        return await _variableService.AddOneVariableAsync(v).ConfigureAwait(false);
    }

    [HttpPost("variable/variablesAdd")]
    public async Task<ResponseDto> AddVariablesAsync(List<VariableDto> vs)
    {
        return await _variableService.AddVariablesAsync(vs).ConfigureAwait(false);
    }

    [HttpPost("variable/variableDel")]
    public async Task<ResponseDto> DelOneVariableAsync(string variableName)
    {
        return await _variableService.DelOneVariableAsync(variableName).ConfigureAwait(false);
    }

    [HttpPost("variable/allVariableDel")]
    public async Task<ResponseDto> DelAllVariablesAsync()
    {
        return await _variableService.DelAllVariablesAsync().ConfigureAwait(false);
    }

    [HttpPost("variable/variableValueUpdate")]
    public async Task<ResponseDto> UpdateVariableValueAsync(UpdateVariableDto para)
    {
        return await _variableService.UpdateVariableValueAsync(para).ConfigureAwait(false);
    }

    [HttpGet("variable/variableGet")]
    public async Task<VariableDto> GetVariableAsync(string vName)
    {
        return await _variableService.GetVariableAsync(vName).ConfigureAwait(false);
    }

    [HttpGet("variable/allVariableGet")]
    public async Task<List<VariableDto>> GetAllVariablesAsync()
    {
        return await _variableService.GetAllVariablesAsync().ConfigureAwait(false);
    }
    #endregion

    #region StationStep
    [HttpPost("stationStep/stationStepAdd")]
    public async Task<ResponseDto> AddOneStationStepAsync(StationStepDto step)
    {
        return await _stationStepService.AddOneStationStepAsync(step).ConfigureAwait(false);
    }

    [HttpPost("stationStep/stationStepsOfStationAdd")]
    public async Task<ResponseDto> AddAllStationStepsOfStationAsync(List<StationStepDto> steps)
    {
        return await _stationStepService.AddAllStationStepsOfStationAsync(steps).ConfigureAwait(false);
    }

    [HttpPost("stationStep/stationStepDel")]
    public async Task<ResponseDto> DelOneStationStepAsync(DelOneStationStepDto para)
    {
        return await _stationStepService.DelOneStationStepAsync(para).ConfigureAwait(false);
    }

    [HttpPost("stationStep/stationStepsOfStationDel")]
    public async Task<ResponseDto> DelAllStationStepsOfStationAsync(string stationCode)
    {
        return await _stationStepService.DelAllStationStepsOfStationAsync(stationCode).ConfigureAwait(false);
    }

    [HttpPost("stationStep/allStationStepsDel")]
    public async Task<ResponseDto> DelAllStationStepsAsync()
    {
        return await _stationStepService.DelAllStationStepsAsync().ConfigureAwait(false);
    }

    [HttpGet("stationStep/stationStepGet")]
    public async Task<StationStepDto> GetOneStationStepAsync(GetOneStationStepDto para)
    {
        return await _stationStepService.GetOneStationStepAsync(para).ConfigureAwait(false);
    }

    [HttpGet("stationStep/stationStepsOfStationGet")]
    public async Task<List<StationStepDto>> GetAllStationStepsOfStationAsync(string stationCode)
    {
        return await _stationStepService.GetAllStationStepsOfStationAsync(stationCode).ConfigureAwait(false);
    }

    [HttpGet("stationStep/allStationStepsGet")]
    public async Task<List<StationStepDto>> GetAllStationStepsAsync()
    {
        return await _stationStepService.GetAllStationStepsAsync().ConfigureAwait(false);
    }

    [HttpPost("stationStep/paraLinkAdd")]
    public async Task<ResponseDto> AddOneParaLinkAsync(StationStepParaLinkDto link)
    {
        return await _stationStepService.AddOneParaLinkAsync(link).ConfigureAwait(false);
    }

    [HttpPost("stationStep/paraLinksAdd")]
    public async Task<ResponseDto> AddParaLinksAsync(List<StationStepParaLinkDto> links)
    {
        return await _stationStepService.AddParaLinksAsync(links).ConfigureAwait(false);
    }

    [HttpPost("stationStep/paraLinkDel")]
    public async Task<ResponseDto> DelOneParaLinkAsync(DelOneParaLinkDto para)
    {
        return await _stationStepService.DelOneParaLinkAsync(para).ConfigureAwait(false);
    }

    [HttpPost("stationStep/paraLinksOfStationStepDel")]
    public async Task<ResponseDto> DelParaLinksOfStationStepAsync(DelParaLinksOfStationStepDto para)
    {
        return await _stationStepService.DelParaLinksOfStationStepAsync(para).ConfigureAwait(false);
    }

    [HttpPost("stationStep/paraLinksOfStationDel")]
    public async Task<ResponseDto> DelParaLinksOfStationAsync(string stationCode)
    {
        return await _stationStepService.DelParaLinksOfStationAsync(stationCode).ConfigureAwait(false);
    }

    [HttpPost("stationStep/allParaLinksDel")]
    public async Task<ResponseDto> DelAllParaLinksAsync()
    {
        return await _stationStepService.DelAllParaLinksAsync().ConfigureAwait(false);
    }

    [HttpGet("stationStep/paraLinkGet")]
    public async Task<StationStepParaLinkDto> GetOneParaLinkAsync(GetOneParaLinkDto para)
    {
        return await _stationStepService.GetOneParaLinkAsync(para).ConfigureAwait(false);
    }

    [HttpGet("stationStep/paraLinksOfStationStepGet")]
    public async Task<List<StationStepParaLinkDto>> GetParaLinksOfStationStepAsync(GetParaLinksOfStationStepDto para)
    {
        return await _stationStepService.GetParaLinksOfStationStepAsync(para).ConfigureAwait(false);
    }

    [HttpGet("stationStep/paraLinksOfStationGet")]
    public async Task<List<StationStepParaLinkDto>> GetParaLinksOfStationAsync(string stationCode)
    {
        return await _stationStepService.GetParaLinksOfStationAsync(stationCode).ConfigureAwait(false);
    }

    [HttpGet("stationStep/allParaLinksGet")]
    public async Task<List<StationStepParaLinkDto>> GetAllParaLinksAsync()
    {
        return await _stationStepService.GetAllParaLinksAsync().ConfigureAwait(false);
    }
    #endregion
}