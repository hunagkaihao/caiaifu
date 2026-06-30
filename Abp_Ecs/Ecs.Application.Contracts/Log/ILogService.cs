using System.Collections.Generic;
using Volo.Abp.Application.Services;

namespace Ecs.Log;

public interface ILogService : IApplicationService
{
    public List<LogDto> query(string msgSnip, string grade, int logCntMax = 2000);
}