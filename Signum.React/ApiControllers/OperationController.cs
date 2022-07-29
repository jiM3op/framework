using Signum.Engine.Basics;
using Signum.Entities.Reflection;
using Signum.React.Facades;
using Signum.React.Filters;
using System.Text.Json;
using static Signum.React.ApiControllers.OperationController;

namespace Signum.React.ApiControllers;

[ValidateModelFilter]
public class OperationController : Controller
{
    [HttpPost("api/operation/construct"), ValidateModelFilter, ProfilerActionSplitter]
    public EntityPackTS? Construct([Required, FromBody] ConstructOperationRequest request)
    {
        var entityType = TypeLogic.GetType(request.Type);

        var op = request.GetOperationSymbol(entityType);

        var entity = OperationLogic.ServiceConstruct(entityType, op, request.ParseArgs(op));

        return entity == null ? null : SignumServer.GetEntityPack(entity);
    }

    [HttpPost("api/operation/constructFromEntity"), ProfilerActionSplitter]
    public EntityPackTS? ConstructFromEntity([Required, FromBody] EntityOperationRequest request)
    {
        var op = request.GetOperationSymbol(request.entity.GetType());

        var entity = OperationLogic.ServiceConstructFrom(request.entity, op, request.ParseArgs(op));

        return entity == null ? null : SignumServer.GetEntityPack(entity);
    }

    [HttpPost("api/operation/constructFromLite"), ProfilerActionSplitter]
    public EntityPackTS? ConstructFromLite([Required, FromBody] LiteOperationRequest request)
    {
        var op = request.GetOperationSymbol(request.lite.EntityType);
        var entity = OperationLogic.ServiceConstructFromLite(request.lite, op, request.ParseArgs(op));
        return entity == null ? null : SignumServer.GetEntityPack(entity);
    }


    [HttpPost("api/operation/executeEntity"), ProfilerActionSplitter]
    public ActionResult<EntityPackTS> ExecuteEntity([Required, FromBody] EntityOperationRequest request)
    {
        var op = request.GetOperationSymbol(request.entity.GetType());
        Entity entity;
        try
        {

            entity = OperationLogic.ServiceExecute(request.entity, op, request.ParseArgs(op));
        }
        catch (IntegrityCheckException ex)
        {
            GraphExplorer.SetValidationErrors(GraphExplorer.FromRootVirtual(request.entity), ex);
            this.TryValidateModel(request, "request");
            if (this.ModelState.IsValid)
                throw;

            return BadRequest(this.ModelState);
        }

        return SignumServer.GetEntityPack(entity);
    }


    [HttpPost("api/operation/executeLite"), ProfilerActionSplitter]
    public EntityPackTS ExecuteLite([Required, FromBody] LiteOperationRequest request)
    {
        var op = request.GetOperationSymbol(request.lite.EntityType);
        var entity = OperationLogic.ServiceExecuteLite(request.lite, op, request.ParseArgs(op));

        return SignumServer.GetEntityPack(entity);
    }

    [HttpPost("api/operation/deleteEntity"), ProfilerActionSplitter]
    public void DeleteEntity([Required, FromBody] EntityOperationRequest request)
    {
        var op = request.GetOperationSymbol(request.entity.GetType());
        OperationLogic.ServiceDelete(request.entity, op, request.ParseArgs(op));
    }

    [HttpPost("api/operation/deleteLite"), ProfilerActionSplitter]
    public void DeleteLite([Required, FromBody] LiteOperationRequest request)
    {
        var op = request.GetOperationSymbol(request.lite.EntityType);
        OperationLogic.ServiceDelete(request.lite, op, request.ParseArgs(op));
    }




    [HttpPost("api/operation/constructFromMany"), ProfilerActionSplitter]
    public EntityPackTS? ConstructFromMany([Required, FromBody]MultiOperationRequest request)
    {
        var type = request.Lites.Select(l => l.EntityType).Distinct().Only() ?? TypeLogic.GetType(request.Type!);

        var op = request.GetOperationSymbol(type);
        var entity = OperationLogic.ServiceConstructFromMany(request.Lites, type, op, request.ParseArgs(op));

        return entity == null ? null : SignumServer.GetEntityPack(entity);
    }

    [HttpPost("api/operation/constructFromMultiple"), ProfilerActionSplitter]
    public MultiOperationResponse ConstructFromMultiple([Required, FromBody] MultiOperationRequest request)
    {
        if (request.Setters.HasItems())
        {
            var errors = ForeachMultiple(request.Lites, lite =>
            {
                var entity = lite.Retrieve();

                MultiSetter.SetSetters(entity, request.Setters, PropertyRoute.Root(entity.GetType()));

                var op = request.GetOperationSymbol(entity.GetType());

                OperationLogic.ServiceConstructFrom(entity, op, request.ParseArgs(op));
            });

            return new MultiOperationResponse(errors);
        }
        else
        {
            var errors = ForeachMultiple(request.Lites, lite =>
            {
                var op = request.GetOperationSymbol(lite.EntityType);

                OperationLogic.ServiceConstructFromLite(lite, op, request.ParseArgs(op));
            });

            return new MultiOperationResponse(errors);
        }
    }


