using Signum.Entities.DynamicQuery;
using Signum.Utilities.Reflection;
using Signum.Engine.Linq;
using Signum.Entities.Basics;
using System.Diagnostics.CodeAnalysis;
using System.Collections;

namespace Signum.Engine.DynamicQuery;

public class DynamicQueryBucket
{
    public ResetLazy<IDynamicQueryCore> Core { get; private set; }

    public object QueryName { get; private set; }

    public Implementations EntityImplementations { get; private set; }

    public DynamicQueryBucket(object queryName, Func<IDynamicQueryCore> lazyQueryCore, Implementations entityImplementations)
    {
        if (lazyQueryCore == null)
            throw new ArgumentNullException(nameof(lazyQueryCore));

        this.QueryName = queryName ?? throw new ArgumentNullException(nameof(queryName));
        this.EntityImplementations = entityImplementations;

        this.Core = new ResetLazy<IDynamicQueryCore>(() =>
        {
            var core = lazyQueryCore();

            core.QueryName = QueryName;

            core.StaticColumns.Where(sc => sc.IsEntity).SingleEx(() => "Entity column on {0}".FormatWith(QueryUtils.GetKey(QueryName)));

            core.EntityColumnFactory().Implementations = entityImplementations;

            var errors = core.StaticColumns.Where(sc => sc.Implementations == null && sc.Type.CleanType().IsIEntity());

            if (errors.Any())
                throw new InvalidOperationException("Column {0} of query '{1}' do(es) not have implementations defined. Use Column extension method".FormatWith(errors.CommaAnd(a => $"'{a.Name}'"), QueryUtils.GetKey(QueryName)));

            return core;
        });
    }


    public QueryDescription GetDescription()
    {
        return Core.Value.GetQueryDescription();
    }
}


public interface IDynamicQueryCore
{
    object QueryName { get; set; }
    ColumnDescriptionFactory[] StaticColumns { get; }
    Expression? Expression { get; }

    ColumnDescriptionFactory EntityColumnFactory();
    QueryDescription GetQueryDescription();

    ResultTable ExecuteQuery(QueryRequest request);
    Task<ResultTable> ExecuteQueryAsync(QueryRequest request, CancellationToken cancellationToken);
    ResultTable ExecuteQueryGroup(QueryRequest request);
    Task<ResultTable> ExecuteQueryGroupAsync(QueryRequest request, CancellationToken cancellationToken);
    object? ExecuteQueryValue(QueryValueRequest request);
    Task<object?> ExecuteQueryValueAsync(QueryValueRequest request, CancellationToken cancellationToken);
    Lite<Entity>? ExecuteUniqueEntity(UniqueEntityRequest request);
    Task<Lite<Entity>?> ExecuteUniqueEntityAsync(UniqueEntityRequest request, CancellationToken cancellationToken);

    IQueryable<Lite<Entity>> GetEntitiesLite(QueryEntitiesRequest request);
    IQueryable<Entity> GetEntitiesFull(QueryEntitiesRequest request);
}


public static class DynamicQueryCore
{
    public static AutoDynamicQueryCore<T> Auto<T>(IQueryable<T> query)
    {
        return new AutoDynamicQueryCore<T>(query);
    }

    public static ManualDynamicQueryCore<T> Manual<T>(Func<QueryRequest, QueryDescription, CancellationToken, Task<DEnumerableCount<T>>> execute)
    {
        return new ManualDynamicQueryCore<T>(execute);
    }

    internal static IDynamicQueryCore FromSelectorUntyped<T>(Expression<Func<T, object?>> expression)
        where T : Entity
    {
        var eType = expression.Parameters.SingleEx().Type;
        var tType = expression.Body.Type;
        var typedSelector = Expression.Lambda(expression.Body, expression.Parameters);

        return giAutoPrivate.GetInvoker(eType, tType)(typedSelector);
    }

    static readonly GenericInvoker<Func<LambdaExpression, IDynamicQueryCore>> giAutoPrivate =
        new(lambda => FromSelector<TypeEntity, object?>((Expression<Func<TypeEntity, object?>>)lambda));
    public static AutoDynamicQueryCore<T> FromSelector<E, T>(Expression<Func<E, T>> selector)
        where E : Entity
    {
        return new AutoDynamicQueryCore<T>(Database.Query<E>().Select(selector));
    }

    public static Dictionary<string, Meta?>? QueryMetadata(IQueryable query)
    {
        return MetadataVisitor.GatherMetadata(query.Expression);
    }

}

public abstract class DynamicQueryCore<T> : IDynamicQueryCore
{
    public object QueryName { get; set; } = null!;

    public ColumnDescriptionFactory[] StaticColumns { get; protected set; } = null!;

    public abstract ResultTable ExecuteQuery(QueryRequest request);
    public abstract Task<ResultTable> ExecuteQueryAsync(QueryRequest request, CancellationToken cancellationToken);

    public abstract ResultTable ExecuteQueryGroup(QueryRequest request);
    public abstract Task<ResultTable> ExecuteQueryGroupAsync(QueryRequest request, CancellationToken cancellationToken);

    public abstract object? ExecuteQueryValue(QueryValueRequest request);
    public abstract Task<object?> ExecuteQueryValueAsync(QueryValueRequest request, CancellationToken cancellationToken);

    public abstract Lite<Entity>? ExecuteUniqueEntity(UniqueEntityRequest request);
    public abstract Task<Lite<Entity>?> ExecuteUniqueEntityAsync(UniqueEntityRequest request, CancellationToken cancellationToken);

    public abstract IQueryable<Lite<Entity>> GetEntitiesLite(QueryEntitiesRequest request);
    public abstract IQueryable<Entity> GetEntitiesFull(QueryEntitiesRequest request);


    protected virtual ColumnDescriptionFactory[] InitializeColumns()
    {
        var result = MemberEntryFactory.GenerateList<T>(MemberOptions.Properties | MemberOptions.Fields)
          .Select((e, i) => new ColumnDescriptionFactory(i, e.MemberInfo, null)).ToArray();

        return result;
    }

    public DynamicQueryCore<T> ColumnDisplayName<S>(Expression<Func<T, S>> column, Enum messageValue)
    {
        return this.Column(column, c => c.OverrideDisplayName = () => messageValue.NiceToString());
    }

    public DynamicQueryCore<T> ColumnDisplayName<S>(Expression<Func<T, S>> column, Func<string> messageValue)
    {
        return this.Column(column, c => c.OverrideDisplayName = messageValue);
    }

    public DynamicQueryCore<T> ColumnProperyRoutes<S>(Expression<Func<T, S>> column, params PropertyRoute[] routes)
    {
        return this.Column(column, c => c.PropertyRoutes = routes);
    }

    public DynamicQueryCore<T> Column<S>(Expression<Func<T, S>> column, Action<ColumnDescriptionFactory> change)
    {
        MemberInfo member = ReflectionTools.GetMemberInfo(column);
        ColumnDescriptionFactory col = StaticColumns.SingleEx(a => a.Name == member.Name);
        change(col);

        return this;
    }

