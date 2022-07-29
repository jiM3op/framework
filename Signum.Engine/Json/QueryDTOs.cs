//using Signum.Engine.Basics;
using Signum.Entities.DynamicQuery;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Signum.Engine.Json;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
public class FilterJsonConverter : JsonConverter<FilterTS>
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(FilterTS).IsAssignableFrom(objectType);
    }

    public override void Write(Utf8JsonWriter writer, FilterTS value, JsonSerializerOptions options)
    {
        if (value is FilterConditionTS fc)
            JsonSerializer.Serialize(writer, fc, options);
        else if (value is FilterGroupTS fg)
            JsonSerializer.Serialize(writer, fg, options);
        else
            throw new UnexpectedValueException(value);
    }

    public override FilterTS? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (var doc = JsonDocument.ParseValue(ref reader))
        {
            var elem = doc.RootElement;

            if (elem.TryGetProperty("operation", out var oper))
            {
                return new FilterConditionTS
                {
                    token = elem.GetProperty("token").GetString()!,
                    operation = oper.GetString()!.ToEnum<FilterOperation>(),
                    value = elem.TryGetProperty("value", out var val) ? val.ToObject<object>(options) : null,
                };
            }

            if (elem.TryGetProperty("groupOperation", out var groupOper))
                return new FilterGroupTS
                {
                    groupOperation = groupOper.GetString()!.ToEnum<FilterGroupOperation>(),
                    token = elem.TryGetProperty("token", out var token) ? token.GetString() : null,
                    filters = elem.GetProperty("filters").EnumerateArray().Select(a => a.ToObject<FilterTS>()!).ToList()
                };

            throw new InvalidOperationException("Impossible to determine type of filter");
        }
    }
}

[JsonConverter(typeof(FilterJsonConverter))]
public abstract class FilterTS
{
    public abstract Filter ToFilter(QueryDescription qd, bool canAggregate, JsonSerializerOptions jsonSerializerOptions);

    public static FilterTS FromFilter(Filter filter)
    {
        if (filter is FilterCondition fc)
            return new FilterConditionTS
            {
                token = fc.Token.FullKey(),
                operation = fc.Operation,
                value = fc.Value
            };

        if (filter is FilterGroup fg)
            return new FilterGroupTS
            {
                token = fg.Token?.FullKey(),
                groupOperation = fg.GroupOperation,
                filters = fg.Filters.Select(f => FromFilter(f)).ToList(),
            };

        throw new UnexpectedValueException(filter);
    }
}

public class FilterConditionTS : FilterTS
{
    public string token;
    public FilterOperation operation;
    public object? value;

    public override Filter ToFilter(QueryDescription qd, bool canAggregate, JsonSerializerOptions jsonSerializerOptions)
    {
        var options = SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | (canAggregate ? SubTokensOptions.CanAggregate : 0);
        var parsedToken = QueryUtils.Parse(token, qd, options);
        var expectedValueType = operation.IsList() ? typeof(List<>).MakeGenericType(parsedToken.Type.Nullify()) : parsedToken.Type;

        var val = value is JsonElement jtok ?
             jtok.ToObject(expectedValueType, jsonSerializerOptions) :
             value;

        if (val is DateTime dt)
            val = dt.FromUserInterface();
        else if (val is ObservableCollection<DateTime?> col)
            val = col.Select(dt => dt?.FromUserInterface()).ToObservableCollection();

        return new FilterCondition(parsedToken, operation, val);
    }

    public override string ToString() => $"{token} {operation} {value}";
}

public class FilterGroupTS : FilterTS
{
    public FilterGroupOperation groupOperation;
    public string? token;
    public List<FilterTS> filters;

    public override Filter ToFilter(QueryDescription qd, bool canAggregate, JsonSerializerOptions jsonSerializerOptions)
    {
        var options = SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | (canAggregate ? SubTokensOptions.CanAggregate : 0);
        var parsedToken = token == null ? null : QueryUtils.Parse(token, qd, options);

        var parsedFilters = filters.Select(f => f.ToFilter(qd, canAggregate, jsonSerializerOptions)).ToList();

        return new FilterGroup(groupOperation, parsedToken, parsedFilters);
    }
}

public class ColumnTS
{
    public string token;
    public string? displayName;

    public Column ToColumn(QueryDescription qd, bool canAggregate)
    {
        var queryToken = QueryUtils.Parse(token, qd, SubTokensOptions.CanElement | (canAggregate ? SubTokensOptions.CanAggregate : 0));

        return new Column(queryToken, displayName ?? queryToken.NiceName());
    }

    public override string ToString() => $"{token} '{displayName}'";

}

public class PaginationTS
{
    public PaginationMode mode;
    public int? elementsPerPage;
    public int? currentPage;

    public PaginationTS() { }

