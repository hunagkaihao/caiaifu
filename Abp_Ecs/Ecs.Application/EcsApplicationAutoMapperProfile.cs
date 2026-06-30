using AutoMapper;
using Ecs.AgvPlc;
using Ecs.AgvPlcTcp;
using Ecs.Log;
using Ecs.LogTool;
using Ecs.PlcMonitor;
using Ecs.Stations;
using Ecs.WorkPositions;

namespace Ecs;

public class EcsApplicationAutoMapperProfile : Profile
{
    public EcsApplicationAutoMapperProfile()
    {
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */
         CreateMap<MonitorValue, MonitorDto>();
         CreateMap<AgvTransportTask, AgvTransportTaskDto>();
         CreateMap<StationInfo, StationInfoDto>();
         CreateMap<SqliteLogItem, LogDto>();
         CreateMap<StationDto, Station>();
         CreateMap<Station, StationDto>();
         CreateMap<Step, StepDto>();
         CreateMap<StepDto, Step>();
         CreateMap<StepPara, StepParaDto>();
         CreateMap<StepParaDto, StepPara>();
         CreateMap<Variable, VariableDto>();
         CreateMap<VariableDto, Variable>();
         CreateMap<StationStep, StationStepDto>();
         CreateMap<StationStepDto, StationStep>();
         CreateMap<StationStepParaLink, StationStepParaLinkDto>();
         CreateMap<StationStepParaLinkDto, StationStepParaLink>();
         CreateMap<WorkPosition, WorkPositionDto>();
         CreateMap<WorkPositionDto, WorkPosition>();
    }
}
