using Signum.Entities.DynamicQuery;
using System.Text.Json;

namespace Signum.React.ApiControllers;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
public class ConstructOperationRequest : BaseOperationRequest
{
    public string Type { get; set; }
}

public class EntityOperationRequest : BaseOperationRequest
{
    public Entity entity { get; set; }
}

public class LiteOperationRequest : BaseOperationRequest
{
    public Lite<Entity> lite { get; set; }
}

public class BaseOperationRequest
{
    public string OperationKey { get; set; }

    public List<JsonElement>? Args { get; set; }


   

    public override string ToString() => OperationKey;
}


public class MultiOperationRequest : BaseOperationRequest
{
    public string? Type { get; set; }
    public Lite<Entity>[] Lites { get; set; }

    public List<PropertySetter>? Setters { get; set; }
}

public class PropertySetter
{
    public string Property;
    public PropertyOperation? Operation;
    public FilterOperation? FilterOperation;
    public object? Value;
    public string? EntityType;
    public List<PropertySetter>? Predicate;
    public List<PropertySetter>? Setters;
}


public class MultiOperationResponse
{
    public MultiOperationResponse(Dictionary<string, string> errors)
    {
        this.Errors = errors;
    }

    public Dictionary<string, string> Errors { get; set; }
}

public class StateCanExecuteRequest
{
    public string[] OperationKeys { get; set; }
    public Lite<Entity>[] Lites { get; set; }
}

public class StateCanExecuteResponse
{
    public StateCanExecuteResponse(Dictionary<string, string> canExecutes)
    {
        this.CanExecutes = canExecutes;
    }

    public bool AnyReadonly;
    public Dictionary<string, string> CanExecutes { get; set; }
}
