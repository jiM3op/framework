using Signum.Engine.Basics;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.React.Facades;

internal class LambdaToJavascriptConverter
{
    public static string? ToJavascript(LambdaExpression lambdaExpression)
    {
        if (lambdaExpression == null)
            return null;

        var newLambda = (LambdaExpression)ExpressionCleaner.Clean(lambdaExpression)!;

        var body = ToJavascript(newLambda.Parameters.Single(), newLambda.Body);

        if (body == null)
            return null;

        return "return " + body;
    }

    static Dictionary<string, string> replacements = new Dictionary<string, string>
    {
        { "\t", "\\t"},
        { "\n", "\\n"},
        { "\r", ""},
    };

    private static string? ToJavascript(ParameterExpression param, Expression expr)
    {
        if (param == expr)
            return "e";

        if (expr is ConstantExpression ce)
        {
            if (expr.Type == typeof(string))
            {
                var str = (string)ce.Value!;

                if (!str.HasText())
                    return "\"\"";

                return "\"" + str.Replace(replacements) + "\"";
            }

            if(ce.Value == null)
            {
                return "null";
            }
        }

        if (expr is MemberExpression me)
        {
            var a = ToJavascript(param, me.Expression!);

            if (a != null)
            {
                if (me.Expression!.Type.IsNullable())
                {
                    if (me.Member.Name == "HasValue")
                        return a + " != null";
                    else if (me.Member.Name == "Value")
                        return a;
                }

                if (me.Expression.Type.IsEntity())
                {
                    if (me.Member.Name == "IdOrNull")
                        return a + "." + "id";
                }

                return a + "." + me.Member.Name.FirstLower();
            }
        }

        if(expr is ConditionalExpression cond)
        {
            var test = ToJavascript(param, cond.Test);
            var ifTrue = ToJavascript(param, cond.IfTrue);
            var ifFalse = ToJavascript(param, cond.IfFalse);

            if (test != null && ifTrue != null && ifFalse != null)
                return "(" + test + " ? " + ifTrue + " : " + ifFalse + ")";
        }

        if (expr is BinaryExpression be)
        {
            if (be.NodeType == ExpressionType.Add && be.Type == typeof(string))
            {

                var a = ToJavascriptToString(param, be.Left);
                var b = ToJavascriptToString(param, be.Right);

                if (a != null && b != null)
                    return "(" + a + " + " + b + ")";
            }
            else if (be.NodeType == ExpressionType.Add)
            {

                var a = ToJavascript(param, be.Left);
                var b = ToJavascript(param, be.Right);

                if (a != null && b != null)
                    return "(" + a + " + " + b + ")";
            }
            else
            {
                var a = ToJavascript(param, be.Left);
                var b = ToJavascript(param, be.Right);

                var op = ToJsOperator(be.NodeType);

                if (a != null && op != null && b != null)
                    return a + op + b;
            }
        }

        if (expr is MethodCallExpression mc)
        {
            if (mc.Method.Name == "ToString")
                return ToJavascriptToString(param, mc.Object!, mc.TryGetArgument("format") is ConstantExpression format ? (string)format.Value! : null);

            if (mc.Method.Name == "GetType" && mc.Object != null && (mc.Object.Type.IsIEntity() || mc.Object.Type.IsModelEntity()))
            {
                var obj = ToJavascript(param, mc.Object!);
                if (obj != null)
                    return "fd.getTypeInfo(" + obj + ")";
            }

            if (mc.Method.Name == nameof(string.Format) && mc.Method.DeclaringType == typeof(string) ||
                mc.Method.Name == nameof(StringExtensions.FormatWith) && mc.Method.DeclaringType == typeof(StringExtensions))
            {
                var format = (mc.Object ?? mc.GetArgument("format"));

                var args = mc.TryGetArgument("args")?.Let(a => ((NewArrayExpression)a).Expressions) ??
                    new[] { mc.TryGetArgument("arg0"), mc.TryGetArgument("arg1"), mc.TryGetArgument("arg2"), mc.TryGetArgument("arg3") }.NotNull().ToReadOnly();

                var strFormat = ToJavascriptToString(param, format);
                var arguments = args.Select(a => ToJavascriptToString(param, a)).ToList();

                if (strFormat != null && !arguments.Contains(null))
                    return $"{strFormat}.formatWith(" + arguments.ToString(", ") + ")";
            }

            if (mc.Method.IsExtensionMethod() && mc.Arguments.Only()?.Type == typeof(Type))
            {
                var obj = ToJavascript(param, mc.Arguments.SingleEx());
                if (obj != null)
                {
                    if (mc.Method.Name == nameof(DescriptionManager.NiceName))
                        return obj + ".niceName";


                    if (mc.Method.Name == nameof(DescriptionManager.NicePluralName))
                        return obj + ".nicePluralName";

                    if (mc.Method.Name == nameof(Reflector.NewNiceName))
                        return "fd.newNiceName(" + obj + ")";

                  
                }
            }

            if (mc.Method.Name == nameof(Symbol.NiceToString))
            {
                if(mc.Object != null && (typeof(Symbol).IsAssignableFrom(mc.Object.Type) || typeof(SemiSymbol).IsAssignableFrom(mc.Object.Type)))
                {
                    var obj = ToJavascript(param, mc.Object!);
                    if (obj != null)
                        return "fd.getToString(" + obj + ")";
                }
            }


            if (mc.Method.DeclaringType == typeof(DateTime))
            {
                switch (mc.Method.Name)
                {
                    case nameof(DateTime.ToShortDateString): return ToJavascriptToString(param, mc.Object!, "d");
                    case nameof(DateTime.ToShortTimeString): return ToJavascriptToString(param, mc.Object!, "t");
                    case nameof(DateTime.ToLongDateString): return ToJavascriptToString(param, mc.Object!, "D");
                    case nameof(DateTime.ToLongTimeString): return ToJavascriptToString(param, mc.Object!, "T");
                }
            }

            if (mc.Method.DeclaringType == typeof(DateOnly))
            {
                switch (mc.Method.Name)
                {
                    case  nameof(DateOnly.ToShortDateString): return ToJavascriptToString(param, mc.Object!, "d");
                    case  nameof(DateOnly.ToLongDateString): return ToJavascriptToString(param, mc.Object!, "D");
                    case  nameof(DateOnly.ToString): return ToJavascriptToString(param, mc.Object!, "d");
                }
            }

            if (mc.Method.DeclaringType == typeof(StringExtensions) && mc.Method.Name == nameof(StringExtensions.Etc))
            {
                var str = ToJavascriptToString(param, mc.GetArgument("str"));
                var max = ((ConstantExpression)mc.GetArgument("max")).Value!.ToString();

                var etcString = mc.TryGetArgument("etcString");

                if (etcString == null)
                    return $"{str}.etc({max})";
                else
                    return $"{str}.etc({max},{ToJavascriptToString(param, etcString)})";
            }
        }

        if (expr is UnaryExpression ue && ue.NodeType == ExpressionType.Convert)
        {
            return ToJavascriptToString(param, ue.Operand);
        }

        if (expr is ConditionalExpression iff)
        {
            var t = ToJavascript(param, iff.Test);
            var a = ToJavascriptToString(param, iff.IfTrue);
            var b = ToJavascriptToString(param, iff.IfFalse);

            if (t != null && a != null && b != null)
                return "(" + t + " ? " + a + " : " + b + ")";
        }

        if(expr is MemberInitExpression mie && 
            mie.NewExpression.Arguments.Count == 0 && 
            mie.Bindings.All(b => b is MemberAssignment))
        {
            var fields = mie.Bindings.Cast<MemberAssignment>()
                .ToString(ma => ma.Member.Name.FirstLower() + ": " + ToJavascript(param, ma.Expression) + ",", "\n");

            var t = mie.Type;

            var typeName = TypeLogic.TryGetCleanName(t) ?? t.Name;

            return $"fd.New(\"{typeName}\", {{\n{fields}\n}})";
        }

        return null;
    }

