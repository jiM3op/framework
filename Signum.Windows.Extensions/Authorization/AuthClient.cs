﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Utilities;
using System.Windows;
using Signum.Services;
using System.Reflection;
using System.Collections;
using Signum.Windows;
using System.Windows.Controls;
using Signum.Entities.Basics;
using Signum.Entities;

namespace Signum.Windows.Authorization
{
    public static class AuthClient
    {
        static HashSet<object> authorizedQueries; 
        static Dictionary<Type, TypeAccess> typeRules; 
        static Dictionary<Type, Dictionary<string, Access>> propertyRules;
        static Dictionary<Enum, bool> permissionRules; 

        public static void UpdateCache()
        {
            if (propertyRules != null)
                propertyRules = Server.Return((IPropertyAuthServer s) => s.AuthorizedProperties());

            if (authorizedQueries != null)
                authorizedQueries = Server.Return((IQueryAuthServer s) => s.AuthorizedQueries()); 

            if (typeRules != null)
                typeRules = Server.Return((ITypeAuthServer s) => s.AuthorizedTypes());

            if (permissionRules != null)
                permissionRules = Server.Return((IPermissionAuthServer s) => s.PermissionRules());
        }

        public static bool? TryIsAuthorized(this Enum permissionKey)
        {
            return permissionRules.TryGetS(permissionKey);
        }

        public static bool IsAuthorized(this Enum permissionKey)
        {
            if(permissionRules == null)
                throw new InvalidOperationException("Permissions not enabled in AuthClient");
 
            bool result;
            if(!permissionRules.TryGetValue(permissionKey, out result))
                throw new ArgumentException("{0} is not a permissionKey registered in the server".Formato(permissionKey));
 
            return result;
        }

        public static void Start(bool types, bool property, bool queries, bool permissions)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.Manager.Settings.Add(typeof(UserDN), new EntitySettings(EntityType.Admin) { View = e => new User() });
                Navigator.Manager.Settings.Add(typeof(RoleDN), new EntitySettings(EntityType.Default) { View = e => new Role() });

                if (property)
                {
                    propertyRules = Server.Return((IPropertyAuthServer s)=>s.AuthorizedProperties()); 
                    Common.RouteTask += Common_RouteTask;
                    Common.PseudoRouteTask += Common_RouteTask;
                    PropertyRoute.SetIsAllowedCallback(pr => GetPropertyAccess(pr) >= Access.Read);
                }

                if (types)
                {
                    typeRules = Server.Return((ITypeAuthServer s)=>s.AuthorizedTypes());
                    Navigator.Manager.GlobalIsCreable += type => GetTypeAccess(type).HasFlag(TypeAccessRule.CreateKey);
                    Navigator.Manager.GlobalIsReadOnly += type => !GetTypeAccess(type).HasFlag(TypeAccessRule.ModifyKey);
                    Navigator.Manager.GlobalIsViewable += type => GetTypeAccess(type).HasFlag(TypeAccess.Read);

                    MenuManager.Tasks += new Action<MenuItem>(MenuManager_TasksTypes);
                }

                if (queries)
                {
                    authorizedQueries = Server.Return((IQueryAuthServer s)=>s.AuthorizedQueries()); 
                    Navigator.Manager.GlobalIsFindable += qn => GetQueryAceess(qn);

                    MenuManager.Tasks += new Action<MenuItem>(MenuManager_TasksQueries);
                }

                if (permissions)
                {
                    permissionRules = Server.Return((IPermissionAuthServer s) => s.PermissionRules());
                }

                Links.RegisterEntityLinks<RoleDN>((r, c) =>
                {
                    bool authorized = BasicPermissions.AdminRules.TryIsAuthorized() ?? true;
                    return new QuickLink[]
                    {
                         new QuickLinkAction("Type Rules", () => 
                            new TypeRules 
                            { 
                                Owner = c.FindCurrentWindow(),
                                Role = r.ToLite(), 
                                Properties = property, 
                                Operations = Server.Implements<IOperationAuthServer>(), 
                                Queries = queries 
                            }.Show())
                         { 
                             IsVisible = authorized && types
                         },
                         new QuickLinkAction("Permission Rules", () => new PermissionRules { Role = r.ToLite(), Owner = c.FindCurrentWindow() }.Show())
                         {
                             IsVisible = authorized && permissions
                         },
                         new QuickLinkAction("Facade Method Rules", () => new FacadeMethodRules { Role = r.ToLite(), Owner = c.FindCurrentWindow() }.Show())
                         { 
                             IsVisible = authorized && Server.Implements<IFacadeMethodAuthServer>()
                         },
                         new QuickLinkAction("Entity Groups", () => new EntityGroupRules { Role = r.ToLite(), Owner = c.FindCurrentWindow() }.Show())
                         {
                             IsVisible = authorized && Server.Implements<IEntityGroupAuthServer>(),
                         }
                     };
                }); 
            }
        }



        static void MenuManager_TasksTypes(MenuItem menuItem)
        {
            if (menuItem.NotSet(MenuItem.VisibilityProperty))
            {
                object tag = menuItem.Tag;

                if (tag == null)
                    return;

                Type type = tag as Type ?? (tag as AdminOptions).TryCC(a => a.Type);

                if (type != null && Navigator.Manager.Settings.ContainsKey(type))
                {
                    if (GetTypeAccess(type) == TypeAccess.None)
                        menuItem.Visibility = Visibility.Collapsed;
                }
            }
        }

        static void MenuManager_TasksQueries(MenuItem menuItem)
        {
            if (menuItem.NotSet(MenuItem.VisibilityProperty))
            {
                object tag = menuItem.Tag;

                if (tag == null)
                    return;

                object queryName =
                    tag is Type ? null : //maybe a type but only if in FindOptions
                    tag is FindOptionsBase ? ((FindOptionsBase)tag).QueryName :
                    tag;

                if (queryName != null && Navigator.Manager.QuerySetting.ContainsKey(queryName))
                {
                    if (!GetQueryAceess(queryName))
                        menuItem.Visibility = Visibility.Collapsed;
                }
            }
        }

        static TypeAccess GetTypeAccess(Type type)
        {
            return typeRules.TryGetS(type) ?? TypeAccess.FullAccess;
        }

        static Access GetPropertyAccess(PropertyRoute route)
        {
            if (route.PropertyRouteType == PropertyRouteType.MListItems)
                return GetPropertyAccess(route.Parent);

            return propertyRules.TryGetC(route.IdentifiableType).TryGetS(route.PropertyString()) ?? Access.Modify;
        }

        static bool GetQueryAceess(object queryName)
        {
            return authorizedQueries.Contains(queryName); 
        }

        static void Common_RouteTask(FrameworkElement fe, string route, PropertyRoute context)
        {
            if (context.PropertyRouteType == PropertyRouteType.Property)
            {
                switch (GetPropertyAccess(context))
                {
                    case Access.None: fe.Visibility = Visibility.Collapsed; break;
                    case Access.Read: Common.SetIsReadOnly(fe, true); break;
                    case Access.Modify: break;
                } 
            }
        }
    }
}
