using System.ComponentModel.DataAnnotations;
using Signum.React.ApiControllers;
using Signum.Entities.UserAssets;
using Signum.React.Files;
using Signum.Engine.UserAssets;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Signum.React.Filters;
using System.Collections.ObjectModel;
using Signum.React.Facades;
using System.Text.Json;

namespace Signum.React.UserAssets;

[ValidateModelFilter]
public class UserAssetController : ControllerBase
{
    public class ParseFiltersRequest
    {
        public string queryKey;
        public bool canAggregate;
        public List<QueryFilterItem> filters;
        public Lite<Entity> entity;
    }

    [HttpPost("api/userAssets/parseFilters")]
    public List<FilterNode> ParseFilters([Required, FromBody]ParseFiltersRequest request)
    {
        var queryName = QueryLogic.ToQueryName(request.queryKey);
        var qd = QueryLogic.Queries.QueryDescription(queryName);
        var options = SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | (request.canAggregate ? SubTokensOptions.CanAggregate : 0);

        using (request.entity != null ? CurrentEntityConverter.SetCurrentEntity(request.entity.RetrieveAndRemember()) : null)
        {
            var result = ParseFilterInternal(request.filters, qd, options, 0).ToList();

            return result;
        }
    }

    static List<FilterNode> ParseFilterInternal(IEnumerable<QueryFilterItem> filters, QueryDescription qd, SubTokensOptions options, int indent)
    {
        return filters.GroupWhen(filter => filter.indentation == indent).Select(gr =>
        {
            if (!gr.Key.isGroup)
            {
                if (gr.Count() != 0)
                    throw new InvalidOperationException("Unexpected childrens of condition");

                var filter = gr.Key;

                var token = QueryUtils.Parse(filter.tokenString!, qd, options);

                var value = FilterValueConverter.Parse(filter.valueString, token.Type, filter.operation!.Value.IsList());

                return new FilterNode
                {
                    token = token.ToQueryTokenTS(true),
                    operation = filter.operation.Value,
                    value = value,
                    pinned = filter.pinned,
                    dashboardBehaviour = filter.dashboardBehaviour,
                };
            }
            else
            {
                var group = gr.Key;

                var token = group.tokenString == null ? null : QueryUtils.Parse(group.tokenString!, qd, options);

                var value = FilterValueConverter.Parse(group.valueString, typeof(string), false);

                return new FilterNode
                {
                    groupOperation = group.groupOperation!.Value,
                    token = token == null ? null : token.ToQueryTokenTS(true),
                    pinned = group.pinned,
                    dashboardBehaviour = group.dashboardBehaviour,
                    filters = ParseFilterInternal(gr, qd, options, indent + 1).ToList()
                };
            }
        }).ToList();
    }

    public class StringifyFiltersRequest
    {
        public string queryKey;
        public bool canAggregate;
        public List<FilterNode> filters;
    }

    [HttpPost("api/userAssets/stringifyFilters")]
    public List<QueryFilterItem> StringifyFilters([Required, FromBody]StringifyFiltersRequest request)
    {
        var queryName = QueryLogic.ToQueryName(request.queryKey);
        var qd = QueryLogic.Queries.QueryDescription(queryName);
        var options = SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | (request.canAggregate ? SubTokensOptions.CanAggregate : 0);

        List<QueryFilterItem> result = new List<QueryFilterItem>();
        foreach (var f in request.filters)
        {
            result.AddRange(ToQueryFiltersEmbedded(f, qd, options, 0));
        }

        return result;
    }

    public static IEnumerable<QueryFilterItem> ToQueryFiltersEmbedded(FilterNode filter, QueryDescription qd, SubTokensOptions options, int ident = 0)
    {
        if (filter.groupOperation == null)
        {
            var token = QueryUtils.Parse(filter.tokenString!, qd, options);

            var expectedValueType = filter.operation!.Value.IsList() ? typeof(ObservableCollection<>).MakeGenericType(token.Type.Nullify()) : token.Type;
            
            var val = filter.value is JsonElement jtok ?
                 jtok.ToObject(expectedValueType, SignumServer.JsonSerializerOptions) :
                 filter.value;
            
            yield return new QueryFilterItem
            {
                token = token.ToQueryTokenTS(true),
                operation = filter.operation,
                valueString = FilterValueConverter.ToString(val, token.Type),
                indentation = ident,
                pinned = filter.pinned,
            };
        }
        else
        {
            var token = filter.tokenString == null ? null : QueryUtils.Parse(filter.tokenString, qd, options);


            yield return new QueryFilterItem
            {
                isGroup = true,
                groupOperation = filter.groupOperation,
                token = token == null ? null : token.ToQueryTokenTS(true),
                indentation = ident,
                valueString = filter.value != null ? FilterValueConverter.ToString(filter.value, typeof(string)) : null,
                pinned = filter.pinned,
            };

            foreach (var f in filter.filters)
            {
                foreach (var fe in ToQueryFiltersEmbedded(f, qd, options, ident + 1))
                {
                    yield return fe;
                }
            }
        }
    }

    public class QueryFilterItem
    {
        public QueryTokenTS? token;
        public string? tokenString;
        public bool isGroup;
        public FilterGroupOperation? groupOperation;
        public FilterOperation? operation;
        public string? valueString;
        public PinnedFilter pinned;
        public DashboardBehaviour? dashboardBehaviour; 
        public int indentation;
    }

    public class PinnedFilter
    {
        public string label;
        public int? row;
        public int? column;
        public PinnedFilterActive? active;
        public bool? splitText;
    }

    public class FilterNode
    {
        public FilterGroupOperation? groupOperation;
        public string? tokenString; //For Request
        public QueryTokenTS? token; //For response
        public FilterOperation? operation;
        public object? value;
        public List<FilterNode> filters;
        public PinnedFilter pinned;
        public DashboardBehaviour? dashboardBehaviour;
    }

    public class FilterElement
    {

    }
    
    [HttpPost("api/userAssets/export")]
    public FileStreamResult Export([Required, FromBody]Lite<IUserAssetEntity>[] lites)
    {
        var bytes = UserAssetsExporter.ToXml(lites.RetrieveFromListOfLite().ToArray());

        string typeName = lites.Select(a => a.EntityType).Distinct().SingleEx().Name;
        var fileName = "{0}{1}.xml".FormatWith(typeName, lites.ToString(a => a.Id.ToString(), "_"));

        return FilesController.GetFileStreamResult(new MemoryStream(bytes), fileName);
    }

    [HttpPost("api/userAssets/importPreview")]
    public UserAssetPreviewModel ImportPreview([Required, FromBody]FileUpload file)
    {
        return UserAssetsImporter.Preview(file.content);
    }

    [HttpPost("api/userAssets/import")]
    public void Import([Required, FromBody]FileUploadWithModel file)
    {
        UserAssetsImporter.Import(file.file.content, file.model);
    }

    public class FileUpload
    {
        public string fileName;
        public byte[] content;
    }

    public class FileUploadWithModel
    {
        public FileUpload file;
        public UserAssetPreviewModel model;
    }
}