    private static string? ToJsOperator(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Add => "+",
            ExpressionType.Subtract => "-",
            ExpressionType.Equal => "==",
            ExpressionType.NotEqual => "!=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.Coalesce => "??",
            _ => null
        };
    }

    private static string? ToJavascriptToString(ParameterExpression param, Expression expr, string? format = null)
    {
        var r = ToJavascript(param, expr);

        if (r == null)
            return null;

        if (expr.NodeType != ExpressionType.MemberAccess)
            return r;

        if (expr.Type.IsModifiableEntity() || expr.Type.IsLite() || expr.Type.IsIEntity())
            return "fd.getToString(" + r + ")";

        string? formatFull = format == null ? null : (", '" + format + "'");

        if (expr.Type.UnNullify() == typeof(DateTime))
            return "fd.dateToString(" + r + ", 'DateTime'" +  formatFull + ")";

        if (expr.Type.UnNullify() == typeof(DateOnly))
            return "fd.dateToString(" + r + ", 'Date'" + formatFull + ")";

        if (expr.Type.UnNullify() == typeof(TimeSpan)) /*deprecate?*/
            return "fd.timeToString(" + r + formatFull + ")";

        if (expr.Type.UnNullify() == typeof(TimeOnly))
            return "fd.timeToString(" + r + formatFull + ")";

        if (ReflectionTools.IsIntegerNumber(expr.Type.UnNullify()))
            return "fd.numberToString(" + r + (formatFull ?? ", 'D'") + ")";

        if (ReflectionTools.IsDecimalNumber(expr.Type.UnNullify()))
            return "fd.numberToString(" + r + (formatFull ?? ", 'N'") + ")";

        return "fd.valToString(" + r + ")";
    }
}
