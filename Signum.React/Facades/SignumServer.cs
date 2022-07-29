using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Signum.Engine.Basics;
using Signum.Engine.Json;
using Signum.Engine.Maps;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Json;
using Signum.Entities.Reflection;
using Signum.React.ApiControllers;
using Signum.React.Filters;
using Signum.React.JsonModelValidators;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signum.React.Facades;

public static class SignumServer
{
    public static WebEntityJsonConverterFactory WebEntityJsonConverterFactory = null!;

    static SignumServer()
    {
        WebEntityJsonConverterFactory = new WebEntityJsonConverterFactory();
        WebEntityJsonConverterFactory.CanWritePropertyRoute += EntityJsonConverter_CanWritePropertyRoute;
    }

    public static JsonSerializerOptions JsonSerializerOptions = null!;

    public static JsonOptions AddSignumJsonConverters(this JsonOptions jsonOptions)
    {
        //Signum converters
        jsonOptions.JsonSerializerOptions.IncludeFields = true;
        jsonOptions.JsonSerializerOptions.Do(s =>
        {
            JsonSerializerOptions = s;
            s.WriteIndented = true;
            s.Converters.Add(WebEntityJsonConverterFactory);
            s.Converters.Add(new LiteJsonConverterFactory());
            s.Converters.Add(new WebMListJsonConverterFactory());
            s.Converters.Add(new JsonStringEnumConverter());
            s.Converters.Add(new ResultTableConverter());
            s.Converters.Add(new TimeSpanConverter());
            s.Converters.Add(new DateOnlyConverter());
            s.Converters.Add(new TimeOnlyConverter());

        });

        return jsonOptions;
    }

    public static MvcOptions AddSignumGlobalFilters(this MvcOptions options)
    {
        options.Filters.Add(new SignumInitializeFilterAttribute());
        options.Filters.Add(new SignumExceptionFilterAttribute());
        options.Filters.Add(new CleanThreadContextAndAssertFilter());
        options.Filters.Add(new SignumEnableBufferingFilter());
        options.Filters.Add(new SignumCurrentContextFilter());
        options.Filters.Add(new SignumTimesTrackerFilter());
        options.Filters.Add(new SignumHeavyProfilerFilter());
        options.Filters.Add(new SignumHeavyProfilerResultFilter());
        options.Filters.Add(new SignumHeavyProfilerActionFilter());
        options.Filters.Add(new SignumAuthenticationFilter());
        options.Filters.Add(new SignumCultureSelectorFilter());
        options.Filters.Add(new VersionFilterAttribute());

        return options;
    }

    public static void AddSignumValidation(this IServiceCollection services)
    {
        services.AddSingleton<IModelMetadataProvider>(s =>
        {
            var modelMetadataProvider = s.GetRequiredService<ICompositeMetadataDetailsProvider>();
            return new SignumModelMetadataProvider(modelMetadataProvider);
        });
        services.AddSingleton<IObjectModelValidator>(s =>
        {
            var options = s.GetRequiredService<IOptions<MvcOptions>>().Value;
            var modelMetadataProvider = s.GetRequiredService<IModelMetadataProvider>();
            return new SignumObjectModelValidator(modelMetadataProvider, options.ModelValidatorProviders);
        });
    }

    public static void Start(IApplicationBuilder app, IWebHostEnvironment hostingEnvironment, Assembly mainAsembly)
    {
        Schema.Current.ApplicationName = hostingEnvironment.ContentRootPath;

        SignumControllerFactory.RegisterArea(typeof(EntitiesController));
        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod()!);

        ReflectionServer.Start();
        ReflectionServer.RegisterLike(typeof(SearchMessage), () => UserHolder.Current != null);
        ReflectionServer.RegisterLike(typeof(PaginationMode), () => UserHolder.Current != null);
        ReflectionServer.OverrideIsNamespaceAllowed.Add(typeof(DayOfWeek).Namespace!, () => UserHolder.Current != null);
        ReflectionServer.OverrideIsNamespaceAllowed.Add(typeof(CollectionMessage).Namespace!, () => UserHolder.Current != null);
    }

    private static string? EntityJsonConverter_CanWritePropertyRoute(PropertyRoute arg, ModifiableEntity? mod)
    {
        var val = Entities.Validator.TryGetPropertyValidator(arg);

        if (val == null || mod == null)
            return null;

        if (val.IsPropertyReadonly(mod))
            return $"Property {arg} is readonly";

        return null;
    }

    public static EntityPackTS GetEntityPack(Entity entity)
    {
        var canExecutes = OperationLogic.ServiceCanExecute(entity);

        var result = new EntityPackTS(entity,
            canExecutes.ToDictionary(a => a.Key.Key, a => a.Value)
        );

        if (EntityPackTS.AddExtension != null)
            foreach (var action in EntityPackTS.AddExtension.GetInvocationListTyped())
            {
                try
                {
                    action(result);
                }
                catch (Exception) when (StartParameters.IgnoredDatabaseMismatches != null)
                {

                }
            }

        return result;
    }
}

public class WebMListJsonConverterFactory : MListJsonConverterFactory
{
    protected override void AssertCanWrite(PropertyRoute pr, ModifiableEntity? mod)
    {
        SignumServer.WebEntityJsonConverterFactory.AssertCanWrite(pr, mod);
    }
}

public class WebEntityJsonConverterFactory : EntityJsonConverterFactoryBase
{
    public Func<PropertyRoute, ModifiableEntity, string?>? CanReadPropertyRoute;
    public Func<PropertyRoute, ModifiableEntity?, string?>? CanWritePropertyRoute;
    public Func<PropertyInfo, Exception, string?> GetErrorMessage = (pi, ex) => "Unexpected error";

