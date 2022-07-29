using Microsoft.Data.SqlClient.Server;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.React.ApiControllers;

namespace Signum.React.Facades;

public static class QueryServer
{

    public static QueryDescriptionTS ToQueryDescriptionTS(QueryDescription queryDescription)
    {
        var result = new QueryDescriptionTS
        {
            queryKey = QueryUtils.GetKey(queryDescription.QueryName),
            columns = queryDescription.Columns.ToDictionary(a => a.Name, a => ToColumnDescriptionTS(a, queryDescription.QueryName))
        };

        foreach (var action in QueryDescriptionTS.AddExtension.GetInvocationListTyped())
        {
            action(result);
        }

        return result;
    }
    public static ColumnDescriptionTS ToColumnDescriptionTS(ColumnDescription a, object queryName)
    {
        var token = new ColumnToken(a, queryName);
        return new ColumnDescriptionTS
        {
            name = a.Name,
            type = ReflectionServer.ToTypeReferenceTS(a.Type, a.Implementations),
            filterType = QueryUtils.TryGetFilterType(a.Type),
            typeColor = token.TypeColor,
            niceTypeName = token.NiceTypeName,
            isGroupable = token.IsGroupable,
            hasOrderAdapter = QueryUtils.OrderAdapters.Any(a => a(token) != null),
            preferEquals = token.Type == typeof(string) &&
            token.GetPropertyRoute() is PropertyRoute pr &&
            typeof(Entity).IsAssignableFrom(pr.RootType) &&
            pr.HasSomeIndex(),
            unit = UnitAttribute.GetTranslation(a.Unit),
            format = a.Format,
            displayName = a.DisplayName,
            propertyRoute = token.GetPropertyRoute()?.ToString(),
        };
    }

    public static QueryTokenTS ToQueryTokenTS(this QueryToken qt, bool recursive)
    {
        return new QueryTokenTS
        {

            toStr = qt.ToString(),
            niceName = qt.NiceName(),
            key = qt.Key,
            fullKey = qt.FullKey(),
            type = ReflectionServer.ToTypeReferenceTS(qt.Type, qt.GetImplementations()),
            filterType = QueryUtils.TryGetFilterType(qt.Type),
            format = qt.Format,
            unit = UnitAttribute.GetTranslation(qt.Unit),
            typeColor = qt.TypeColor,
            niceTypeName = qt.NiceTypeName,
            queryTokenType = GetQueryTokenType(qt),
            isGroupable = qt.IsGroupable,
            hasOrderAdapter = QueryUtils.OrderAdapters.Any(a => a(qt) != null),

            preferEquals = qt.Type == typeof(string) &&
            qt.GetPropertyRoute() is PropertyRoute pr &&
            typeof(Entity).IsAssignableFrom(pr.RootType) &&
            Schema.Current.HasSomeIndex(pr),

            propertyRoute = qt.GetPropertyRoute()?.ToString(),
            parent = !recursive ? null : qt.Parent?.ToQueryTokenTS(recursive),

        };
    }

    private static QueryTokenType? GetQueryTokenType(QueryToken qt)
    {
        if (qt is AggregateToken)
            return QueryTokenType.Aggregate;

        if (qt is CollectionElementToken ce)
            return QueryTokenType.Element;

        if (qt is CollectionAnyAllToken caat)
            return QueryTokenType.AnyOrAll;

        return null;
    }
}
