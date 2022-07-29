using Signum.Entities.UserQueries;
using Signum.React.UserAssets;
using Signum.React.Facades;
using Signum.Engine.UserQueries;
using Signum.Engine.Authorization;
using Microsoft.AspNetCore.Builder;
using Signum.React.ApiControllers;

namespace Signum.React.UserQueries;

public static class UserQueryServer
{
    public static void Start(IApplicationBuilder app)
    {
        UserAssetServer.Start(app);

        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

        SignumServer.WebEntityJsonConverterFactory.AfterDeserilization.Register((UserQueryEntity uq) =>
        {
            if (uq.Query != null)
            {
                var qd = QueryLogic.Queries.QueryDescription(uq.Query.ToQueryName());
                uq.ParseData(qd);
            }
        });

        EntityPackTS.AddExtension += ep =>
        {
            if (ep.entity.IsNew || !UserQueryPermission.ViewUserQuery.IsAuthorized())
                return;

            var userQueries = UserQueryLogic.GetUserQueriesEntity(ep.entity.GetType());
            if (userQueries.Any())
                ep.extension.Add("userQueries", userQueries);
        };
    }
}