    [HttpPost("api/operation/executeMultiple"), ProfilerActionSplitter]
    public MultiOperationResponse ExecuteMultiple([Required, FromBody] MultiOperationRequest request)
    {
        if (request.Setters.HasItems())
        {
            var errors = ForeachMultiple(request.Lites, lite =>
            {
                var entity = lite.Retrieve();

                MultiSetter.SetSetters(entity, request.Setters, PropertyRoute.Root(entity.GetType()));
                var op = request.GetOperationSymbol(entity.GetType());
                OperationLogic.ServiceExecute(entity, op, request.ParseArgs(op));
            });

            return new MultiOperationResponse(errors);
        }
        else
        {
            var errors = ForeachMultiple(request.Lites, lite =>
            {
                var op = request.GetOperationSymbol(lite.EntityType);
                OperationLogic.ServiceExecuteLite(lite, op, request.ParseArgs(op));
            });

            return new MultiOperationResponse(errors);
        }
    }


    [HttpPost("api/operation/deleteMultiple"), ProfilerActionSplitter]
    public MultiOperationResponse DeleteMultiple([Required, FromBody] MultiOperationRequest request)
    {
        if (request.Setters.HasItems())
        {
            var errors = ForeachMultiple(request.Lites, lite =>
            {
                var entity = lite.Retrieve();

                MultiSetter.SetSetters(entity, request.Setters, PropertyRoute.Root(entity.GetType()));

                var op = request.GetOperationSymbol(entity.GetType());

                OperationLogic.ServiceDelete(entity, op, request.ParseArgs(op));
            });

            return new MultiOperationResponse(errors);
        }
        else
        {
            var errors = ForeachMultiple(request.Lites, lite =>
            {
                var op = request.GetOperationSymbol(lite.EntityType);
                OperationLogic.ServiceDelete(lite, op, request.ParseArgs(op));
            });

            return new MultiOperationResponse(errors);
        }
    }

    static Dictionary<string, string> ForeachMultiple(IEnumerable<Lite<Entity>> lites, Action<Lite<Entity>> action)
    {
        Dictionary<string, string> errors = new Dictionary<string, string>();
        foreach (var lite in lites.Distinct())
        {
            try
            {
                action(lite);
                errors.Add(lite.Key(), "");
            }
            catch (Exception e)
            {
                e.Data["lite"] = lite;
                e.LogException();
                errors.Add(lite.Key(), e.Message);
            }
        }
        return errors;
    }




    [HttpPost("api/operation/stateCanExecutes"), ValidateModelFilter]
    public StateCanExecuteResponse StateCanExecutes([Required, FromBody]StateCanExecuteRequest request)
    {
        var types = request.Lites.Select(a => a.EntityType).ToHashSet();

        var operationSymbols = request.OperationKeys
            .Select(operationKey => types.Select(t => OperationRequestExtensions.ParseOperationAssert(operationKey, t)).Distinct().SingleEx())
            .ToList();

        var result = OperationLogic.GetContextualCanExecute(request.Lites, operationSymbols);
        var anyReadonly = AnyReadonly.GetInvocationListTyped().Any(f => f(request.Lites));

        return new StateCanExecuteResponse(result.SelectDictionary(a => a.Key, v => v))
        {
            AnyReadonly = anyReadonly
        };
    }


    public static Func<Lite<Entity>[], bool>? AnyReadonly; 
}

public static class OperationRequestExtensions
{
    public static OperationSymbol GetOperationSymbol(this BaseOperationRequest opRequest, Type entityType) => ParseOperationAssert(opRequest.OperationKey, entityType);

    public static OperationSymbol ParseOperationAssert(string operationKey, Type entityType)
    {
        var symbol = SymbolLogic<OperationSymbol>.ToSymbol(operationKey);

        OperationLogic.AssertOperationAllowed(symbol, entityType, inUserInterface: true);

        return symbol;
    }


    public static object?[]? ParseArgs(this BaseOperationRequest opRequest, OperationSymbol op)
    {
        return opRequest.Args?.Select(a => ConvertObject(a, op)).ToArray();
    }


    public static Dictionary<OperationSymbol, Func<JsonElement, object?>> CustomOperationArgsConverters = new Dictionary<OperationSymbol, Func<JsonElement, object?>>();

    public static void RegisterCustomOperationArgsConverter(OperationSymbol operationSymbol, Func<JsonElement, object?> converter)
    {
        Func<JsonElement, object?>? a = CustomOperationArgsConverters.TryGetC(operationSymbol); /*CSBUG*/

        CustomOperationArgsConverters[operationSymbol] = a + converter;
    }

    public static object? ConvertObject(JsonElement token, OperationSymbol? operationSymbol)
    {
        switch (token.ValueKind)
        {
            case JsonValueKind.Undefined: return null;
            case JsonValueKind.String:
                if (token.TryGetDateTime(out var dt))
                    return dt;

                if (token.TryGetDateTimeOffset(out var dto))
                    return dto;

                return token.GetString();
            case JsonValueKind.Number: return token.GetDecimal();
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Null: return null;
            case JsonValueKind.Object:
                {
                    if (token.TryGetProperty("EntityType", out var entityType))
                        return token.ToObject<Lite<Entity>>(SignumServer.JsonSerializerOptions);

                    if (token.TryGetProperty("Type", out var type))
                        return token.ToObject<ModifiableEntity>(SignumServer.JsonSerializerOptions);

                    var conv = operationSymbol == null ? null : CustomOperationArgsConverters.TryGetC(operationSymbol);

                    return conv.GetInvocationListTyped().Select(f => f(token)).NotNull().FirstOrDefault();
                }
            case JsonValueKind.Array:
                var result = token.EnumerateArray().Select(t => ConvertObject(t, operationSymbol)).ToList();
                return result;
            default:
                throw new UnexpectedValueException(token.ValueKind);
        }

    }
}
