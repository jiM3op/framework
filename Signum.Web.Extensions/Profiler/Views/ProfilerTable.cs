﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ASP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using Signum.Utilities;
    using Signum.Entities;
    using Signum.Web;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel.DataAnnotations;
    using System.Configuration;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web.Caching;
    using System.Web.DynamicData;
    using System.Web.SessionState;
    using System.Web.Profile;
    using System.Web.UI.WebControls;
    using System.Web.UI.WebControls.WebParts;
    using System.Web.UI.HtmlControls;
    using System.Xml.Linq;
    using Signum.Utilities.ExpressionTrees;
    using Signum.Web.Profiler;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MvcRazorClassGenerator", "1.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Profiler/Views/ProfilerTable.cshtml")]
    public class _Page_Profiler_Views_ProfilerTable_cshtml : System.Web.Mvc.WebViewPage<dynamic>
    {
#line hidden

        public _Page_Profiler_Views_ProfilerTable_cshtml()
        {
        }
        protected System.Web.HttpApplication ApplicationInstance
        {
            get
            {
                return ((System.Web.HttpApplication)(Context.ApplicationInstance));
            }
        }
        public override void Execute()
        {


WriteLiteral("\r\n");


   List<HeavyProfilerEntry> entries = (List<HeavyProfilerEntry>)Model;
   var roles = entries.Select(a => a.GetDescendantRoles()).ToList();
   var allKeys = roles.SelectMany(a => a.Keys).Distinct().Order().ToList();


WriteLiteral(@"
<table class=""tblResults"">
    <thead>
        <tr>
            <th>
                Entry
            </th>
            <th>
                Type
            </th>
            <th>
                Method
            </th>
            <th>
                Role
            </th>
            <th>
                Time
            </th>
            <th>
                Childs
            </th>
");


             foreach (var k in allKeys)
            {

WriteLiteral("                <th>");


               Write(k);

WriteLiteral(" Childs\r\n                </th>\r\n");


            }

WriteLiteral("            <th>\r\n                Aditional Data\r\n            </th>\r\n        </tr" +
">\r\n    </thead>\r\n    <tbody>\r\n");


         for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var rol = roles[i];

WriteLiteral("            <tr>\r\n                <td>\r\n                    ");


               Write(Html.ProfilerEntry("View", entry.FullIndex()));

WriteLiteral("\r\n                </td>\r\n                <td>\r\n                    ");


               Write(entry.Type.TypeName());

WriteLiteral("\r\n                </td>\r\n                <td>\r\n                    ");


               Write(entry.Method.Name);

WriteLiteral("\r\n                </td>\r\n                <td>\r\n                    ");


               Write(entry.Role);

WriteLiteral("\r\n                </td>\r\n                <td align=\"right\">\r\n                    " +
"");


               Write(entry.Elapsed.NiceToString());

WriteLiteral("\r\n                </td>\r\n                <td align=\"right\">\r\n                    " +
"");


               Write(entry.GetEntriesResume().TryCC(r => r.ToString(entry)));

WriteLiteral("\r\n                </td>\r\n");


                 foreach (var k in allKeys)
                {

WriteLiteral("                    <td align=\"right\">\r\n                        ");


                   Write(rol.TryGetC(k).TryCC(r => r.ToString(entry)));

WriteLiteral("\r\n                    </td>\r\n");


                }

WriteLiteral("                <td>\r\n                    ");


               Write(entry.AditionalData.TryCC(o => o.ToString().Left(50, false)));

WriteLiteral("\r\n                </td>\r\n            </tr>\r\n");


        }

WriteLiteral("    </tbody>\r\n</table>\r\n<br />\r\n");


        }
    }
}
