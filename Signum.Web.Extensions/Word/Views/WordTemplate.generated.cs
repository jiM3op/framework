﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Signum.Web.Word.Views
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    
    #line 6 "..\..\Word\Views\WordTemplate.cshtml"
    using Signum.Engine.Basics;
    
    #line default
    #line hidden
    
    #line 7 "..\..\Word\Views\WordTemplate.cshtml"
    using Signum.Engine.DynamicQuery;
    
    #line default
    #line hidden
    using Signum.Entities;
    
    #line 5 "..\..\Word\Views\WordTemplate.cshtml"
    using Signum.Entities.DynamicQuery;
    
    #line default
    #line hidden
    
    #line 1 "..\..\Word\Views\WordTemplate.cshtml"
    using Signum.Entities.Files;
    
    #line default
    #line hidden
    
    #line 2 "..\..\Word\Views\WordTemplate.cshtml"
    using Signum.Entities.Word;
    
    #line default
    #line hidden
    using Signum.Utilities;
    using Signum.Web;
    
    #line 4 "..\..\Word\Views\WordTemplate.cshtml"
    using Signum.Web.Files;
    
    #line default
    #line hidden
    
    #line 3 "..\..\Word\Views\WordTemplate.cshtml"
    using Signum.Web.Word;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Word/Views/WordTemplate.cshtml")]
    public partial class WordTemplate : System.Web.Mvc.WebViewPage<dynamic>
    {
        public WordTemplate()
        {
        }
        public override void Execute()
        {
WriteLiteral("\r\n");

            
            #line 9 "..\..\Word\Views\WordTemplate.cshtml"
 using (var ec = Html.TypeContext<WordTemplateEntity>())
{
    ec.LabelColumns = new BsColumn(4);

            
            #line default
            #line hidden
WriteLiteral("    <div");

WriteLiteral(" class=\"row\"");

WriteLiteral(">\r\n        <div");

WriteLiteral(" class=\"col-sm-8\"");

WriteLiteral(">\r\n");

WriteLiteral("            ");

            
            #line 14 "..\..\Word\Views\WordTemplate.cshtml"
       Write(Html.ValueLine(ec, f => f.Name));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("            ");

            
            #line 15 "..\..\Word\Views\WordTemplate.cshtml"
       Write(Html.EntityLine(ec, f => f.Query));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("            ");

            
            #line 16 "..\..\Word\Views\WordTemplate.cshtml"
       Write(Html.EntityCombo(ec, f => f.SystemWordTemplate));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("            ");

            
            #line 17 "..\..\Word\Views\WordTemplate.cshtml"
       Write(Html.EntityCombo(ec, f => f.Culture));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("            ");

            
            #line 18 "..\..\Word\Views\WordTemplate.cshtml"
       Write(Html.EntityCombo(ec, f => f.WordTransformer));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("            ");

            
            #line 19 "..\..\Word\Views\WordTemplate.cshtml"
       Write(Html.EntityCombo(ec, f => f.WordConverter));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("            ");

            
            #line 20 "..\..\Word\Views\WordTemplate.cshtml"
       Write(Html.ValueLine(ec, f => f.FileName));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("            ");

            
            #line 21 "..\..\Word\Views\WordTemplate.cshtml"
       Write(Html.ValueLine(ec, f => f.DisableAuthorization));

            
            #line default
            #line hidden
WriteLiteral("\r\n        </div>\r\n");

            
            #line 23 "..\..\Word\Views\WordTemplate.cshtml"
        
            
            #line default
            #line hidden
            
            #line 23 "..\..\Word\Views\WordTemplate.cshtml"
         if (!ec.Value.IsNew)
        {
            using (var sc = ec.SubContext())
            {
                sc.FormGroupStyle = FormGroupStyle.Basic;

            
            #line default
            #line hidden
WriteLiteral("                <div");

WriteLiteral(" class=\"col-sm-4 form-vertical\"");

WriteLiteral(">\r\n                    <fieldset");

WriteLiteral(" style=\"margin-top: -25px\"");

WriteLiteral(">\r\n                        <legend>Active</legend>\r\n");

WriteLiteral("                        ");

            
            #line 31 "..\..\Word\Views\WordTemplate.cshtml"
                   Write(Html.ValueLine(sc, e => e.Active, vl => vl.InlineCheckbox = true));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("                        ");

            
            #line 32 "..\..\Word\Views\WordTemplate.cshtml"
                   Write(Html.ValueLine(sc, e => e.StartDate));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("                        ");

            
            #line 33 "..\..\Word\Views\WordTemplate.cshtml"
                   Write(Html.ValueLine(sc, e => e.EndDate));

            
            #line default
            #line hidden
WriteLiteral("\r\n                    </fieldset>\r\n                </div>\r\n");

            
            #line 36 "..\..\Word\Views\WordTemplate.cshtml"
            }
        }

            
            #line default
            #line hidden
WriteLiteral("    </div>\r\n");

            
            #line 39 "..\..\Word\Views\WordTemplate.cshtml"

    if (ec.Value.Query != null)
    {
        var ctx = new Context(ec, "tokenBuilder");

        var qd = DynamicQueryManager.Current.QueryDescription(ec.Value.Query.ToQueryName());


            
            #line default
            #line hidden
WriteLiteral("        <div");

WriteLiteral(" class=\"panel panel-default form-xs\"");

WriteLiteral(">\r\n            <div");

WriteLiteral(" class=\"panel-heading\"");

WriteLiteral(" style=\"padding:5px\"");

WriteLiteral(">\r\n                <div");

WriteLiteral(" class=\"sf-word-template-container\"");

WriteLiteral(">\r\n");

WriteLiteral("                    ");

            
            #line 49 "..\..\Word\Views\WordTemplate.cshtml"
               Write(Html.QueryTokenBuilder(null, ctx, WordClient.GetQueryTokenBuilderSettings(qd, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement)));

            
            #line default
            #line hidden
WriteLiteral("\r\n                    <input");

WriteLiteral(" type=\"button\"");

WriteLiteral(" disabled=\"disabled\"");

WriteLiteral(" data-prefix=\"");

            
            #line 50 "..\..\Word\Views\WordTemplate.cshtml"
                                                                     Write(ctx.Prefix);

            
            #line default
            #line hidden
WriteLiteral("\"");

WriteLiteral(" class=\"btn btn-default btn-sm sf-button sf-word-inserttoken sf-word-inserttoken-" +
"basic\"");

WriteAttribute("value", Tuple.Create(" value=\"", 2152), Tuple.Create("\"", 2233)
            
            #line 50 "..\..\Word\Views\WordTemplate.cshtml"
                                                                                                      , Tuple.Create(Tuple.Create("", 2160), Tuple.Create<System.Object, System.Int32>(Signum.Entities.Mailing.EmailTemplateViewMessage.Insert.NiceToString()
            
            #line default
            #line hidden
, 2160), false)
);

WriteLiteral(" />\r\n                    <input");

WriteLiteral(" type=\"button\"");

WriteLiteral(" disabled=\"disabled\"");

WriteLiteral(" data-prefix=\"");

            
            #line 51 "..\..\Word\Views\WordTemplate.cshtml"
                                                                     Write(ctx.Prefix);

            
            #line default
            #line hidden
WriteLiteral("\"");

WriteLiteral(" class=\"btn btn-default btn-sm sf-button sf-word-inserttoken sf-word-inserttoken-" +
"if\"");

WriteLiteral(" data-block=\"if\"");

WriteLiteral(" value=\"if\"");

WriteLiteral(" />\r\n                    <input");

WriteLiteral(" type=\"button\"");

WriteLiteral(" disabled=\"disabled\"");

WriteLiteral(" data-prefix=\"");

            
            #line 52 "..\..\Word\Views\WordTemplate.cshtml"
                                                                     Write(ctx.Prefix);

            
            #line default
            #line hidden
WriteLiteral("\"");

WriteLiteral(" class=\"btn btn-default btn-sm sf-button sf-word-inserttoken sf-word-inserttoken-" +
"foreach\"");

WriteLiteral(" data-block=\"foreach\"");

WriteLiteral(" value=\"foreach\"");

WriteLiteral(" />\r\n                    <input");

WriteLiteral(" type=\"button\"");

WriteLiteral(" disabled=\"disabled\"");

WriteLiteral(" data-prefix=\"");

            
            #line 53 "..\..\Word\Views\WordTemplate.cshtml"
                                                                     Write(ctx.Prefix);

            
            #line default
            #line hidden
WriteLiteral("\"");

WriteLiteral(" class=\"btn btn-default btn-sm sf-button sf-word-inserttoken sf-word-inserttoken-" +
"any\"");

WriteLiteral(" data-block=\"any\"");

WriteLiteral(" value=\"any\"");

WriteLiteral(" />\r\n                </div>\r\n\r\n                <script>\r\n                    $(fu" +
"nction () {\r\n");

WriteLiteral("                        ");

            
            #line 58 "..\..\Word\Views\WordTemplate.cshtml"
                    Write(WordClient.Module["initReplacements"]());

            
            #line default
            #line hidden
WriteLiteral("\r\n                    });\r\n                </script>\r\n            </div>\r\n       " +
" </div>\r\n");

WriteLiteral("        <div");

WriteLiteral(" class=\"col-sm-8\"");

WriteLiteral(">\r\n");

WriteLiteral("            ");

            
            #line 64 "..\..\Word\Views\WordTemplate.cshtml"
       Write(Html.FileLineLite(ec, e => e.Template));

            
            #line default
            #line hidden
WriteLiteral("\r\n        </div>\r\n");

            
            #line 66 "..\..\Word\Views\WordTemplate.cshtml"
    }
}

            
            #line default
            #line hidden
WriteLiteral("\r\n");

        }
    }
}
#pragma warning restore 1591
