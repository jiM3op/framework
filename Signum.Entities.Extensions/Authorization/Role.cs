using System.Security.Authentication;
using System.Collections.Specialized;
using Signum.Entities.Basics;

namespace Signum.Entities.Authorization;

[EntityKind(EntityKind.Shared, EntityData.Master)]
public class RoleEntity : Entity
{
    [UniqueIndex]
    [StringLengthValidator(Min = 2, Max = 100)]
    public string Name { get; set; }

    MergeStrategy mergeStrategy;
    public MergeStrategy MergeStrategy
    {
        get { return mergeStrategy; }
        set
        {
            if (Set(ref mergeStrategy, value))
                Notify(() => StrategyHint);
        }
    }

    [NotifyCollectionChanged]
    public MList<Lite<RoleEntity>> Roles { get; set; } = new MList<Lite<RoleEntity>>();

    protected override void ChildCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        Notify(() => StrategyHint);
    }

    [HiddenProperty]
    public string? StrategyHint
    {
        get
        {
            if (Roles.Any())
                return null;

            return AuthAdminMessage.NoRoles.NiceToString()  + "-> " + (mergeStrategy == MergeStrategy.Union ? AuthAdminMessage.Nothing : AuthAdminMessage.Everything).NiceToString();
        }
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);

    public static Lite<RoleEntity> Current
    {
        get
        {
            var userHolder = UserHolder.Current;
            if (userHolder == null)
                throw new AuthenticationException(LoginAuthMessage.NotUserLogged.NiceToString());

            return (Lite<RoleEntity>)userHolder.GetClaim("Role")!;
        }
    }
}

public enum MergeStrategy
{
    Union,
    Intersection,
}

[AutoInit]
public static class RoleOperation
{
    public static ExecuteSymbol<RoleEntity> Save;
    public static DeleteSymbol<RoleEntity> Delete;
}
