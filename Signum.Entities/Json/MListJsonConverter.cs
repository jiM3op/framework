using Signum.Entities;
using Signum.Entities.Json;
using Signum.Utilities.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signum.Entities.Json;

interface IJsonConverterWithExisting
{
    object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, object? existingValue);
}

public abstract class JsonConverterWithExisting<T> : JsonConverter<T>, IJsonConverterWithExisting
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Read(ref reader, typeToConvert, options, default);

    public object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, object? existingValue) =>
        Read(ref reader, typeToConvert, options, (T?)existingValue);

    public abstract T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, T? existingValue);
}

public abstract class MListJsonConverterFactoryBase : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(IMListPrivate).IsAssignableFrom(typeToConvert);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)Activator.CreateInstance(typeof(MListJsonConverter<>).MakeGenericType(typeToConvert.ElementType()!), options, this)!;
    }

    protected internal abstract void AssertCanWrite(PropertyRoute pr, ModifiableEntity? mod);

    protected internal abstract Type GetRowIdTypeFromAttribute(PropertyRoute route);

    protected internal abstract bool GetPreserveOrderFromAttribute(PropertyRoute route);
}



public class MListJsonConverter<T> : JsonConverterWithExisting<MList<T>>
{
    readonly MListJsonConverterFactoryBase Factory;

    readonly JsonConverter<T> converter;
    public MListJsonConverter(JsonSerializerOptions options, MListJsonConverterFactoryBase factory)
    {
        this.converter = (JsonConverter<T>)options.GetConverter(typeof(T));
        Factory = factory;
    }

    public override void Write(Utf8JsonWriter writer, MList<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        var (pr, mod, rowId) = EntityJsonContext.CurrentPropertyRouteAndEntity!.Value;

        var elementPr = pr.Add("Item");

        foreach (var item in ((IMListPrivate<T>)value).InnerList)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("rowId");
            JsonSerializer.Serialize(writer, item.RowId?.Object, item.RowId?.Object.GetType() ?? typeof(object), options);

            writer.WritePropertyName("element");
            using (EntityJsonContext.SetCurrentPropertyRouteAndEntity((elementPr, mod, item.RowId)))
            {
                JsonSerializer.Serialize(writer, item.Element, item.Element?.GetType() ?? typeof(T), options);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    public override MList<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, MList<T>? existingValue)
    {

        var existingMList = (IMListPrivate<T>?)existingValue;

        var dic = existingMList == null ? new Dictionary<PrimaryKey, MList<T>.RowIdElement>() :
             existingMList.InnerList.Where(a => a.RowId.HasValue).ToDictionary(a => a.RowId!.Value, a => a);

        var newList = new List<MList<T>.RowIdElement>();

        var tup = EntityJsonContext.CurrentPropertyRouteAndEntity!.Value;

        var elementPr = tup.pr.Add("Item");

        var rowIdType = this.Factory.GetRowIdTypeFromAttribute(tup.pr);

        reader.Assert(JsonTokenType.StartArray);

        reader.Read();

        while (reader.TokenType == JsonTokenType.StartObject)
        {
            reader.Read();
            reader.Assert(JsonTokenType.PropertyName);
            if (reader.GetString() != "rowId")
                throw new JsonException($"member 'rowId' expected in {reader.CurrentState}");

            reader.Read();
            var rowIdValue = reader.GetLiteralValue();

            reader.Read();
            reader.Assert(JsonTokenType.PropertyName);
            if (reader.GetString() != "element")
                throw new JsonException($"member 'element' expected in {reader.CurrentState}");

            reader.Read();

            using (EntityJsonConverterFactoryBase.SetPath($"[{newList.Count}].element"))
            {
                if (rowIdValue != null && !rowIdValue.Equals(MListExtensions.DummyRowId.Object))
                {
                    var rowId = new PrimaryKey((IComparable)ReflectionTools.ChangeType(rowIdValue, rowIdType)!);

                    var oldValue = dic.TryGetS(rowId);

                    using (EntityJsonContext.SetCurrentPropertyRouteAndEntity((elementPr, tup.mod, rowId)))
                    {
                        if (oldValue == null)
                        {
                            T newValue = (T)converter.Read(ref reader, typeof(T), options)!;

                            newList.Add(new MList<T>.RowIdElement(newValue, rowId, null));
                        }
                        else
                        {
                            T newValue = converter is JsonConverterWithExisting<T> jcwe ?
                                (T)jcwe.Read(ref reader, typeof(T), options, oldValue.Value.Element!)! :
                                (T)converter.Read(ref reader, typeof(T), options)!;

                            if (oldValue.Value.Element!.Equals(newValue))
                                newList.Add(new MList<T>.RowIdElement(newValue, rowId, oldValue.Value.OldIndex));
                            else
                                newList.Add(new MList<T>.RowIdElement(newValue));
                        }
                    }
                }
                else
                {
                    using (EntityJsonContext.SetCurrentPropertyRouteAndEntity((elementPr, tup.mod, null)))
                    {
                        var newValue = (T)converter.Read(ref reader, typeof(T), options)!;
                        newList.Add(new MList<T>.RowIdElement(newValue));
                    }
                }
            }

            reader.Read();
            reader.Assert(JsonTokenType.EndObject);
            reader.Read();
        }

        reader.Assert(JsonTokenType.EndArray);

        if (existingMList == null) //Strange case...
        {
            if (newList.IsEmpty())
                return null!;
            else
                existingMList = new MList<T>();
        }

        bool orderMatters = this.Factory.GetPreserveOrderFromAttribute(tup.pr);

        if (!existingMList.IsEqualTo(newList, orderMatters))
        {
            if (!EntityJsonContext.AllowDirectMListChanges)
                return new MList<T>(newList);

            this.Factory.AssertCanWrite(tup.pr, tup.mod);

            existingMList.AssignMList(newList);
        }

        return (MList<T>)existingMList;
    }
}


