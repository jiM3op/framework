using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;
using Signum.React.Facades;
using Signum.Entities.Basics;
using Signum.React.Filters;
using System.Collections.ObjectModel;
using Signum.Engine.Maps;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Text.Json;
using Signum.Entities.Json;
using Signum.Engine.Json;

namespace Signum.React.ApiControllers;


[ValidateModelFilter]
public class QueryController : ControllerBase
{
    [HttpGet("api/query/findLiteLike"), ProfilerActionSplitter("types")]
    public async Task<List<Lite<Entity>>> FindLiteLike(string types, string subString, int count, CancellationToken token)
    {
        Implementations implementations = ParseImplementations(types);

        return await AutocompleteUtils.FindLiteLikeAsync(implementations, subString, count, token);
    }

    [HttpGet("api/query/allLites"), ProfilerActionSplitter("types")]
    public async Task<List<Lite<Entity>>> FetchAllLites(string types, CancellationToken token)
    {
        Implementations implementations = ParseImplementations(types);

        return await AutocompleteUtils.FindAllLiteAsync(implementations, token);
    }

    private static Implementations ParseImplementations(string types)
    {
        return Implementations.By(types.Split(',').Select(a => TypeLogic.GetType(a.Trim())).ToArray());
    }

    [HttpGet("api/query/description/{queryKey}"), ProfilerActionSplitter("queryKey")]
    public QueryDescriptionTS GetQueryDescription(string queryKey)
    {
        var qn = QueryLogic.ToQueryName(queryKey);
        return QueryServer.ToQueryDescriptionTS(QueryLogic.Queries.QueryDescription(qn));
    }

    [HttpGet("api/query/queryEntity/{queryKey}"), ProfilerActionSplitter("queryKey")]
    public QueryEntity GetQueryEntity(string queryKey)
    {
        var qn = QueryLogic.ToQueryName(queryKey);
        return QueryLogic.GetQueryEntity(qn);
    }

    [HttpPost("api/query/parseTokens")]
    public List<QueryTokenTS> ParseTokens([Required, FromBody]ParseTokensRequest request)
    {
        var qn = QueryLogic.ToQueryName(request.queryKey);
        var qd = QueryLogic.Queries.QueryDescription(qn);

        var tokens = request.tokens.Select(tr => QueryUtils.Parse(tr.token, qd, tr.options)).ToList();

        return tokens.Select(qt => qt.ToQueryTokenTS(recursive: true)).ToList();
    }



    [HttpPost("api/query/subTokens")]
    public List<QueryTokenTS> SubTokens([Required, FromBody]SubTokensRequest request)
    {
        var qn = QueryLogic.ToQueryName(request.queryKey);
        var qd = QueryLogic.Queries.QueryDescription(qn);

        var token = request.token == null ? null: QueryUtils.Parse(request.token, qd, request.options);


        var tokens = QueryUtils.SubTokens(token, qd, request.options);

        return tokens.Select(qt => qt.ToQueryTokenTS(recursive: false)).ToList();
    }



    [HttpPost("api/query/executeQuery"), ProfilerActionSplitter]
    public async Task<ResultTable> ExecuteQuery([Required, FromBody]QueryRequestTS request, CancellationToken token)
    {
        var result = await QueryLogic.Queries.ExecuteQueryAsync(request.ToQueryRequest(SignumServer.JsonSerializerOptions, this.HttpContext.Request.Headers.Referer), token);
        return result;
    }

    [HttpPost("api/query/lites"), ProfilerActionSplitter]
    public async Task<List<Lite<Entity>>> GetLites([Required, FromBody]QueryEntitiesRequestTS request, CancellationToken token)
    {
        return await QueryLogic.Queries.GetEntitiesLite(request.ToQueryEntitiesRequest(SignumServer.JsonSerializerOptions)).ToListAsync(token);
    }

