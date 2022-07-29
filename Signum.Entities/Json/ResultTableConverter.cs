using Signum.Entities.DynamicQuery;
using Signum.Utilities.Reflection;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signum.Entities.Json;

public class ResultTableConverter : JsonConverter<ResultTable>
{
    public override void Write(Utf8JsonWriter writer, ResultTable value, JsonSerializerOptions options)
    {
        using (HeavyProfiler.LogNoStackTrace("ReadJson", () => typeof(ResultTable).Name))
        {
            var rt = (ResultTable)value!;

            writer.WriteStartObject();

            writer.WritePropertyName("columns");
            writer.WriteStartArray();
            foreach (var rc in rt.Columns)
            {
                writer.WriteStringValue(rc.Column.Token.FullKey());
            }
            writer.WriteEndArray();

            Dictionary<ResultColumn, List<int?>> uniqueValueIndexes = new Dictionary<ResultColumn, List<int?>>();

            writer.WritePropertyName("uniqueValues");
            writer.WriteStartObject();
            foreach (var rc in rt.Columns.Where(a => a.CompressUniqueValues).DistinctBy(rc => rc.Column.Token))
            {
                writer.WritePropertyName(rc.Column.Token.FullKey());
                {
                    var pair = giUniqueValues.GetInvoker(rc.Column.Token.Type)(rc.Values);

                    using (EntityJsonContext.SetCurrentPropertyRouteAndEntity((rc.Column.Token.GetPropertyRoute()!, null, null)))
                    {
                        JsonSerializer.Serialize(writer, pair.UniqueValues, pair.UniqueValues.GetType(), options);
                    }

                    uniqueValueIndexes.Add(rc, pair.Indexes);
                }
            }
            writer.WriteEndObject();

            writer.WritePropertyName("pagination");
            JsonSerializer.Serialize(writer, new PaginationTableTS(rt.Pagination), typeof(PaginationTableTS), options);

            writer.WritePropertyName("totalElements");
            if (rt.TotalElements == null)
                writer.WriteNullValue();
            else
                writer.WriteNumberValue(rt.TotalElements!.Value);


            writer.WritePropertyName("rows");
            writer.WriteStartArray();
            foreach (var row in rt.Rows)
            {
                writer.WriteStartObject();
                if (rt.EntityColumn != null)
                {
                    writer.WritePropertyName("entity");
                    JsonSerializer.Serialize(writer, row.Entity, options);
                }

                writer.WritePropertyName("columns");
                writer.WriteStartArray();
                foreach (var column in rt.Columns)
                {
                    if (uniqueValueIndexes.TryGetValue(column, out var indexes))
                    {
                        var ix = indexes[row.Index];
                        if (ix != null)
                            writer.WriteNumberValue(ix.Value);
                        else
                            writer.WriteNullValue();
                    }
                    else
                    {
                        using (EntityJsonContext.SetCurrentPropertyRouteAndEntity((column.Column.Token.GetPropertyRoute()!, null, null)))
                        {
                            JsonSerializer.Serialize(writer, row[column], options);
                        }
                    }
                }
                writer.WriteEndArray();


                writer.WriteEndObject();

            }
            writer.WriteEndArray();


            writer.WriteEndObject();
        }
    }

    public class PaginationTableTS
    {
        public PaginationMode mode;
        public int? elementsPerPage;
        public int? currentPage;

        public PaginationTableTS() { }

        public PaginationTableTS(Pagination pagination)
        {
            this.mode = pagination.GetMode();
            this.elementsPerPage = pagination.GetElementsPerPage();
            this.currentPage = (pagination as Pagination.Paginate)?.CurrentPage;
        }
    }

    interface IUniqueValuesPair
    {
        Array UniqueValues { get; }
        List<int?> Indexes { get; }
    }

    class UniqueValuesPair<T> : IUniqueValuesPair
    {
        public UniqueValuesPair(T[] uniqueValues, List<int?> indexes)
        {
            this.UniqueValues = uniqueValues;
            this.Indexes = indexes;
        }

        public T[] UniqueValues { get; }
        public List<int?> Indexes { get; }

        Array IUniqueValuesPair.UniqueValues => UniqueValues;
    }

    static GenericInvoker<Func<IList, IUniqueValuesPair>> giUniqueValues =
        new(list => UniqueValues<string>((string[])list));

    static UniqueValuesPair<T> UniqueValues<T>(T[] list) where T : notnull
    {
        List<int?> indexes = new List<int?>(list.Length);
        Dictionary<T, int> uniqueDic = new Dictionary<T, int>();
        foreach (var item in list)
        {
            int? idx = item == null ? null : uniqueDic.GetOrCreate(item, uniqueDic.Count);

            indexes.Add(idx);
        }

        var uniqueValues = new T[uniqueDic.Count];
        foreach (var kvp in uniqueDic)
        {
            uniqueValues[kvp.Value] = kvp.Key;
        }

        return new UniqueValuesPair<T>(uniqueValues, indexes);
    }

    public override ResultTable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