    public ColumnDescriptionFactory EntityColumnFactory()
    {
        return StaticColumns.Where(c => c.IsEntity).SingleEx(() => "Entity column on {0}".FormatWith(QueryUtils.GetKey(QueryName)));
    }

    public virtual Expression? Expression
    {
        get { return null; }
    }

    public QueryDescription GetQueryDescription()
    {
        var entity = EntityColumnFactory();
        string? allowed = entity.IsAllowed();
        if (allowed != null)
            throw new InvalidOperationException(
                "Not authorized to see Entity column on {0} because {1}".FormatWith(QueryUtils.GetKey(QueryName), allowed));

        var columns = StaticColumns.Where(f => f.IsAllowed() == null).Select(f => f.BuildColumnDescription()).ToList();

        return new QueryDescription(QueryName, columns);
    }
}

public interface IDynamicInfo
{
    BuildExpressionContext Context { get; }
}

/// <typeparam name="T">Unraleted with the content, only with the original anonymous type </typeparam>
public class DQueryable<T> : IDynamicInfo
{
    public DQueryable(IQueryable query, BuildExpressionContext context)
    {
        this.Query = query;
        this.Context = context;
    }

    public IQueryable Query { get; private set; }
    public BuildExpressionContext Context { get; private set; }
}

/// <typeparam name="T">Unraleted with the content, only with the original anonymous type </typeparam>
public class DQueryableCount<T> : DEnumerable<T>
{
    public DQueryableCount(IQueryable query, BuildExpressionContext context, int totalElements) :
        base(query, context)
    {
        this.TotalElements = totalElements;
    }

    public int TotalElements { get; private set; }
}

/// <typeparam name="T">Unraleted with the content, only with the original anonymous type </typeparam>
public class DEnumerable<T> : IDynamicInfo
{
    public DEnumerable(IEnumerable collection, BuildExpressionContext context)
    {
        this.Collection = collection;
        this.Context = context;
    }

    public IEnumerable Collection { get; private set; }
    public BuildExpressionContext Context { get; private set; }
}

/// <typeparam name="T">Unraleted with the content, only with the original anonymous type</typeparam>
public class DEnumerableCount<T> : DEnumerable<T>
{
    public DEnumerableCount(IEnumerable collection, BuildExpressionContext context, int? totalElements) :
        base(collection, context)
    {
        this.TotalElements = totalElements;
    }

    public int? TotalElements {get; private set;}
}


public static class DQueryable
{
    #region ToDQueryable

    public static DQueryable<T> ToDQueryable<T>(this IQueryable<T> query, QueryDescription description)
    {
        ParameterExpression pe = Expression.Parameter(typeof(T));

        var dic = description.Columns.ToDictionary(
            cd => (QueryToken)new ColumnToken(cd, description.QueryName),
            cd => new ExpressionBox(Expression.PropertyOrField(pe, cd.Name).BuildLiteNullifyUnwrapPrimaryKey(cd.PropertyRoutes!)));

        return new DQueryable<T>(query, new BuildExpressionContext(typeof(T), pe, dic));
    }


    public static Task<DEnumerableCount<T>> AllQueryOperationsAsync<T>(this DQueryable<T> query, QueryRequest request, CancellationToken token)
    {
        return query
            .SelectMany(request.Multiplications())
            .Where(request.Filters)
            .OrderBy(request.Orders)
            .Select(request.Columns)
            .TryPaginateAsync(request.Pagination, request.SystemTime, token);
    }

    public static DEnumerableCount<T> AllQueryOperations<T>(this DQueryable<T> query, QueryRequest request)
    {
        return query
            .SelectMany(request.Multiplications())
            .Where(request.Filters)
            .OrderBy(request.Orders)
            .Select(request.Columns)
            .TryPaginate(request.Pagination, request.SystemTime);
    }

    #endregion

    #region Select

    public static IEnumerable<object?> SelectOne<T>(this DEnumerable<T> collection, QueryToken token)
    {
        var exp = Expression.Lambda(Expression.Convert(token.BuildExpression(collection.Context), typeof(object)), collection.Context.Parameter);

        return (IEnumerable<object?>)Untyped.Select(collection.Collection, exp.Compile());
    }

    public static IQueryable<object?> SelectOne<T>(this DQueryable<T> query, QueryToken token)
    {
        var exp = Expression.Lambda(Expression.Convert(token.BuildExpression(query.Context), typeof(object)), query.Context.Parameter);

        return (IQueryable<object?>)Untyped.Select(query.Query, exp);
    }

    public static DQueryable<T> Select<T>(this DQueryable<T> query, List<Column> columns)
    {
        return Select<T>(query, new HashSet<QueryToken>(columns.Select(c => c.Token)));
    }

    public static DQueryable<T> Select<T>(this DQueryable<T> query, HashSet<QueryToken> columns)
    {
        var selector = SelectTupleConstructor(query.Context, columns, out BuildExpressionContext newContext);

        return new DQueryable<T>(Untyped.Select(query.Query, selector), newContext);
    }

    public static DEnumerable<T> Select<T>(this DEnumerable<T> collection, List<Column> columns)
    {
        return Select<T>(collection, new HashSet<QueryToken>(columns.Select(c => c.Token)));
    }

    public static DEnumerable<T> Select<T>(this DEnumerable<T> collection, HashSet<QueryToken> columns)
    {
        var selector = SelectTupleConstructor(collection.Context, columns, out BuildExpressionContext newContext);

        return new DEnumerable<T>(Untyped.Select(collection.Collection, selector.Compile()), newContext);
    }


    static LambdaExpression SelectTupleConstructor(BuildExpressionContext context, HashSet<QueryToken> tokens, out BuildExpressionContext newContext)
    {
        string str = tokens.Select(t => QueryUtils.CanColumn(t)).NotNull().ToString("\r\n");
        if (str == null)
            throw new ApplicationException(str);

        List<Expression> expressions = tokens.Select(t => t.BuildExpression(context)).ToList();
        Expression ctor = TupleReflection.TupleChainConstructor(expressions);

        var pe = Expression.Parameter(ctor.Type);

        newContext = new BuildExpressionContext(
                ctor.Type, pe,
                tokens.Select((t, i) => new
                {
                    Token = t,
                    Expr = TupleReflection.TupleChainProperty(pe, i)
                }).ToDictionary(t => t.Token!, t => new ExpressionBox(t.Expr)));

        return Expression.Lambda(ctor, context.Parameter);
    }



    public static DEnumerable<T> Concat<T>(this DEnumerable<T> collection, DEnumerable<T> other)
    {
        if (collection.Context.ElementType != other.Context.ElementType)
            throw new InvalidOperationException("Enumerable's TupleType does not match Other's one.\r\n Enumerable: {0}: \r\n Other:  {1}".FormatWith(
                collection.Context.ElementType.TypeName(),
                other.Context.ElementType.TypeName()));

        return new DEnumerable<T>(Untyped.Concat(collection.Collection, other.Collection, collection.Context.ElementType), collection.Context);
    }