    public PaginationTS(Pagination pagination)
    {
        this.mode = pagination.GetMode();
        this.elementsPerPage = pagination.GetElementsPerPage();
        this.currentPage = (pagination as Pagination.Paginate)?.CurrentPage;
    }

    public override string ToString() => $"{mode} {elementsPerPage} {currentPage}";


    public Pagination ToPagination()
    {
        return mode switch
        {
            PaginationMode.All => new Pagination.All(),
            PaginationMode.Firsts => new Pagination.Firsts(this.elementsPerPage!.Value),
            PaginationMode.Paginate => new Pagination.Paginate(this.elementsPerPage!.Value, this.currentPage!.Value),
            _ => throw new InvalidOperationException($"Unexpected {mode}"),
        };
    }
}

public class SystemTimeTS
{
    public SystemTimeMode mode;
    public SystemTimeJoinMode? joinMode;
    public DateTimeOffset? startDate;
    public DateTimeOffset? endDate;

    public SystemTimeTS() { }

    public SystemTimeTS(SystemTime systemTime)
    {
        if (systemTime is SystemTime.AsOf asOf)
        {
            mode = SystemTimeMode.AsOf;
            startDate = asOf.DateTime;
        }
        else if (systemTime is SystemTime.Between between)
        {
            mode = SystemTimeMode.Between;
            joinMode = ToSystemTimeJoinMode(between.JoinBehaviour);
            startDate = between.StartDateTime;
            endDate = between.EndtDateTime;
        }
        else if (systemTime is SystemTime.ContainedIn containedIn)
        {
            mode = SystemTimeMode.ContainedIn;
            joinMode = ToSystemTimeJoinMode(containedIn.JoinBehaviour);
            startDate = containedIn.StartDateTime;
            endDate = containedIn.EndtDateTime;
        }
        else if (systemTime is SystemTime.All all)
        {
            mode = SystemTimeMode.All;
            joinMode = ToSystemTimeJoinMode(all.JoinBehaviour);
            startDate = null;
            endDate = null;
        }
        else
            throw new InvalidOperationException("Unexpected System Time");
    }

    public override string ToString() => $"{mode} {startDate} {endDate}";


    public SystemTime ToSystemTime()
    {
        return mode switch
        {
            SystemTimeMode.AsOf => new SystemTime.AsOf(startDate!.Value),
            SystemTimeMode.Between => new SystemTime.Between(startDate!.Value, endDate!.Value, ToJoinBehaviour(joinMode!.Value)),
            SystemTimeMode.ContainedIn => new SystemTime.ContainedIn(startDate!.Value, endDate!.Value, ToJoinBehaviour(joinMode!.Value)),
            SystemTimeMode.All => new SystemTime.All(ToJoinBehaviour(joinMode!.Value)),
            _ => throw new InvalidOperationException($"Unexpected {mode}"),
        };
    }

    public static JoinBehaviour ToJoinBehaviour(SystemTimeJoinMode joinMode)
    {
        return joinMode switch
        {
            SystemTimeJoinMode.Current => JoinBehaviour.Current,
            SystemTimeJoinMode.FirstCompatible => JoinBehaviour.FirstCompatible,
            SystemTimeJoinMode.AllCompatible => JoinBehaviour.AllCompatible,
            _ => throw new UnexpectedValueException(joinMode),
        };
    }

    public static SystemTimeJoinMode ToSystemTimeJoinMode(JoinBehaviour joinBehaviour)
    {
        return joinBehaviour switch
        {
            JoinBehaviour.Current => SystemTimeJoinMode.Current,
            JoinBehaviour.FirstCompatible => SystemTimeJoinMode.FirstCompatible,
            JoinBehaviour.AllCompatible => SystemTimeJoinMode.AllCompatible,
            _ => throw new UnexpectedValueException(joinBehaviour),
        };
    }
}

public class QueryValueRequestTS
{
    public string querykey;
    public List<FilterTS> filters;
    public string valueToken;
    public bool? multipleValues;
    public SystemTimeTS/*?*/ systemTime;

   
    public override string ToString() => querykey;
}

public class QueryRequestTS
{
    public string queryKey;
    public bool groupResults;
    public List<FilterTS> filters;
    public List<OrderTS> orders;
    public List<ColumnTS> columns;
    public PaginationTS pagination;
    public SystemTimeTS? systemTime;

  

    public override string ToString() => queryKey;
}

public class QueryEntitiesRequestTS
{
    public string queryKey;
    public List<FilterTS> filters;
    public List<OrderTS> orders;
    public int? count;

    public override string ToString() => queryKey;

  
}

public class OrderTS
{
    public string token;
    public OrderType orderType;

    public Order ToOrder(QueryDescription qd, bool canAggregate)
    {
        return new Order(QueryUtils.Parse(this.token, qd, SubTokensOptions.CanElement | (canAggregate ? SubTokensOptions.CanAggregate : 0)), orderType);
    }

    public override string ToString() => $"{token} {orderType}";
}
