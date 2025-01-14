using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Authorization;
using Signum.Engine.Cache;
using Signum.Entities.Cache;
using Signum.Engine.Scheduler;
using Signum.React.Filters;

namespace Signum.React.Cache;

public class CacheController : ControllerBase
{
    [HttpGet("api/cache/view")]
    public CacheStateTS View()
    {
        CachePermission.ViewCache.AssertAuthorized();

        var tables = CacheLogic.Statistics().Select(ctb => new CacheTableTS(ctb)).ToList();

        var lazies = GlobalLazy.Statistics().Select(ctb => new ResetLazyStatsTS(ctb)).ToList();

        return new CacheStateTS
        {
            IsEnabled = !CacheLogic.GloballyDisabled,
            ServerBroadcast = CacheLogic.ServerBroadcast?.ToString(),
            SqlDependency = CacheLogic.WithSqlDependency,
            Tables = tables,
            Lazies = lazies
        };
    }

    [HttpPost("api/cache/enable")]
    public void Enable()
    {
        CachePermission.ViewCache.AssertAuthorized();

        CacheLogic.GloballyDisabled = false;
        SystemEventLogLogic.Log("CacheLogic.Enable");
    }

    [HttpPost("api/cache/disable")]
    public void Disable()
    {
        CachePermission.ViewCache.AssertAuthorized();

        CacheLogic.GloballyDisabled = true;
        SystemEventLogLogic.Log("CacheLogic.Disable");
    }

    [HttpPost("api/cache/clear")]
    public void Clear()
    {
        CachePermission.InvalidateCache.AssertAuthorized();

        CacheLogic.ForceReset();
        GlobalLazy.ResetAll();
        Schema.Current.InvalidateMetadata();
        GC.Collect(2);
    }

    [HttpPost("api/cache/invalidateTable"), SignumAllowAnonymous]
    public void InvalidateTable([FromBody]InvalidateTableRequest req)
    {
        if (CacheLogic.ServerBroadcast is not SimpleHttpBroadcast sci)
            throw new InvalidOperationException("CacheInvalidator is not a SimpleHttpCacheInvalidator");

        sci.InvalidateTable(req);
    }
}

public class ResetLazyStatsTS
{
    public string typeName;
    public int hits;
    public int invalidations;
    public int loads;
    public string sumLoadTime;

    public ResetLazyStatsTS(ResetLazyStats rls)
    {
        this.typeName = rls.Type.TypeName();
        this.hits = rls.Hits;
        this.invalidations = rls.Invalidations;
        this.loads = rls.Loads;
        this.sumLoadTime = rls.SumLoadTime.NiceToString();
    }
}

public class CacheStateTS
{
    public bool IsEnabled;
    public bool SqlDependency;
    public string? ServerBroadcast;
    public List<CacheTableTS> Tables;
    public List<ResetLazyStatsTS> Lazies;
}

public class CacheTableTS
{
    public string tableName;
    public string typeName;
    public int? count;
    public int hits;
    public int invalidations;
    public int loads;
    public string sumLoadTime;
    public List<CacheTableTS> subTables;

    public CacheTableTS(CachedTableBase ct)
    {
        this.tableName = ct.Table.Name.Name;
        this.typeName = ct.Type.TypeName();
        this.count = ct.Count;
        this.hits = ct.Hits;
        this.invalidations = ct.Invalidations;
        this.loads = ct.Loads;
        this.sumLoadTime = ct.SumLoadTime.NiceToString();

        if (ct.SubTables != null)
            this.subTables = ct.SubTables.Select(ctv => new CacheTableTS(ctv)).ToList();
    }
}
