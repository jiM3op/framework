﻿#region usings
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI;
using System.Threading;
using Signum.Entities.Authorization;
using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.Entities;
using Signum.Web.Controllers;
using Signum.Web.Extensions.Properties;
#endregion

namespace Signum.Web.ViewsChecker
{

    [HandleError]
    public class ViewsCheckerController : Controller
    {
        public ViewResult ViewsChecker()
        {

            //We ensure Buffer is active
            Response.Buffer = true;

            List<ViewError> errors = new List<ViewError>();

            HtmlHelper helper = SignumController.CreateHtmlHelper(this);

            foreach (var entry in Navigator.Manager.EntitySettings)
            {
                if (entry.Value.PartialViewName == null)
                    continue;

                string result = "";
                ModifiableEntity entity = null;
                try
                {
                    Response.Clear();
                    entity = (ModifiableEntity)Constructor.Construct(entry.Key, this);
                    result = helper.RenderPartialToString(entry.Value.PartialViewName(entity), new ViewDataDictionary(entity));
                }
                catch (Exception ex)
                {
                    Exception firstEx = FindMostInnerException(ex);

                    ViewError error = new ViewError
                    {
                        ViewName = entry.Value.PartialViewName(entity),
                        Message = ex.Message,
                        Source = ex.Source,
                        StackTrace = ex.StackTrace,
                        TargetSite = ex.TargetSite.ToString()
                    };

                    errors.Add(error);
                }
            }
            //Clear content written by the renderization of views, just want error content
            Response.Clear();

            return View("~/Plugin/Signum.Web.Extensions.dll/Signum.Web.Extensions.ViewsChecker.ViewsChecker.aspx", errors);
        }

        private string FindRegion(string result, string key)
        { 
            int index = result.IndexOf(key);
            string region = result.Substring(result.IndexOf("</b>"), index).Replace("<b>","");
            result = result.Substring(index);
            return region;
        }

        private Exception FindMostInnerException(Exception ex)
        {
            if (ex.InnerException == null)
                return ex;

            return FindMostInnerException(ex.InnerException);
        }
    }
}
