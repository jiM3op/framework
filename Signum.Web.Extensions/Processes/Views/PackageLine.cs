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
    using Signum.Entities.Processes;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MvcRazorClassGenerator", "1.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Processes/Views/PackageLine.cshtml")]
    public class _Page_Processes_Views_PackageLine_cshtml : System.Web.Mvc.WebViewPage<dynamic>
    {
#line hidden

        public _Page_Processes_Views_PackageLine_cshtml()
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


 using (var e = Html.TypeContext<PackageLineDN>())
{
    
Write(Html.EntityLine(e, f => f.Package, f => f.ReadOnly = true));

                                                               
    
Write(Html.EntityLine(e, f => f.Target, f => f.ReadOnly = true));

                                                              
    
Write(Html.EntityLine(e, f => f.Result, f => f.ReadOnly = true));

                                                              
    
Write(Html.ValueLine(e, f => f.FinishTime, f => f.ReadOnly = true));

                                                                 
    
Write(Html.ValueLine(e, f => f.Exception, f => f.ReadOnly = true));

                                                                
    
Write(Html.ValueLine(e, f => f.IdOrNull, f => f.ReadOnly = true));

                                                               
}
WriteLiteral(" ");


        }
    }
}
