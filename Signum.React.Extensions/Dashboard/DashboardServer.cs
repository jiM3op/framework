using Signum.React.UserAssets;
using Signum.React.Facades;
using Signum.Engine.Dashboard;
using Signum.Entities.Dashboard;
using Signum.Engine.Authorization;
using Microsoft.AspNetCore.Builder;
using Signum.React.ApiControllers;

namespace Signum.React.Dashboard;

public static class DashboardServer
{
    public static void Start(IApplicationBuilder app)
    {
        UserAssetServer.Start(app);

        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

        EntityPackTS.AddExtension += ep =>
        {
            if (ep.entity.IsNew || !DashboardPermission.ViewDashboard.IsAuthorized())
                return;

            var dashboards = DashboardLogic.GetDashboardsEntity(ep.entity.GetType());
            if (dashboards.Any())
                ep.extension.Add("dashboards", dashboards);

            var result = DashboardLogic.GetEmbeddedDashboards(ep.entity.GetType());
            if (result != null)
                ep.extension.Add("embeddedDashboards", result);
        };

        SignumServer.WebEntityJsonConverterFactory.AfterDeserilization.Register((DashboardEntity uq) =>
        {
            uq.ParseData(q => QueryLogic.Queries.QueryDescription(q.ToQueryName()));
        });
    }
}
