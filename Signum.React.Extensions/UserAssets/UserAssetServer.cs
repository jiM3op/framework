using Signum.Entities.UserAssets;
using Signum.React.ApiControllers;
using Signum.React.Facades;
using Microsoft.AspNetCore.Builder;
using Signum.Entities.UserQueries;
using Signum.Engine.Authorization;
using Signum.Entities.Chart;
using System.Text.Json;
using Signum.Engine.Json;
using Signum.Entities.Json;

namespace Signum.React.UserAssets;

public static class UserAssetServer
{
    static bool started;
    public static void Start(IApplicationBuilder app)
    {
        if (started)
            return;

        started = true;

        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
        ReflectionServer.RegisterLike(typeof(QueryTokenEmbedded), () => UserAssetPermission.UserAssetsToXML.IsAuthorized() ||
        TypeAuthLogic.GetAllowed(typeof(UserQueryEntity)).MaxUI() > Entities.Authorization.TypeAllowedBasic.None ||
        TypeAuthLogic.GetAllowed(typeof(UserChartEntity)).MaxUI() > Entities.Authorization.TypeAllowedBasic.None
        );
        //EntityJsonConverter.DefaultPropertyRoutes.Add(typeof(QueryFilterEmbedded), PropertyRoute.Construct((UserQueryEntity e) => e.Filters.FirstEx()));
        //EntityJsonConverter.DefaultPropertyRoutes.Add(typeof(PinnedQueryFilterEmbedded), PropertyRoute.Construct((UserQueryEntity e) => e.Filters.FirstEx().Pinned));

        var pcs = SignumServer.WebEntityJsonConverterFactory.GetPropertyConverters(typeof(QueryTokenEmbedded));
        pcs.Add("token", new PropertyConverter
        {
            CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) =>
            {
                var qte = (QueryTokenEmbedded)ctx.Entity;

                writer.WritePropertyName(ctx.LowerCaseName);
                JsonSerializer.Serialize(writer, qte.TryToken?.ToQueryTokenTS( true), ctx.JsonSerializerOptions);
            },
            AvoidValidate = true,
            CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
            {
                var result = JsonSerializer.Deserialize<object>(ref reader, ctx.JsonSerializerOptions);
                //Discard
            }
        });
        pcs.Add("parseException", new PropertyConverter
        {
            CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) =>
            {
                var qte = (QueryTokenEmbedded)ctx.Entity;

                writer.WritePropertyName(ctx.LowerCaseName);
                writer.WriteStringValue(qte.ParseException?.Message);
            },
            AvoidValidate = true,
            CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
            {
                var result = reader.GetString();
                //Discard
            }
        });
        pcs.GetOrThrow("tokenString").CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) =>
        {
            var qte = (QueryTokenEmbedded)ctx.Entity;

            writer.WritePropertyName(ctx.LowerCaseName);
            writer.WriteStringValue(qte.TryToken?.FullKey() ?? qte.TokenString);
        };
    }
}