    public static DEnumerableCount<T> Concat<T>(this DEnumerableCount<T> collection, DEnumerableCount<T> other)
    {
        if (collection.Context.ElementType != other.Context.ElementType)
            throw new InvalidOperationException("Enumerable's TupleType does not match Other's one.\r\n Enumerable: {0}: \r\n Other:  {1}".FormatWith(
                collection.Context.ElementType.TypeName(),
                other.Context.ElementType.TypeName()));

        return new DEnumerableCount<T>(Untyped.Concat(collection.Collection,other.Collection, collection.Context.ElementType), collection.Context, collection.TotalElements + other.TotalElements);
    }
    #endregion

    public static DEnumerable<T> ToDEnumerable<T>(this DQueryable<T> query)
    {
        return new DEnumerable<T>(Untyped.ToList(query.Query, query.Context.ElementType), query.Context);
    }

    public static DEnumerable<T> ToDEnumerable<T>(this IEnumerable<T> query, QueryDescription description)
    {
        ParameterExpression pe = Expression.Parameter(typeof(T));

        var dic = description.Columns.ToDictionary(
            cd => (QueryToken)new ColumnToken(cd, description.QueryName),
            cd => new ExpressionBox(Expression.PropertyOrField(pe, cd.Name).BuildLiteNullifyUnwrapPrimaryKey(cd.PropertyRoutes!)));

        return new DEnumerable<T>(query, new BuildExpressionContext(typeof(T), pe, dic));
    }

    public static DEnumerableCount<T> WithCount<T>(this DEnumerable<T> result, int? totalElements)
    {
        return new DEnumerableCount<T>(result.Collection, result.Context, totalElements);
    }

    public static async Task<DEnumerable<T>> ToDEnumerableAsync<T>(this DQueryable<T> query, CancellationToken token)
    {
        var list = await Untyped.ToListAsync(query.Query, token, query.Context.ElementType);
        return new DEnumerable<T>(list, query.Context);
    }

    #region SelectMany
    public static DQueryable<T> SelectMany<T>(this DQueryable<T> query, List<CollectionElementToken> elementTokens)
    {
        foreach (var cet in elementTokens)
        {
            query = query.SelectMany(cet);
        }

        return query;
    }

    static MethodInfo miSelectMany = ReflectionTools.GetMethodInfo(() => Database.Query<TypeEntity>().SelectMany(t => t.Namespace, (t, c) => t)).GetGenericMethodDefinition();
    static MethodInfo miDefaultIfEmptyE = ReflectionTools.GetMethodInfo(() => Database.Query<TypeEntity>().AsEnumerable().DefaultIfEmpty()).GetGenericMethodDefinition();

    public static DQueryable<T> SelectMany<T>(this DQueryable<T> query, CollectionElementToken cet)
    {
        var eptML = MListElementPropertyToken.AsMListEntityProperty(cet.Parent!);

        Type elementType = eptML  != null ?
            MListElementPropertyToken.MListElementType(eptML) : 
            cet.Parent!.Type.ElementType()!;

        var collectionSelector = Expression.Lambda(typeof(Func<,>).MakeGenericType(query.Context.ElementType, typeof(IEnumerable<>).MakeGenericType(elementType)),
            Expression.Call(miDefaultIfEmptyE.MakeGenericMethod(elementType),
               eptML != null ? MListElementPropertyToken.BuildMListElements(eptML, query.Context) :
                cet.Parent!.BuildExpression(query.Context)),
            query.Context.Parameter);

        var elementParameter = Expression.Parameter(elementType);

        var properties = query.Context.Replacements.Values.Select(box => box.RawExpression).And(elementParameter.BuildLite().Nullify()).ToList();

        var ctor = TupleReflection.TupleChainConstructor(properties);

        var resultSelector = Expression.Lambda(ctor, query.Context.Parameter, elementParameter);

        var resultQuery = query.Query.Provider.CreateQuery(Expression.Call(null, miSelectMany.MakeGenericMethod(query.Context.ElementType, elementType, ctor.Type),
            new Expression[] { query.Query.Expression, Expression.Quote(collectionSelector), Expression.Quote(resultSelector) }));

        var parameter = Expression.Parameter(ctor.Type);

        var newReplacements = query.Context.Replacements.Select((kvp, i) => new
        {
            Token = kvp.Key,
            Expression = new ExpressionBox(TupleReflection.TupleChainProperty(parameter, i), 
            mlistElementRoute: kvp.Value.MListElementRoute)
        }).ToDictionary(a => a.Token, a => a.Expression);

        newReplacements.Add(cet,
            new ExpressionBox(TupleReflection.TupleChainProperty(parameter, query.Context.Replacements.Keys.Count),
            mlistElementRoute: eptML != null ? cet.GetPropertyRoute() : null
            ));

        var newContext = new BuildExpressionContext(ctor.Type, parameter, newReplacements);

        return new DQueryable<T>(resultQuery, newContext);
    }

    #endregion

    #region Where

    public static DQueryable<T> Where<T>(this DQueryable<T> query, params Filter[] filters)
    {
        return Where(query, filters.NotNull().ToList());
    }

    public static DQueryable<T> Where<T>(this DQueryable<T> query, List<Filter> filters)
    {
        LambdaExpression? predicate = GetPredicateExpression(query.Context, filters);
        if (predicate == null)
            return query;

        return new DQueryable<T>(Untyped.Where(query.Query, predicate), query.Context);
    }

    public static DQueryable<T> Where<T>(this DQueryable<T> query, Expression<Func<object, bool>> filter)
    {
        return new DQueryable<T>(Untyped.Where(query.Query, filter), query.Context);
    }

    public static DEnumerable<T> Where<T>(this DEnumerable<T> collection, params Filter[] filters)
    {
        return Where(collection, filters.NotNull().ToList());
    }

    public static DEnumerable<T> Where<T>(this DEnumerable<T> collection, List<Filter> filters)
    {
        LambdaExpression? where = GetPredicateExpression(collection.Context, filters);
        if (where == null)
            return collection;

        return new DEnumerable<T>(Untyped.Where(collection.Collection, where.Compile()), collection.Context);
    }

    static LambdaExpression? GetPredicateExpression(BuildExpressionContext context, List<Filter> filters)
    {
        if (filters == null || filters.Count == 0)
            return null;

        string str = filters
            .SelectMany(f => f.GetFilterConditions())
            .Select(f => QueryUtils.CanFilter(f.Token))
            .NotNull()
            .ToString("\r\n");

        if (str == null)
            throw new ApplicationException(str);

        Expression body = filters.Select(f => f.GetExpression(context)).AggregateAnd();

        return Expression.Lambda(body, context.Parameter);
    }

    #endregion

    #region OrderBy

    

