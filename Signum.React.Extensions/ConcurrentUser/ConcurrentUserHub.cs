using Microsoft.AspNetCore.SignalR;
using Signum.Engine.Authorization;
using Signum.Entities.Authorization;
using Signum.Entities.ConcurrentUser;

namespace Signum.React.ConcurrentUser;

public interface IConcurrentUserClient
{
    Task EntitySaved(string? newTicks);

    Task ConcurrentUsersChanged();
}

public class ConcurrentUserHub : Hub<IConcurrentUserClient>
{
    public override Task OnConnectedAsync()
    {        
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        using (AuthLogic.Disable())
        {
            var entities = Database.Query<ConcurrentUserEntity>()
                .Where(a => a.SignalRConnectionID == this.Context.ConnectionId).Select(a => a.TargetEntity).ToList();

            Database.Query<ConcurrentUserEntity>().Where(a => a.SignalRConnectionID == this.Context.ConnectionId).UnsafeDelete();

            ConcurrentUserServer.UpdateConcurrentUsers(entities.Select(a => a.Key()).ToHashSet());
        }
        return base.OnDisconnectedAsync(exception);
    }

    public Task EnterEntity(string liteKey, DateTime startTime, string userKey)
    {
        var lite = Lite.Parse(liteKey);
        var user = (Lite<UserEntity>)Lite.Parse(userKey);
        using (AuthLogic.Disable())
        using (OperationLogic.AllowSave<ConcurrentUserEntity>())
        {
            new ConcurrentUserEntity
            {
                TargetEntity = lite,
                User = user,
                StartTime = startTime,
                SignalRConnectionID = this.Context.ConnectionId,
            }.Save();
        };

        ConcurrentUserServer.UpdateConcurrentUsers(new HashSet<string> { liteKey });

        return this.Groups.AddToGroupAsync(this.Context.ConnectionId, liteKey);
    }

    public Task EntityModified(string liteKey, DateTime startTime, string userKey, bool modified)
    {
        var lite = Lite.Parse(liteKey);
        var user = (Lite<UserEntity>)Lite.Parse(userKey);
        using (AuthLogic.Disable())
        {
            Database.Query<ConcurrentUserEntity>()
                .Where(a => a.TargetEntity.Is(lite) && a.User.Is(user) && a.SignalRConnectionID == this.Context.ConnectionId && a.StartTime == startTime)
                .UnsafeUpdate(a => a.IsModified, a => modified);
        };

        ConcurrentUserServer.UpdateConcurrentUsers(new HashSet<string> { liteKey });

        return Task.CompletedTask;
    }

    public Task ExitEntity(string liteKey, DateTime startTime, string userKey)
    {
        var lite = Lite.Parse(liteKey);
        var user = (Lite<UserEntity>)Lite.Parse(userKey);

        using (AuthLogic.Disable())
        {
            Database.Query<ConcurrentUserEntity>()
                .Where(a => a.TargetEntity.Is(lite) && a.User.Is(user) && a.SignalRConnectionID == this.Context.ConnectionId && a.StartTime == startTime)
                .UnsafeDelete();

            if (Database.Query<ConcurrentUserEntity>().Any(a => a.TargetEntity.Is(lite) && a.User.Is(user) && a.SignalRConnectionID == this.Context.ConnectionId))
                return Task.CompletedTask;
        }

        ConcurrentUserServer.UpdateConcurrentUsers(new HashSet<string> { liteKey });

        return this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, liteKey);
    }
}