    public override void AssertCanWrite(PropertyRoute pr, ModifiableEntity? mod)
    {
        string? error = CanWritePropertyRoute.GetInvocationListTyped().Select(a => a(pr, mod)).NotNull().FirstOrDefault();
        if (error != null)
            throw new UnauthorizedAccessException(error);
    }

    protected override Type GetType(string typeString)
    {
        return TypeLogic.GetType(typeString);
    }

    protected override string GetCleanName(Type entityType)
    {
        return TypeLogic.GetCleanName(entityType);
    }

    protected override bool HasTicks(Type entityType)
    {
        return Schema.Current.Table(entityType).Ticks != null;
    }

    protected override IDisposable NewEntityCache()
    {
        return new EntityCache();
    }

    protected override bool ContinueOnError(Exception e, ModifiableEntity entity, PropertyInfo pi)
    {
        e.LogException();
        entity.SetTemporalError(pi, GetErrorMessage(pi, e));
        return true;

    }

    protected override bool CanRead(PropertyRoute route, ModifiableEntity mod)
    {
        return CanReadPropertyRoute?.Invoke(route, mod) == null;
    }

    protected override bool GetAllowDirectMListChanges()
    {
        return false;
    }

    protected override ModifiableEntity GetExistingEntity(ref Utf8JsonReader reader, IModifiableEntity? existingValue, IdentityInfo identityInfo, Type type)
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


        var retrievedEntity = Database.Retrieve(type, id);
        if (identityInfo.Ticks != null)
        {
            if (identityInfo.Modified == true && retrievedEntity.Ticks != identityInfo.Ticks.Value)
                throw new ConcurrencyException(type, id);

            retrievedEntity.Ticks = identityInfo.Ticks.Value;
        }

        return retrievedEntity;

    }

    private static bool IsEquals(object? newValue, object? oldValue)
    {
        if (newValue is byte[] nba && oldValue is byte[] oba)
            return MemoryExtensions.SequenceEqual<byte>(nba, oba);

        if (newValue is DateTime ndt && oldValue is DateTime odt)
            return Math.Abs(ndt.Subtract(odt).TotalMilliseconds) < 10; //Json dates get rounded

        if (newValue is DateTimeOffset ndto && oldValue is DateTimeOffset odto)
            return Math.Abs(ndto.Subtract(odto).TotalMilliseconds) < 10; //Json dates get rounded

        return object.Equals(newValue, oldValue);
    }

    protected override void SetProperty(PropertyConverter pc, PropertyRoute pr, ModifiableEntity entity, object? newValue, object? oldValue, bool markedAsModified)
    {
        var pi = pc.PropertyValidator!.PropertyInfo;
        if (pi.CanWrite)
        {
            if (!IsEquals(newValue, oldValue))
            {
                if (!markedAsModified && pr.Parent!.RootType.IsEntity())
                {
                    if (!pi.HasAttribute<IgnoreAttribute>())
                    {
                        try
                        {
                            //Call attention of developer
                            throw new InvalidOperationException($"'modified' is not set but '{pi.Name}' is modified");
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                else
                {
                    AssertCanWrite(pr, entity);
                    if (newValue == null && pc.IsNotNull())
                    {
                        entity.SetTemporalError(pi, ValidationMessage._0IsNotSet.NiceToString(pi.NiceName()));
                        return;
                    }

                    pc.SetValue?.Invoke(entity, newValue);
                }
            }
        }
    }

    protected override void CleanModificationsIsNecessary(ModifiableEntity mod)
    {
        return;
    }
    protected override PropertyRoute GetCurrentPropertyRouteEmbedded(EmbeddedEntity embedded)
    {
        var controller = ((ControllerActionDescriptor)SignumCurrentContextFilter.CurrentContext!.ActionDescriptor);
        var att =
            controller.MethodInfo.GetCustomAttribute<EmbeddedPropertyRouteAttribute>() ??
            controller.MethodInfo.DeclaringType!.GetCustomAttribute<EmbeddedPropertyRouteAttribute>() ??
            throw new InvalidOperationException(@$"Impossible to determine PropertyRoute for {embedded.GetType().Name}. 
        Consider adding someting like [EmbeddedPropertyRoute(typeof({embedded.GetType().Name}), typeof(SomeEntity), nameof(SomeEntity.SomeProperty))] to your action or controller.
        Current action: {controller.MethodInfo.MethodSignature()}
        Current controller: {controller.MethodInfo.DeclaringType!.FullName}");

        return att.PropertyRoute;
    }

    public override Type ResolveType(string typeStr, Type objectType)
    {
        if (Reflector.CleanTypeName(objectType) == typeStr)
            return objectType;

        var type = ReflectionServer.TypesByName.Value.GetOrThrow(typeStr);

        if (type.IsEnum)
            type = EnumEntity.Generate(type);

        if (!objectType.IsAssignableFrom(type))
            throw new JsonException($"Type '{type.Name}' is not assignable to '{objectType.TypeName()}'");

        return type;
    }
}

[System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
public class EmbeddedPropertyRouteAttribute : Attribute
{

    public Type EmbeddedType { get; private set; }
    public PropertyRoute PropertyRoute { get; private set; }
    // This is a positional argument
    public EmbeddedPropertyRouteAttribute(Type embeddedType, Type propertyRouteRoot, string propertyRouteText)
    {
        this.EmbeddedType = embeddedType;
        this.PropertyRoute = PropertyRoute.Parse(propertyRouteRoot, propertyRouteText);
    }
}