    public static DQueryable<T> OrderBy<T>(this DQueryable<T> query, List<Order> orders)
    {
        string str = orders.Select(f => QueryUtils.CanOrder(f.Token)).NotNull().ToString("\r\n");
        if (str == null)
            throw new ApplicationException(str);

        var pairs = orders.Select(o => (
            lambda: QueryUtils.CreateOrderLambda(o.Token, query.Context),
            orderType: o.OrderType
        )).ToList();

        return new DQueryable<T>(Untyped.OrderBy(query.Query, pairs), query.Context);
    }

   
    public static DEnumerable<T> OrderBy<T>(this DEnumerable<T> collection, List<Order> orders)
    {
        var pairs = orders.Select(o => (
          lambda: QueryUtils.CreateOrderLambda(o.Token, collection.Context),
          orderType: o.OrderType
        )).ToList();


        return new DEnumerable<T>(Untyped.OrderBy(collection.Collection, pairs), collection.Context);
    }

    public static DEnumerableCount<T> OrderBy<T>(this DEnumerableCount<T> collection, List<Order> orders)
    {
        var pairs = orders.Select(o => (
          lambda: QueryUtils.CreateOrderLambda(o.Token, collection.Context),
          orderType: o.OrderType
        )).ToList();

        return new DEnumerableCount<T>(Untyped.OrderBy(collection.Collection, pairs), collection.Context, collection.TotalElements);
    }

    #endregion

    #region Unique

    [return: MaybeNull]
    public static T Unique<T>(this IEnumerable<T> collection, UniqueType uniqueType)
    {
        return uniqueType switch
        {
            UniqueType.First => collection.First(),
            UniqueType.FirstOrDefault => collection.FirstOrDefault(),
            UniqueType.Single => collection.SingleEx(),
            UniqueType.SingleOrDefault => collection.SingleOrDefaultEx(),
            UniqueType.Only => collection.Only(),
            _ => throw new InvalidOperationException(),
        };
    }

    //[return: MaybeNull]
    public static Task<T> UniqueAsync<T>(this IQueryable<T> collection, UniqueType uniqueType, CancellationToken token)
    {
        return uniqueType switch
        {
            UniqueType.First => collection.FirstAsync(token),
            UniqueType.FirstOrDefault => collection.FirstOrDefaultAsync(token)!,
            UniqueType.Single => collection.SingleAsync(token),
            UniqueType.SingleOrDefault => collection.SingleOrDefaultAsync(token)!,
            UniqueType.Only => collection.Take(2).ToListAsync(token).ContinueWith(l => l.Result.Only()!),
            _ => throw new InvalidOperationException(),
        };
    }

    #endregion

    #region TryTake
    public static DQueryable<T> TryTake<T>(this DQueryable<T> query, int? num)
    {
        if (num.HasValue)
            return new DQueryable<T>(Untyped.Take(query.Query, num.Value, query.Context.ElementType), query.Context);
        return query;
    }

    public static DEnumerable<T> TryTake<T>(this DEnumerable<T> collection, int? num)
    {
        if (num.HasValue)
            return new DEnumerable<T>(Untyped.Take(collection.Collection, num.Value, collection.Context.ElementType), collection.Context);
        return collection;
    }
    #endregion


    #region TryPaginate

