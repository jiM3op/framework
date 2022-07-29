using Signum.Engine.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.React.ApiControllers;
using Signum.Utilities.Reflection;
using System.Collections;
using System.Text.Json;

namespace Signum.React.Facades;

internal static class MultiSetter
{
    public static void SetSetters(ModifiableEntity entity, List<PropertySetter> setters, PropertyRoute route)
    {
        var options = SignumServer.JsonSerializerOptions;

        foreach (var setter in setters)
        {
            var pr = route.AddMany(setter.Property);

            SignumServer.WebEntityJsonConverterFactory.AssertCanWrite(pr, entity);

            if (pr.Type.IsMList())
            {
                var elementPr = pr.Add("Item");
                var mlist = pr.GetLambdaExpression<ModifiableEntity, IMListPrivate>(false).Compile()(entity);
                switch (setter.Operation)
                {
                    case PropertyOperation.AddElement:
                        {
                            var value = ConvertObject(setter.Value, elementPr, options);
                            ((IList)mlist).Add(value);
                        }
                        break;
                    case PropertyOperation.AddNewElement:
                        {
                            var item = (ModifiableEntity)Activator.CreateInstance(elementPr.Type)!;
                            SetSetters(item, setter.Setters!, elementPr);
                            ((IList)mlist).Add(item);
                        }
                        break;
                    case PropertyOperation.ChangeElements:
                        {
                            var predicate = GetPredicate(setter.Predicate!, elementPr, options);
                            var toChange = ((IEnumerable<object>)mlist).Where(predicate.Compile()).ToList();
                            foreach (var item in toChange)
                            {
                                SetSetters((ModifiableEntity)item, setter.Setters!, elementPr);
                            }
                        }
                        break;
                    case PropertyOperation.RemoveElementsWhere:
                        {
                            var predicate = GetPredicate(setter.Predicate!, elementPr, options);
                            var toRemove = ((IEnumerable<object>)mlist).Where(predicate.Compile()).ToList();
                            foreach (var item in toRemove)
                            {
                                ((IList)mlist).Remove(item);
                            }
                        }
                        break;
                    case PropertyOperation.RemoveElement:
                        {
                            var value = ConvertObject(setter.Value, elementPr, options);
                            ((IList)mlist).Remove(value);
                        }
                        break;
                    default:
                        break;
                }
            }
            else if (setter.Operation == PropertyOperation.CreateNewEntity)
            {
                var subPr = pr.Type.IsEmbeddedEntity() ? pr : PropertyRoute.Root(TypeLogic.GetType(setter.EntityType!));
                var item = (ModifiableEntity)Activator.CreateInstance(subPr.Type)!;
                SetSetters(item, setter.Setters!, subPr);
                SetProperty(entity, pr, route, item);
            }
            else if (setter.Operation == PropertyOperation.ModifyEntity)
            {
                var item = GetProperty(entity, pr, route);
                if (!(item is ModifiableEntity mod))
                    throw new InvalidOperationException($"Unable to change entity in {pr}: {item}");

                SetSetters(mod, setter.Setters!, pr);
                SetProperty(entity, pr, route, mod);
            }
            else if (setter.Operation == PropertyOperation.Set)
            {
                var value = ConvertObject(setter.Value, pr, options);
                SetProperty(entity, pr, route, value);
            }
            else
            {
                throw new UnexpectedValueException(setter.Operation);
            }
        }
    }

    private static void SetProperty(ModifiableEntity entity, PropertyRoute pr, PropertyRoute parentRoute, object? value)
    {
        var subEntity = pr.Parent == parentRoute ? entity :
                    (ModifiableEntity)pr.Parent!.GetLambdaExpression<object, object>(true, parentRoute).Compile()(entity);

        pr.PropertyInfo!.SetValue(subEntity, value);
    }

    private static object? GetProperty(ModifiableEntity entity, PropertyRoute pr, PropertyRoute parentRoute)
    {
        var subEntity = pr.Parent == parentRoute ? entity :
                    (ModifiableEntity)pr.Parent!.GetLambdaExpression<object, object>(true, parentRoute).Compile()(entity);

        return pr.PropertyInfo!.GetValue(subEntity);
    }


    static Expression<Func<object, bool>> GetPredicate(List<PropertySetter> predicate, PropertyRoute mainRoute, JsonSerializerOptions options)
    {
        var param = Expression.Parameter(typeof(object), "p");

        var body = predicate.Select(p =>
        {
            var pr = mainRoute.AddMany(p.Property);

            var lambda = pr.GetLambdaExpression<object, object>(true, mainRoute.GetMListItemsRoute());

            var left = Expression.Invoke(lambda, param);
            object? objClean = ConvertObject(p.Value, pr, options);

            return QueryUtils.GetCompareExpression(p.FilterOperation!.Value, left, Expression.Constant(objClean), inMemory: true);

        }).Aggregate((a, b) => Expression.AndAlso(a, b));

        return Expression.Lambda<Func<object, bool>>(body, param);
    }

    private static object? ConvertObject(object? value, PropertyRoute pr, JsonSerializerOptions options)
    {
        var objRaw = value == null ? null :
                        value is JsonElement elem ? elem.ToObject(pr.Type, options) :
                        value;

        var objClean = ReflectionTools.ChangeType(objRaw, pr.Type);
        return objClean;
    }
}
