﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34003
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Signum.Web.Views
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
    using Signum.Entities;
    using Signum.Utilities;
    using Signum.Web;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Signum/Views/PopupCancelControl.cshtml")]
    public partial class PopupCancelControl : System.Web.Mvc.WebViewPage<Context>
    {
        public PopupCancelControl()
        {
        }
        public override void Execute()
        {
WriteLiteral("<div");

WriteAttribute("id", Tuple.Create(" id=\"", 22), Tuple.Create("\"", 55)
            
            #line 3 "..\..\Signum\Views\PopupCancelControl.cshtml"
, Tuple.Create(Tuple.Create("", 27), Tuple.Create<System.Object, System.Int32>(Model.Compose("panelPopup")
            
            #line default
            #line hidden
, 27), false)
);

WriteLiteral(" class=\"sf-popup-control\"");

WriteLiteral(" data-prefix=\"");

            
            #line 3 "..\..\Signum\Views\PopupCancelControl.cshtml"
                                                                        Write(Model.Prefix);

            
            #line default
            #line hidden
WriteLiteral("\"");

WriteLiteral(" data-title=\"");

            
            #line 3 "..\..\Signum\Views\PopupCancelControl.cshtml"
                                                                                                       Write((string)ViewData[ViewDataKeys.Title]);

            
            #line default
            #line hidden
WriteLiteral("\"");

WriteLiteral(">\r\n");

WriteLiteral("    ");

            
            #line 4 "..\..\Signum\Views\PopupCancelControl.cshtml"
Write(Html.Partial((string)ViewData[ViewDataKeys.PartialViewName], Model));

            
            #line default
            #line hidden
WriteLiteral("   \r\n</div>\r\n");

        }
    }
}
#pragma warning restore 1591
