using Signum.Entities.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Entities;

namespace Signum.Engine.Json;
public class FullEntityJsonSerializer
{
    public static JsonSerializerOptions JsonSerializerOptions;
    static FullEntityJsonSerializer()
    {

        JsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IncludeFields = true,
            Converters =
            {
                new EntityJsonConverterFactoryFull(),
                new LiteJsonConverterFactory(),
                new MListJsonConverterFactory(),
                new JsonStringEnumConverter(),
                new ResultTableConverter(),
                new TimeSpanConverter(),
                new DateOnlyConverter(),
                new TimeOnlyConverter()
            }
        };
    }
}


public class LiteJsonConverterFactory : LiteJsonConverterFactoryBase
{
    protected internal override string GetCleanName(Type type)
    {
        return TypeLogic.GetCleanName(type);
    }

    protected internal override Type GetType(string cleanName)
    {
        return TypeLogic.GetType(cleanName);
    }
}

public class MListJsonConverterFactory : MListJsonConverterFactoryBase
{
    internal protected override Type GetRowIdTypeFromAttribute(PropertyRoute route)
    {
        var settings = Schema.Current.Settings;
        var att = settings.FieldAttribute<PrimaryKeyAttribute>(route) ??
    (route.IsVirtualMList() ? settings.TypeAttribute<PrimaryKeyAttribute>(route.Type.ElementType()!) : null) ??
    settings.DefaultPrimaryKeyAttribute;

        return att.Type;
    }

    internal protected override bool GetPreserveOrderFromAttribute(PropertyRoute route)
    {
        var att = Schema.Current.Settings.FieldAttribute<PreserveOrderAttribute>(route);

        return att != null;
    }

    internal protected override void AssertCanWrite(PropertyRoute pr, ModifiableEntity? mod)
    {
    }
}

public class EntityJsonConverterFactoryFull : EntityJsonConverterFactoryBase
{
    protected internal override Type GetType(string typeString)
    {
        return TypeLogic.GetType(typeString);
    }

    protected internal override string GetCleanName(Type entityType)
    {
        return TypeLogic.GetCleanName(entityType);
    }

    protected internal override bool HasTicks(Type entityType)
    {
        return Schema.Current.Table(entityType).Ticks != null;
    }

    protected internal override IDisposable NewEntityCache()
    {
        return new EntityCache();
    }

    protected internal override bool ContinueOnError(Exception e, ModifiableEntity entity, PropertyInfo pi)
    {
        return false;
    }
    protected internal override bool CanRead(PropertyRoute route, ModifiableEntity mod)
    {
        return true;
    }
    protected internal override bool GetAllowDirectMListChanges()
    {
        return true;
    }
    protected internal override ModifiableEntity GetExistingEntity(ref Utf8JsonReader reader, IModifiableEntity? existingValue, IdentityInfo identityInfo, Type type)
    {
        if (identityInfo.Id == null)
            throw new JsonException($"Missing Id and IsNew for {identityInfo} ({reader.CurrentState})");

        var id = PrimaryKey.Parse(identityInfo.Id, type);
        if (existingValue != null && existingValue.GetType() == type)
        {
            Entity existingEntity = (Entity)existingValue;
            if (existingEntity.Id == id)
            {
                if (identityInfo.Ticks != null)
                {
                    if (identityInfo.Modified == true && existingEntity.Ticks != identityInfo.Ticks.Value)
                        throw new ConcurrencyException(type, id);

                    existingEntity.Ticks = identityInfo.Ticks.Value;
                }

                return existingEntity;
            }
        }

        var result = (Entity?)CustomConstructor.TryGetC(type)?.Invoke() ??
         (Entity)Activator.CreateInstance(type, nonPublic: true)!;

        if (identityInfo.Id != null)
            ((Entity)result).SetId(PrimaryKey.Parse(identityInfo.Id, type));

        if (!identityInfo.Modified!.Value)
            ((Entity)result).SetCleanModified(isSealed: false);

        if (identityInfo.IsNew != true)
            ((Entity)result).SetIsNew(false);

        if (identityInfo.Ticks != null)
            result.Ticks = identityInfo.Ticks.Value;

        return result;
    }

    protected internal override void SetProperty(PropertyConverter pc, PropertyRoute pr, ModifiableEntity entity, object? newValue, object? oldValue, bool markedAsModified)
    {
        pc.SetValue?.Invoke(entity, newValue);
    }

    protected internal override void CleanModificationsIsNecessary(ModifiableEntity mod)
    {
        mod.SetCleanModified(isSealed: false);
    }

    public override void AssertCanWrite(PropertyRoute propertyRoute, ModifiableEntity entity)
    {
    }
}