    public static async Task<DEnumerableCount<T>> TryPaginateAsync<T>(this DQueryable<T> query, Pagination pagination, SystemTime? systemTime, CancellationToken token)
    {
        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));

        var elemType = query.Context.ElementType;

        if (pagination is Pagination.All)
        {
            var allList = await Untyped.ToListAsync(query.Query, token, elemType);

            return new DEnumerableCount<T>(allList, query.Context, allList.Count);
        }
        else if (pagination is Pagination.Firsts top)
        {
            var topList = await Untyped.ToListAsync(Untyped.Take(query.Query, top.TopElements, elemType), token, elemType);

            return new DEnumerableCount<T>(topList, query.Context, null);
        }
        else if (pagination is Pagination.Paginate pag)
        {
            if (systemTime is SystemTime.Interval)  //Results multipy due to Joins, not easy to change LINQ provider because joins are delayed
            {
                var q = Untyped.OrderAlsoByKeys(query.Query, elemType);

                var list = await Untyped.ToListAsync(query.Query /*q maybe?*/, token, elemType);

                var elements = list;
                if (pag.CurrentPage != 1)
                    elements = Untyped.ToList(Untyped.Skip(elements, (pag.CurrentPage - 1) * pag.ElementsPerPage, elemType), elemType);

                elements = Untyped.ToList(Untyped.Take(elements, pag.ElementsPerPage, elemType), elemType);

                return new DEnumerableCount<T>(elements, query.Context, list.Count);
            }
            else
            {
                var q = Untyped.OrderAlsoByKeys(query.Query, elemType);

                if (pag.CurrentPage != 1)
                    q = Untyped.Skip(q, (pag.CurrentPage - 1) * pag.ElementsPerPage, elemType);

                q = Untyped.Take(q, pag.ElementsPerPage, elemType);

                var listTask = await Untyped.ToListAsync(q, token, elemType);
                var countTask = systemTime is SystemTime.Interval ?
                    (await Untyped.ToListAsync(query.Query, token, elemType)).Count : //Results multipy due to Joins, not easy to change LINQ provider because joins are delayed
                    await Untyped.CountAsync(query.Query, token, elemType);

                return new DEnumerableCount<T>(listTask, query.Context, countTask);
            }
        }

        throw new InvalidOperationException("pagination type {0} not expexted".FormatWith(pagination.GetType().Name));
    }

    public static DEnumerableCount<T> TryPaginate<T>(this DQueryable<T> query, Pagination pagination, SystemTime? systemTime)
    {
        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));

        var elemType = query.Context.ElementType;

        if (pagination is Pagination.All)
        {
            var allList = Untyped.ToList(query.Query, elemType);

            return new DEnumerableCount<T>(allList, query.Context, allList.Count);
        }
        else if (pagination is Pagination.Firsts top)
        {
            var topList = Untyped.ToList(Untyped.Take(query.Query, top.TopElements, elemType), elemType);

            return new DEnumerableCount<T>(topList, query.Context, null);
        }
        else if (pagination is Pagination.Paginate pag)
        {
            if(systemTime is SystemTime.Interval)  //Results multipy due to Joins, not easy to change LINQ provider because joins are delayed
            {
                var q = Untyped.OrderAlsoByKeys(query.Query, elemType);

                var list = Untyped.ToList(query.Query /*q?*/, elemType);

                var elements = list;
                if (pag.CurrentPage != 1)
                    elements = Untyped.ToList(Untyped.Skip(elements, (pag.CurrentPage - 1) * pag.ElementsPerPage, elemType), elemType);

                elements = Untyped.ToList(Untyped.Take(elements, pag.ElementsPerPage, elemType), elemType);

                return new DEnumerableCount<T>(elements, query.Context, list.Count);
            }
            else
            {
                var q = Untyped.OrderAlsoByKeys(query.Query, elemType);

                if (pag.CurrentPage != 1)
                    q = Untyped.Skip(q, (pag.CurrentPage - 1) * pag.ElementsPerPage, elemType);

                q = Untyped.Take(q, pag.ElementsPerPage, elemType);

                var list = Untyped.ToList(q, elemType);
                var count = list.Count < pag.ElementsPerPage ? pag.ElementsPerPage :
                    Untyped.Count(query.Query, elemType);

                return new DEnumerableCount<T>(list, query.Context, count);
            }


          
        }

        throw new InvalidOperationException("pagination type {0} not expexted".FormatWith(pagination.GetType().Name));
    }

    public static DEnumerableCount<T> TryPaginate<T>(this DEnumerable<T> collection, Pagination pagination)
    {
        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));


        var elemType = collection.Context.ElementType;

        if (pagination is Pagination.All)
        {
            var allList = Untyped.ToList(collection.Collection, elemType);

            return new DEnumerableCount<T>(allList, collection.Context, allList.Count);
        }
        else if (pagination is Pagination.Firsts top)
        {
            var topList = Untyped.ToList(Untyped.Take(collection.Collection, top.TopElements, elemType), elemType);

            return new DEnumerableCount<T>(topList, collection.Context, null);
        }
        else if (pagination is Pagination.Paginate pag)
        {
            int? totalElements = null;

            var q = collection.Collection;
            if (pag.CurrentPage != 1)
                q = Untyped.Skip(q, (pag.CurrentPage - 1) * pag.ElementsPerPage, elemType);

            q = Untyped.Take(q, pag.ElementsPerPage, elemType);

            var list = Untyped.ToList(q, elemType);

            if (list.Count < pag.ElementsPerPage && pag.CurrentPage == 1)
                totalElements = list.Count;

            return new DEnumerableCount<T>(list, collection.Context, totalElements ?? Untyped.Count(collection.Collection, elemType));
        }

        throw new InvalidOperationException("pagination type {0} not expexted".FormatWith(pagination.GetType().Name));
    }

    public static DEnumerableCount<T> TryPaginate<T>(this DEnumerableCount<T> collection, Pagination pagination)
    {
        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));

        var elemType = collection.Context.ElementType;

        if (pagination is Pagination.All)
        {
            return new DEnumerableCount<T>(collection.Collection, collection.Context, collection.TotalElements);
        }
        else if (pagination is Pagination.Firsts top)
        {
            var topList = Untyped.ToList(Untyped.Take(collection.Collection, top.TopElements, elemType), elemType);

            return new DEnumerableCount<T>(topList, collection.Context, null);
        }
        else if (pagination is Pagination.Paginate pag)
        {
            var c = collection.Collection;
            if (pag.CurrentPage != 1)
                c = Untyped.Skip(c, (pag.CurrentPage - 1) * pag.ElementsPerPage, elemType);

            c = Untyped.Take(c, pag.ElementsPerPage, elemType);

            return new DEnumerableCount<T>(c, collection.Context, collection.TotalElements);
        }

        throw new InvalidOperationException("pagination type {0} not expexted".FormatWith(pagination.GetType().Name));
    }

    #endregion

    #region GroupBy

    static readonly GenericInvoker<Func<IEnumerable, Delegate, Delegate, IEnumerable>> giGroupByE =
        new((col, ks, rs) => (IEnumerable<object>)Enumerable.GroupBy<string, int, double>((IEnumerable<string>)col, (Func<string, int>)ks, (Func<int, IEnumerable<string>, double>)rs));
    public static DEnumerable<T> GroupBy<T>(this DEnumerable<T> collection, HashSet<QueryToken> keyTokens, HashSet<AggregateToken> aggregateTokens)
    {
        var rootKeyTokens = GetRootKeyTokens(keyTokens);

        var redundantKeyTokens = keyTokens.Except(rootKeyTokens).ToHashSet();

        var keySelector = KeySelector(collection.Context, rootKeyTokens);

        LambdaExpression resultSelector = ResultSelectSelectorAndContext(collection.Context, rootKeyTokens, redundantKeyTokens, aggregateTokens, keySelector.Body.Type, isQueryable: false, out BuildExpressionContext newContext);

        var resultCollection = giGroupByE.GetInvoker(collection.Context.ElementType, keySelector.Body.Type, resultSelector.Body.Type)(collection.Collection, keySelector.Compile(), resultSelector.Compile());

        return new DEnumerable<T>(resultCollection, newContext);
    }

    static MethodInfo miGroupByQ = ReflectionTools.GetMethodInfo(() => Queryable.GroupBy<string, int, double>((IQueryable<string>)null!, (Expression<Func<string, int>>)null!, (Expression<Func<int, IEnumerable<string>, double>>)null!)).GetGenericMethodDefinition();
    public static DQueryable<T> GroupBy<T>(this DQueryable<T> query, HashSet<QueryToken> keyTokens, HashSet<AggregateToken> aggregateTokens)
    {
        var rootKeyTokens = GetRootKeyTokens(keyTokens);

        var redundantKeyTokens = keyTokens.Except(rootKeyTokens).ToHashSet();

        var keySelector = KeySelector(query.Context, rootKeyTokens);

        LambdaExpression resultSelector = ResultSelectSelectorAndContext(query.Context, rootKeyTokens, redundantKeyTokens, aggregateTokens, keySelector.Body.Type, isQueryable: true, out BuildExpressionContext newContext);

        var resultQuery = query.Query.Provider.CreateQuery(Expression.Call(null, miGroupByQ.MakeGenericMethod(query.Context.ElementType, keySelector.Body.Type, resultSelector.Body.Type),
            new Expression[] { query.Query.Expression, Expression.Quote(keySelector), Expression.Quote(resultSelector) }));

        return new DQueryable<T>(resultQuery, newContext);
    }

    private static HashSet<QueryToken> GetRootKeyTokens(HashSet<QueryToken> keyTokens)
    {
        return keyTokens.Where(t => !keyTokens.Any(t2 => t2.Dominates(t))).ToHashSet();
    }


    static MethodInfo miFirstE = ReflectionTools.GetMethodInfo(() => Enumerable.First((IEnumerable<string>)null!)).GetGenericMethodDefinition();

    static LambdaExpression ResultSelectSelectorAndContext(BuildExpressionContext context, HashSet<QueryToken> rootKeyTokens, HashSet<QueryToken> redundantKeyTokens, HashSet<AggregateToken> aggregateTokens, Type keyTupleType, bool isQueryable, out BuildExpressionContext newContext)
    {
        Dictionary<QueryToken, Expression> resultExpressions = new Dictionary<QueryToken, Expression>();
        ParameterExpression pk = Expression.Parameter(keyTupleType, "key");
        ParameterExpression pe = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(context.ElementType), "e");
        
        resultExpressions.AddRange(rootKeyTokens.Select((kqt, i) => KeyValuePair.Create(kqt, TupleReflection.TupleChainProperty(pk, i))));
        
        if (redundantKeyTokens.Any())
        {
            if (isQueryable)
            {
                var tempContext = new BuildExpressionContext(keyTupleType, pk, rootKeyTokens.Select((kqt, i) => KeyValuePair.Create(kqt, new ExpressionBox(TupleReflection.TupleChainProperty(pk, i)))).ToDictionary());
                resultExpressions.AddRange(redundantKeyTokens.Select(t => KeyValuePair.Create(t, t.BuildExpression(tempContext))));
            }
            else
            {
                var first = Expression.Call(miFirstE.MakeGenericMethod(typeof(object)), pe);

                resultExpressions.AddRange(redundantKeyTokens.Select(t =>
                {
                    var exp = t.BuildExpression(context);
                    var replaced = ExpressionReplacer.Replace(exp,
                    new Dictionary<ParameterExpression, Expression>
                    {
                        { context.Parameter, first }
                    });

                    return KeyValuePair.Create(t, replaced);
                }));
            }
        }
        
        resultExpressions.AddRange(aggregateTokens.Select(at => KeyValuePair.Create((QueryToken)at, BuildAggregateExpressionEnumerable(pe, at, context))));

        var resultConstructor = TupleReflection.TupleChainConstructor(resultExpressions.Values);

        ParameterExpression pg = Expression.Parameter(resultConstructor.Type, "gr");
        newContext = new BuildExpressionContext(resultConstructor.Type, pg,
            resultExpressions.Keys.Select((t, i) => KeyValuePair.Create(t, new ExpressionBox(TupleReflection.TupleChainProperty(pg, i)))).ToDictionary());

        return Expression.Lambda(resultConstructor, pk, pe);
    }

    static LambdaExpression KeySelector(BuildExpressionContext context, HashSet<QueryToken> keyTokens)
    {
        var keySelector = Expression.Lambda(
          TupleReflection.TupleChainConstructor(keyTokens.Select(t => t.BuildExpression(context)).ToList()),
          context.Parameter);
        return keySelector;
    }

    static Expression BuildAggregateExpressionEnumerable(Expression collection, AggregateToken at, BuildExpressionContext context)
    {
        Type elementType = collection.Type.ElementType()!;

        if (at.AggregateFunction == AggregateFunction.Count && at.Parent == null)
            return Expression.Call(typeof(Enumerable), "Count", new[] { elementType }, new[] { collection });

        var body = at.Parent!.BuildExpression(context);

        if (at.AggregateFunction == AggregateFunction.Count)
        {
            if (at.FilterOperation.HasValue)
            {
                var condition = QueryUtils.GetCompareExpression(at.FilterOperation.Value, body.Nullify(), Expression.Constant(at.Value, body.Type.Nullify()));

                var lambda = Expression.Lambda(condition, context.Parameter);

                return Expression.Call(typeof(Enumerable), AggregateFunction.Count.ToString(), new[] { elementType }, new[] { collection, lambda });
            }
            else if (at.Distinct)
            {
                var lambda = Expression.Lambda(body, context.Parameter);

                var select = Expression.Call(typeof(Enumerable), "Select", new[] { elementType, body.Type }, new[] { collection, lambda });
                var distinct = Expression.Call(typeof(Enumerable), "Distinct", new[] { body.Type }, new[] { select });
                var param = Expression.Parameter(lambda.Body.Type);
                LambdaExpression notNull = Expression.Lambda(Expression.NotEqual(param, Expression.Constant(null, param.Type.Nullify())), param);
                var count = Expression.Call(typeof(Enumerable), "Count", new[] { body.Type }, new Expression[] { distinct, notNull });

                return count;
            }
            else
                throw new InvalidOperationException();
        }
        else
        {
            if (body.Type != at.Type)
                body = body.TryConvert(at.Type);

            var lambda = Expression.Lambda(body, context.Parameter);

            if (at.AggregateFunction == AggregateFunction.Min || at.AggregateFunction == AggregateFunction.Max)
                return Expression.Call(typeof(Enumerable), at.AggregateFunction.ToString(), new[] { elementType, lambda.Body.Type }, new[] { collection, lambda });

            return Expression.Call(typeof(Enumerable), at.AggregateFunction.ToString(), new[] { elementType }, new[] { collection, lambda });
        }
    }

    static Expression BuildAggregateExpressionQueryable(Expression collection, AggregateToken at, BuildExpressionContext context)
    {
        Type elementType = collection.Type.ElementType()!;

        if (at.AggregateFunction == AggregateFunction.Count)
            return Expression.Call(typeof(Queryable), "Count", new[] { elementType }, new[] { collection });

        var body = at.Parent!.BuildExpression(context);

        var type = at.Type;

        if (body.Type != type)
            body = body.TryConvert(type);

        var lambda = Expression.Lambda(body, context.Parameter);
        var quotedLambda = Expression.Quote(lambda);

        if (at.AggregateFunction == AggregateFunction.Min || at.AggregateFunction == AggregateFunction.Max)
            return Expression.Call(typeof(Queryable), at.AggregateFunction.ToString(), new[] { elementType, lambda.Body.Type }, new[] { collection, quotedLambda });

        return Expression.Call(typeof(Queryable), at.AggregateFunction.ToString(), new[] { elementType }, new[] { collection, quotedLambda });
    }

    static Expression BuildAggregateExpressionQueryableAsync(Expression collection, AggregateToken at, BuildExpressionContext context, CancellationToken token)
    {
        var tokenConstant = Expression.Constant(token);

        Type elementType = collection.Type.ElementType()!;

        if (at.AggregateFunction == AggregateFunction.Count)
            return Expression.Call(typeof(QueryableAsyncExtensions), "CountAsync", new[] { elementType }, new[] { collection, tokenConstant });

        var body = at.Parent!.BuildExpression(context);

        var type = at.AggregateFunction == AggregateFunction.Sum ? at.Type.UnNullify() : at.Type;

        if (body.Type != type)
            body = body.TryConvert(type);

        var lambda = Expression.Lambda(body, context.Parameter);
        var quotedLambda = Expression.Quote(lambda);

        if (at.AggregateFunction == AggregateFunction.Min || at.AggregateFunction == AggregateFunction.Max)
            return Expression.Call(typeof(QueryableAsyncExtensions), at.AggregateFunction.ToString() + "Async", new[] { elementType, lambda.Body.Type }, new[] { collection, quotedLambda, tokenConstant });

        return Expression.Call(typeof(QueryableAsyncExtensions), at.AggregateFunction.ToString() + "Async", new[] { elementType }, new[] { collection, quotedLambda, tokenConstant });
    }


    #endregion

    #region SimpleAggregate

    public static object? SimpleAggregate<T>(this DEnumerable<T> collection, AggregateToken simpleAggregate)
    {
        var expr = BuildAggregateExpressionEnumerable(Expression.Constant(collection.Collection), simpleAggregate, collection.Context);

        return Expression.Lambda<Func<object?>>(Expression.Convert(expr, typeof(object))).Compile()();
    }

    public static object? SimpleAggregate<T>(this DQueryable<T> query, AggregateToken simpleAggregate)
    {
        var expr = BuildAggregateExpressionQueryable(query.Query.Expression, simpleAggregate, query.Context);

        return Expression.Lambda<Func<object?>>(Expression.Convert(expr, typeof(object))).Compile()();
    }

    public static Task<object?> SimpleAggregateAsync<T>(this DQueryable<T> query, AggregateToken simpleAggregate, CancellationToken token)
    {
        var expr = BuildAggregateExpressionQueryableAsync(query.Query.Expression, simpleAggregate, query.Context, token);

        var func = (Func<Task>)Expression.Lambda(expr).Compile();

        var task = func();

        return CastTask<object?>(task);
    }
    public static async Task<T> CastTask<T>(this Task task)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        await task.ConfigureAwait(false);

        object? result = task.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(task);
        return (T)result!;
    }

    #endregion

    public struct ExpandColumn<T> : IExpandColumn
    {
        public QueryToken Token { get; private set; }
        
        public readonly Func<Lite<Entity>, T> GetValue;
        public ExpandColumn(QueryToken token, Func<Lite<Entity>, T> getValue)
        {
            Token = token;
            GetValue = getValue;
        }

        Expression IExpandColumn.GetExpression(Expression entitySelector)
        {
            return Expression.Invoke(Expression.Constant(GetValue), entitySelector);
        }
    }

    public interface IExpandColumn
    {
        public QueryToken Token { get;}
        Expression GetExpression(Expression entitySelector);
    }

    public static DEnumerable<T> ReplaceColumns<T>(this DEnumerable<T> query, params IExpandColumn[] newColumns)
    {
        var entity = query.Context.Replacements.Single(a => a.Key.FullKey() == "Entity").Value.GetExpression();
        var newColumnsDic = newColumns.ToDictionary(a => a.Token, a => a.GetExpression(entity));

        List<QueryToken> tokens = query.Context.Replacements.Keys.Union(newColumns.Select(a => a.Token)).ToList();
        List<Expression> expressions = tokens.Select(t => newColumnsDic.TryGetC(t) ?? query.Context.Replacements.GetOrThrow(t).GetExpression()).ToList();
        Expression ctor = TupleReflection.TupleChainConstructor(expressions);

        var pe = Expression.Parameter(ctor.Type);

        var newContext = new BuildExpressionContext(
                ctor.Type, pe,
                tokens
                .Select((t, i) => new { Token = t, Expr = TupleReflection.TupleChainProperty(pe, i) })
                .ToDictionary(t => t.Token!, t => new ExpressionBox(t.Expr)));

        var selector = Expression.Lambda(ctor, query.Context.Parameter);

        return new DEnumerable<T>(Untyped.Select(query.Collection, selector.Compile()), newContext);
    }

    public static ResultTable ToResultTable<T>(this DEnumerableCount<T> collection, QueryRequest req)
    {
        var isMultiKeyGrupping = req.GroupResults && req.Columns.Count(col => col.Token is not AggregateToken) >= 2;

        var columnAccesors = req.Columns.Select(c =>
        {
            var expression = Expression.Lambda(c.Token.BuildExpression(collection.Context), collection.Context.Parameter);

            var lambda = expression.Compile();

            var array = Untyped.ToArray(Untyped.Select(collection.Collection, lambda), expression.Body.Type);

            var rc = new ResultColumn(c, array);

            if (c.Token.Type.IsLite() || isMultiKeyGrupping && c.Token is not AggregateToken)
                rc.CompressUniqueValues = true;

            return rc;
        }).ToArray();

        return new ResultTable(columnAccesors, collection.TotalElements, req.Pagination);
    }
}


