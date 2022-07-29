using Signum.Entities.DynamicQuery;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signum.React.ApiControllers;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.

public class SubTokensRequest
{
    public string queryKey;
    public string? token;
    public SubTokensOptions options;
}
public class TokenRequest
{
    public string token;
    public SubTokensOptions options;

    public override string ToString() => $"{token} ({options})";
}

public class ParseTokensRequest
{
    public string queryKey;
    public List<TokenRequest> tokens;
}

public class QueryDescriptionTS
{
    public string queryKey;
    public Dictionary<string, ColumnDescriptionTS> columns;



    [JsonExtensionData]
    public Dictionary<string, object> Extension { get; set; } = new Dictionary<string, object>();

    public static Action<QueryDescriptionTS> AddExtension;
}

public class ColumnDescriptionTS
{
    public string name;
    public TypeReferenceTS type;
    public string typeColor;
    public string niceTypeName;
    public FilterType? filterType;
    public string? unit;
    public string? format;
    public string displayName;
    public bool isGroupable;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool hasOrderAdapter;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool preferEquals;
    public string? propertyRoute;

   
}

public class QueryTokenTS
{
    public QueryTokenTS() { }
    

    public string toStr;
    public string niceName;
    public string key;
    public string fullKey;
    public string typeColor;
    public string niceTypeName;
    public QueryTokenType? queryTokenType;
    public TypeReferenceTS type;
    public FilterType? filterType;
    public string? format;
    public string? unit;
    public bool isGroupable;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool hasOrderAdapter;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool preferEquals;
    public QueryTokenTS? parent;
    public string? propertyRoute;
}

public enum QueryTokenType
{
    Aggregate,
    Element,
    AnyOrAll,
}
