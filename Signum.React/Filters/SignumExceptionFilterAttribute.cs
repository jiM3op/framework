using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Signum.Engine.Basics;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using Signum.React.Facades;
using System.Security.Authentication;
using System.Text.Json;
using System.Threading.Tasks;

namespace Signum.React.Filters;

public class SignumExceptionFilterAttribute : IAsyncResourceFilter
{
    public static Func<Exception, bool> TranslateExceptionMessage = ex => ex is ApplicationException;

    public static Func<Exception, bool> IncludeErrorDetails = ex => true;

    public static readonly List<Type> AvoidLogException = new() { typeof(OperationCanceledException) };

    public static Func<Exception, HttpError> CustomHttpErrorFactory = ex => ToHttpError(ex);

    public static Action<ResourceExecutedContext, ExceptionEntity>? ApplyMixins = null;

    public async Task OnResourceExecutionAsync(ResourceExecutingContext precontext, ResourceExecutionDelegate next)
    {
        //Eagerly reading the whole body just in case to avoid "Cannot access a disposed object" 
        //TODO: Make it more eficiently when https://github.com/aspnet/AspNetCore/issues/14396
        var body = ReadAllBody(precontext.HttpContext);

        var context = await next();

        if (context.Exception != null)
        {
            if (!AvoidLogException.Contains(context.Exception.GetType()))
            {
                var req = context.HttpContext.Request;

                var connFeature = context.HttpContext.Features.Get<IHttpConnectionFeature>()!;

                var exLog = context.Exception.LogException(e =>
                {
                    e.ActionName = Try(100, () => (context.ActionDescriptor as ControllerActionDescriptor)?.ActionName);
                    e.ControllerName = Try(100, () => (context.ActionDescriptor as ControllerActionDescriptor)?.ControllerName);
                    e.UserAgent = Try(300, () => req.Headers["User-Agent"].FirstOrDefault());
                    e.RequestUrl = Try(int.MaxValue, () => req.GetDisplayUrl());
                    e.UrlReferer = Try(int.MaxValue, () => req.Headers["Referer"].ToString());
                    e.UserHostAddress = Try(100, () => connFeature.RemoteIpAddress?.ToString());
                    e.UserHostName = Try(100, () => connFeature.RemoteIpAddress == null ? null : Dns.GetHostEntry(connFeature.RemoteIpAddress).HostName);
                    e.User = UserHolder.Current?.User ?? ((UserWithClaims?)context.HttpContext.Items[SignumAuthenticationFilter.Signum_User_Holder_Key])?.User ?? e.User;
                    e.QueryString = new BigStringEmbedded(Try(int.MaxValue, () => req.QueryString.ToString()));
                    e.Form = new BigStringEmbedded(Try(int.MaxValue, () => Encoding.UTF8.GetString(body)));
                    e.Session = new BigStringEmbedded();
                    ApplyMixins?.Invoke(context, e);
                });

                if (ExpectsJsonResult(context))
                {
                    var statusCode = GetStatus(context.Exception.GetType());
                    var error = CustomHttpErrorFactory(context.Exception);

                    var ci = TranslateExceptionMessage(context.Exception) ? SignumCultureSelectorFilter.GetCurrentCulture?.Invoke(precontext) : null;

                    using (ci == null ? null : CultureInfoUtils.ChangeBothCultures(ci))
                    {
                        var response = context.HttpContext.Response;
                        response.StatusCode = (int)statusCode;
                        response.ContentType = "application/json";

                        var userWithClaims = (UserWithClaims?)context.HttpContext.Items[SignumAuthenticationFilter.Signum_User_Holder_Key];

                        using (UserHolder.Current == null && userWithClaims != null ? UserHolder.UserSession(userWithClaims) : null)
                        {
                            await response.WriteAsync(JsonSerializer.Serialize(error, SignumServer.JsonSerializerOptions));
                        }
                        context.ExceptionHandled = true;
                    }
                }
            }
        }
    }

    private static string? Try(int size, Func<string?> getValue)
    {
        try
        {
            return getValue()?.TryStart(size);
        }
        catch (Exception e)
        {
            return (e.GetType().Name + ":" + e.Message).TryStart(size);
        }
    }

    public static Func<ResourceExecutedContext, bool> ExpectsJsonResult = (context) =>
    {
        if (context.ActionDescriptor is ControllerActionDescriptor cad)
        {
            return !typeof(IActionResult).IsAssignableFrom(cad.MethodInfo.ReturnType) ||
                typeof(FileResult).IsAssignableFrom(cad.MethodInfo.ReturnType) && context.HttpContext.Request.Method != "GET";

        }
        return false;
    };

    public byte[] ReadAllBody(HttpContext httpContext)
    {
        httpContext.Request.EnableBuffering();
        var result = httpContext.Request.Body.ReadAllBytes();
        httpContext.Request.Body.Seek(0, System.IO.SeekOrigin.Begin);
        return result;
    }

    private static HttpStatusCode GetStatus(Type type)
    {
        if (type == typeof(UnauthorizedAccessException))
            return HttpStatusCode.Forbidden;

        if (type == typeof(AuthenticationException))
            return HttpStatusCode.Forbidden; // Unauthorized produces Login Password dialog in Mixed mode

        if (type == typeof(EntityNotFoundException))
            return HttpStatusCode.NotFound;

        if (type == typeof(IntegrityCheckException))
            return HttpStatusCode.BadRequest;

        return HttpStatusCode.InternalServerError;
    }

    public static HttpError ToHttpError(Exception e, bool includeErrorDetails = true)
    {
        var result = new HttpError
        {
            ExceptionMessage = e.Message,
            ExceptionType = e.GetType().FullName!,
            Model = e is ModelRequestedException mre ? mre.Model : null,
        };

        if (includeErrorDetails)
        {
            result.ExceptionId = e.GetExceptionEntity()?.Id.ToString();
            result.StackTrace = e.StackTrace;
            result.InnerException = e.InnerException == null ? null : ToHttpError(e.InnerException);
        }

        return result;
    }
}

public class SignumInitializeFilterAttribute : IAsyncResourceFilter
{
    public static Action InitializeDatabase = () => throw new InvalidOperationException("SignumInitializeFilterAttribute.InitializeDatabase should be set in Startup");
    static object lockKey = new();
    public bool Initialized = false;

    public Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        if (!Initialized)
        {
            lock (lockKey)
            {
                if (!Initialized)
                {
                    InitializeDatabase();
                    Initialized = true;
                }
            }
        }

        return next();
    }
}
