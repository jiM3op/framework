using Signum.Engine.Authorization;
using Signum.Engine.Processes;
using Signum.Entities.Processes;
using Signum.React.ApiControllers;
using Signum.React.Facades;
using Signum.React.Filters;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Signum.React.Processes;

[ValidateModelFilter]
public class ProcessController : ControllerBase
{
    [HttpPost("api/processes/constructFromMany")]
    public EntityPackTS ConstructFromMany([Required, FromBody]MultiOperationRequest request)
    {
        var type = request.Type == null ? null : TypeLogic.GetType(request.Type);

        var op = request.GetOperationSymbol(type!);
        var entity = PackageLogic.CreatePackageOperation(request.Lites, op, request.ParseArgs(op));

        return SignumServer.GetEntityPack(entity);
    }

    [HttpGet("api/processes/view")]
    public ProcessLogicState View()
    {
        ProcessLogicState state = ProcessRunnerLogic.ExecutionState();

        return state;
    }

    [HttpPost("api/processes/start")]
    public void Start()
    {
        ProcessPermission.ViewProcessPanel.AssertAuthorized();

        ProcessRunnerLogic.StartRunningProcesses();

        Thread.Sleep(1000);
    }

    [HttpPost("api/processes/stop")]
    public void Stop()
    {
        ProcessPermission.ViewProcessPanel.AssertAuthorized();

        ProcessRunnerLogic.Stop();

        Thread.Sleep(1000);
    }
}