    [HttpPost("api/query/entities"), ProfilerActionSplitter]
    public async Task<List<Entity>> GetEntities([Required, FromBody]QueryEntitiesRequestTS request, CancellationToken token)
    {
        return await QueryLogic.Queries.GetEntitiesFull(request.ToQueryEntitiesRequest(SignumServer.JsonSerializerOptions)).ToListAsync(token);
    }

    [HttpPost("api/query/queryValue"), ProfilerActionSplitter]
    public async Task<object?> QueryValue([Required, FromBody]QueryValueRequestTS request, CancellationToken token)
    {
        return await QueryLogic.Queries.ExecuteQueryValueAsync(request.ToQueryValueRequest(SignumServer.JsonSerializerOptions), token);
    }
}

public static class QueryControllerExtensions
{
    public static QueryRequestTS ToQueryRequestTS(this QueryRequest qr)
    {
        return new QueryRequestTS
        {
            queryKey = QueryUtils.GetKey(qr.QueryName),
            groupResults = qr.GroupResults,
            columns = qr.Columns.Select(c => new ColumnTS { token = c.Token.FullKey(), displayName = c.DisplayName }).ToList(),
            filters = qr.Filters.Select(f => FilterTS.FromFilter(f)).ToList(),
            orders = qr.Orders.Select(o => new OrderTS { orderType = o.OrderType, token = o.Token.FullKey() }).ToList(),
            pagination = new PaginationTS(qr.Pagination),
            systemTime = qr.SystemTime == null ? null : new SystemTimeTS(qr.SystemTime),
        };
    }

    public static QueryRequest ToQueryRequest(this QueryRequestTS request, JsonSerializerOptions jsonSerializerOptions, string referrerUrl)
    {
        var qn = QueryLogic.ToQueryName(request.queryKey);
        var qd = QueryLogic.Queries.QueryDescription(qn);
        var groupResults = request.groupResults;
        return new QueryRequest
        {
            QueryUrl = referrerUrl,
            QueryName = qn,
            GroupResults = groupResults,
            Filters = request.filters.EmptyIfNull().Select(f => f.ToFilter(qd, canAggregate: groupResults, jsonSerializerOptions)).ToList(),
            Orders = request.orders.EmptyIfNull().Select(f => f.ToOrder(qd, canAggregate: groupResults)).ToList(),
            Columns = request.columns.EmptyIfNull().Select(f => f.ToColumn(qd, canAggregate: groupResults)).ToList(),
            Pagination = request.pagination.ToPagination(),
            SystemTime = request.systemTime?.ToSystemTime(),
        };
    }

    public static QueryEntitiesRequest ToQueryEntitiesRequest(this QueryEntitiesRequestTS request, JsonSerializerOptions jsonSerializerOptions)
    {
        var qn = QueryLogic.ToQueryName(request.queryKey);
        var qd = QueryLogic.Queries.QueryDescription(qn);
        return new QueryEntitiesRequest
        {
            QueryName = qn,
            Count = request.count,
            Filters = request.filters.EmptyIfNull().Select(f => f.ToFilter(qd, canAggregate: false, jsonSerializerOptions)).ToList(),
            Orders = request.orders.EmptyIfNull().Select(f => f.ToOrder(qd, canAggregate: false)).ToList(),
        };
    }

    public static QueryValueRequest ToQueryValueRequest(this QueryValueRequestTS request, JsonSerializerOptions jsonSerializerOptions)
    {
        var qn = QueryLogic.ToQueryName(request.querykey);
        var qd = QueryLogic.Queries.QueryDescription(qn);

        var value = request.valueToken.HasText() ? QueryUtils.Parse(request.valueToken, qd, SubTokensOptions.CanAggregate | SubTokensOptions.CanElement) : null;

        return new QueryValueRequest
        {
            QueryName = qn,
            MultipleValues = request.multipleValues ?? false,
            Filters = request.filters.EmptyIfNull().Select(f => f.ToFilter(qd, canAggregate: false, jsonSerializerOptions)).ToList(),
            ValueToken = value,
            SystemTime = request.systemTime?.ToSystemTime(),
        };
    }



}