static class Untyped
{
    static MethodInfo miSelectQ =
        ReflectionTools.GetMethodInfo(() => ((IQueryable<string>)null!).Select((Expression<Func<string, int>>)null!)).GetGenericMethodDefinition();
    public static IQueryable Select(IQueryable query, LambdaExpression selector)
    {
        var types = selector.Type.GenericTypeArguments;

        var mi = miSelectQ.MakeGenericMethod(types);

        return query.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { query.Expression, Expression.Quote(selector) }));
    }

    static GenericInvoker<Func<IEnumerable, Delegate, IEnumerable>> giSelectE =
    new((q, selector) => ((IEnumerable<string>)q).Select((Func<string, int>)selector));
    public static IEnumerable Select(IEnumerable collection, Delegate selector)
    {
        var types = selector.GetType().GenericTypeArguments;

        return giSelectE.GetInvoker(types)(collection, selector);
    }


    static MethodInfo miWhereQ =
        ReflectionTools.GetMethodInfo(() => ((IQueryable<string>)null!).Where((Expression<Func<string, bool>>)null!)).GetGenericMethodDefinition();
    public static IQueryable Where(IQueryable query, LambdaExpression predicate)
    {
        var types = query.GetType().GenericTypeArguments;

        var mi = miWhereQ.MakeGenericMethod(types);

        return query.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { query.Expression, Expression.Quote(predicate) }));
    }

    static GenericInvoker<Func<IEnumerable, Delegate, IEnumerable>> giWhereE =
        new((q, predicate) => ((IEnumerable<string>)q).Where<string>((Func<string, bool>)predicate));
    public static IEnumerable Where(IEnumerable collection, Delegate selector)
    {
        var types = selector.GetType().GenericTypeArguments;

        return giSelectE.GetInvoker(types)(collection, selector);
    }

    static MethodInfo miDistinctQ =
        ReflectionTools.GetMethodInfo(() => ((IQueryable<string>)null!).Distinct()).GetGenericMethodDefinition();
    public static IQueryable Distinct(IQueryable query, Type elementType)
    {
        var mi = miDistinctQ.MakeGenericMethod(elementType);

        return query.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { query.Expression }));
    }

    static MethodInfo miOrderAlsoByKeysQ =
    ReflectionTools.GetMethodInfo(() => ((IQueryable<string>)null!).OrderAlsoByKeys()).GetGenericMethodDefinition();
    public static IQueryable OrderAlsoByKeys(IQueryable query, Type elementType)
    {
        var mi = miOrderAlsoByKeysQ.MakeGenericMethod(elementType);

        return query.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { query.Expression }));
    }

    static GenericInvoker<Func<IEnumerable, int, IEnumerable>> giTakeE =
    new((q, limit) => ((IEnumerable<string>)q).Take<string>(limit));
    public static IEnumerable Take(IEnumerable collection, int limit, Type elementType)
    {
        return giTakeE.GetInvoker(elementType)(collection, limit);
    }

    static MethodInfo miTakeQ =
      ReflectionTools.GetMethodInfo(() => ((IQueryable<string>)null!).Take(3)).GetGenericMethodDefinition();
    public static IQueryable Take(IQueryable query, int limit, Type elementType)
    {
        var mi = miTakeQ.MakeGenericMethod(elementType);

        return query.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { query.Expression, Expression.Constant(limit) }));
    }

    static GenericInvoker<Func<IEnumerable, int, IEnumerable>> giSkipE =
        new((q, limit) => ((IEnumerable<string>)q).Skip<string>(limit));
    public static IEnumerable Skip(IEnumerable collection, int limit, Type elementType)
    {
        return giSkipE.GetInvoker(elementType)(collection, limit);
    }

    static MethodInfo miSkipQ =
      ReflectionTools.GetMethodInfo(() => ((IQueryable<string>)null!).Skip(3)).GetGenericMethodDefinition();
    public static IQueryable Skip(IQueryable query, int limit, Type elementType)
    {
        var mi = miSkipQ.MakeGenericMethod(elementType);

        return query.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { query.Expression, Expression.Constant(limit) }));
    }

    static GenericInvoker<Func<IEnumerable, int>> giCountE =
    new((q) => ((IEnumerable<string>)q).Count());
    public static int Count(IEnumerable collection, Type elementType)
    {
        return giCountE.GetInvoker(elementType)(collection);
    }


    static MethodInfo miCountQ =
        ReflectionTools.GetMethodInfo(() => ((IQueryable<string>)null!).Count()).GetGenericMethodDefinition();
    public static int Count(IQueryable query, Type elementType)
    {
        var mi = miCountQ.MakeGenericMethod(elementType);

        return (int)query.Provider.Execute(Expression.Call(null, mi, new Expression[] { query.Expression }))!;
    }

    public static async Task<int> CountAsync(IQueryable query, CancellationToken token, Type elementType)
    {
        var mi = miCountQ.MakeGenericMethod(elementType);

        var result = await ((IQueryProviderAsync)query.Provider).ExecuteAsync(Expression.Call(null, mi, new Expression[] { query.Expression }), token)!;

        return (int)result!;
    }

    static MethodInfo miConcatQ =
      ReflectionTools.GetMethodInfo(() => ((IQueryable<string>)null!).Concat((IQueryable<string>)null!)).GetGenericMethodDefinition();
    public static IQueryable Concat(IQueryable query, IQueryable query2, Type elementType)
    {
        var mi = miConcatQ.MakeGenericMethod(elementType);

        return query.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { query.Expression, query2.Expression }));
    }

    static GenericInvoker<Func<IEnumerable, IEnumerable, IEnumerable>> gConcatE =
        new((q, q2) => ((IEnumerable<string>)q).Concat((IEnumerable<string>)q2));

    public static IEnumerable Concat(IEnumerable collection, IEnumerable collection2, Type elementType)
    {
        return gConcatE.GetInvoker(elementType)(collection, collection2);
    }

    static GenericInvoker<Func<IEnumerable, Array>> gToArrayE =
        new((q) => ((IEnumerable<string>)q).ToArray());
    public static Array ToArray(IEnumerable collection, Type elementType)
    {
        return gToArrayE.GetInvoker(elementType)(collection);
    }

    static GenericInvoker<Func<IEnumerable, IList>> gToListE =
    new((q) => ((IEnumerable<string>)q).ToList());

    public static IList ToList(IEnumerable collection, Type elementType)
    {
        return gToListE.GetInvoker(elementType)(collection);
    }

    static GenericInvoker<Func<IQueryable, CancellationToken, Task<IList>>> gToListAsyncQ =
        new((q, token) => ToIListAsync((IQueryable<string>)q, token));

    public static Task<IList> ToListAsync(IQueryable query, CancellationToken token, Type elementType)
    {
        return gToListAsyncQ.GetInvoker(elementType)(query, token);
    }

    static async Task<IList> ToIListAsync<T>(IQueryable<T> query, CancellationToken token)
    {
        return await query.ToListAsync(token);
    }

    static readonly GenericInvoker<Func<IEnumerable, Delegate, IEnumerable>> giOrderByE = new((col, del) => ((IEnumerable<object>)col).OrderBy((Func<object, object?>)del));
    static readonly GenericInvoker<Func<IEnumerable, Delegate, IEnumerable>> giOrderByDescendingE = new((col, del) => ((IEnumerable<object>)col).OrderByDescending((Func<object, object?>)del));
    public static IEnumerable OrderBy(IEnumerable collection, LambdaExpression lambda, OrderType orderType)
    {
        var mi = orderType == OrderType.Ascending ? giOrderByE : giOrderByDescendingE;

        return mi.GetInvoker(lambda.Type.GetGenericArguments())(collection, lambda.Compile());
    }

    static readonly GenericInvoker<Func<IEnumerable, Delegate, IEnumerable>> giThenByE = new((col, del) => ((IOrderedEnumerable<object>)col).ThenBy((Func<object, object?>)del));
    static readonly GenericInvoker<Func<IEnumerable, Delegate, IEnumerable>> giThenByDescendingE = new((col, del) => ((IOrderedEnumerable<object>)col).ThenByDescending((Func<object, object?>)del));
    public static IEnumerable ThenBy(IEnumerable collection, LambdaExpression lambda, OrderType orderType)
    {
        var mi = orderType == OrderType.Ascending ? giThenByE : giThenByDescendingE;

        return mi.GetInvoker(lambda.Type.GetGenericArguments())(collection, lambda.Compile());
    }

    public static IEnumerable OrderBy(IEnumerable collection, List<(LambdaExpression lambda, OrderType orderType)> orders)
    {
        if (orders == null || orders.Count == 0)
            return collection;

        IEnumerable result = Untyped.OrderBy(collection, orders[0].lambda, orders[0].orderType);

        foreach (var (lambda, orderType) in orders.Skip(1))
        {
            result = Untyped.ThenBy(result, lambda, orderType);
        }

        return result;
    }

    static MethodInfo miOrderByQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeEntity>().OrderBy(t => t.Id)).GetGenericMethodDefinition();
    static MethodInfo miOrderByDescendingQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeEntity>().OrderByDescending(t => t.Id)).GetGenericMethodDefinition();
    public static IOrderedQueryable OrderBy(IQueryable query, LambdaExpression lambda, OrderType orderType)
    {
        MethodInfo mi = (orderType == OrderType.Ascending ? miOrderByQ : miOrderByDescendingQ).MakeGenericMethod(lambda.Type.GetGenericArguments());

        return (IOrderedQueryable)query.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { query.Expression, Expression.Quote(lambda) }));
    }

    static MethodInfo miThenByQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeEntity>().OrderBy(t => t.Id).ThenBy(t => t.Id)).GetGenericMethodDefinition();
    static MethodInfo miThenByDescendingQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeEntity>().OrderBy(t => t.Id).ThenByDescending(t => t.Id)).GetGenericMethodDefinition();
    public static IOrderedQueryable ThenBy(IOrderedQueryable query, LambdaExpression lambda, OrderType orderType)
    {
        MethodInfo mi = (orderType == OrderType.Ascending ? miThenByQ : miThenByDescendingQ).MakeGenericMethod(lambda.Type.GetGenericArguments());

        return (IOrderedQueryable)query.Provider.CreateQuery(Expression.Call(null, mi, new Expression[] { query.Expression, Expression.Quote(lambda) }));
    }

    public static IQueryable OrderBy(IQueryable query, List<(LambdaExpression lambda, OrderType orderType)> orders)
    {
        if (orders == null || orders.Count == 0)
            return query;

        IOrderedQueryable result = Untyped.OrderBy(query, orders[0].lambda, orders[0].orderType);

        foreach (var (lambda, orderType) in orders.Skip(1))
        {
            result = Untyped.ThenBy(result, lambda, orderType);
        }

        return result;
    }
}
