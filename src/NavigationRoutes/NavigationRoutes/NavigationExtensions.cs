﻿using Microsoft.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
using System.Text;

namespace NavigationRoutes
{
    public class CompositeMvcHtmlString : IHtmlString
    {
        readonly IEnumerable<IHtmlString> _strings;

        public CompositeMvcHtmlString(IEnumerable<IHtmlString> strings)
        {
            _strings = strings;
        }

        public string ToHtmlString()
        {
            return string.Join(string.Empty, _strings.Select(x => x.ToHtmlString()));
        }
    }

    public static class NavigationRoutes
    {
        public static List<INavigationRouteFilter> Filters=new List<INavigationRouteFilter>();
    }
    public static class NavigationViewExtensions
    {
        
        public static IHtmlString Navigation(this HtmlHelper helper)
        {
            return new CompositeMvcHtmlString(
                GetRoutesForCurrentRequest(RouteTable.Routes,NavigationRoutes.Filters).Select(namedRoute => helper.NavigationListItemRouteLink(namedRoute)));
        }

        public static IEnumerable<NamedRoute> GetRoutesForCurrentRequest(RouteCollection routes,IEnumerable<INavigationRouteFilter> routeFilters)
        {
            var navigationRoutes = routes.OfType<NamedRoute>().Where(r=>r.IsChild==false).ToList();
            if (routeFilters.Count() > 0)
            {
                foreach (var route in navigationRoutes.ToArray())
                {
                    foreach (var filter in routeFilters)
                    {
                        if (filter.ShouldRemove(route))
                        {
                            navigationRoutes.Remove(route);
                            break;
                        }
                    }
                }
            }
            return navigationRoutes;
        }

        public static MvcHtmlString NavigationListItemRouteLink(this HtmlHelper html, NamedRoute route)
        {
            var li = new TagBuilder("li");
            li.InnerHtml = html.RouteLink(route.DisplayName, route.Name).ToString();

            if (CurrentRouteMatchesName(html, route.Name))
            {
                li.AddCssClass("active");
            }

            if (route.Children.Any())
            {
                BuildChildMenu(html, route, li);
            }

            // collect our html 
            var tagBuilders = new List<TagBuilder>();
            tagBuilders.Add(li);
            if (route.ShouldBreakAfter)
            {
                var breakLi = new TagBuilder("li");
                breakLi.AddCssClass("divider-vertical");
                tagBuilders.Add(breakLi);
            }
            var tags = new StringBuilder();
            tagBuilders.ForEach(b => tags.Append(b.ToString(TagRenderMode.Normal)));

            return MvcHtmlString.Create(tags.ToString());
        }

        private static void BuildChildMenu(HtmlHelper html, NamedRoute route, TagBuilder li)
        {
            // convert menu entry to dropdown
            li.AddCssClass("dropdown");
            li.InnerHtml = "<a href=\"#\" class=\"dropdown-toggle\" data-toggle=\"dropdown\">" + route.DisplayName +
                           "<b class=\"caret\"></b></a>";

            // build LIs for the children
            var ul = new TagBuilder("ul");
            ul.AddCssClass("dropdown-menu");
            foreach (var child in route.Children)
            {
                var childLi = new TagBuilder("li");
                childLi.InnerHtml = html.RouteLink(child.DisplayName, child.Name).ToString();
                ul.InnerHtml += childLi.ToString();

                // support for drop down list breaks 
                if (child.ShouldBreakAfter)
                {
                    var breakLi = new TagBuilder("li");
                    breakLi.AddCssClass("divider");
                    ul.InnerHtml += breakLi.ToString();
                }
            }

            // append the UL
            li.InnerHtml = "<a href='#' class='dropdown-toggle' data-toggle='dropdown'>" + route.DisplayName +
                           " <b class='caret'></b></a>" + ul.ToString();
        }


        static bool CurrentRouteMatchesName(HtmlHelper html, string routeName)
        {
            var namedRoute = html.ViewContext.RouteData.Route as NamedRoute;
            if (namedRoute != null)
            {
                if (string.Equals(routeName, namedRoute.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }

   
}