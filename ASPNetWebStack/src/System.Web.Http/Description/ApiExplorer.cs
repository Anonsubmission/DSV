﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text.RegularExpressions;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Http.Internal;
using System.Web.Http.Routing;
using System.Web.Http.Services;

namespace System.Web.Http.Description
{
    /// <summary>
    /// Explores the URI space of the service based on routes, controllers and actions available in the system.
    /// </summary>
    public class ApiExplorer : IApiExplorer
    {
        private Lazy<Collection<ApiDescription>> _apiDescriptions;
        private readonly HttpConfiguration _config;
        private static readonly Regex _actionVariableRegex = new Regex(String.Format(CultureInfo.CurrentCulture, "{{{0}}}", RouteKeys.ActionKey), RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex _controllerVariableRegex = new Regex(String.Format(CultureInfo.CurrentCulture, "{{{0}}}", RouteKeys.ControllerKey), RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiExplorer"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public ApiExplorer(HttpConfiguration configuration)
        {
            _config = configuration;
            _apiDescriptions = new Lazy<Collection<ApiDescription>>(InitializeApiDescriptions);
        }

        /// <summary>
        /// Gets the API descriptions. The descriptions are initialized on the first access.
        /// </summary>
        public Collection<ApiDescription> ApiDescriptions
        {
            get
            {
                return _apiDescriptions.Value;
            }
        }

        /// <summary>
        /// Gets or sets the documentation provider. The provider will be responsible for documenting the API.
        /// </summary>
        /// <value>
        /// The documentation provider.
        /// </value>
        public IDocumentationProvider DocumentationProvider { get; set; }

        /// <summary>
        /// Determines whether the controller should be considered for <see cref="ApiExplorer.ApiDescriptions"/> generation. Called when initializing the <see cref="ApiExplorer.ApiDescriptions"/>.
        /// </summary>
        /// <param name="controllerVariableValue">The controller variable value from the route.</param>
        /// <param name="controllerDescriptor">The controller descriptor.</param>
        /// <param name="route">The route.</param>
        /// <returns><c>true</c> if the controller should be considered for <see cref="ApiExplorer.ApiDescriptions"/> generation, <c>false</c> otherwise.</returns>
        public virtual bool ShouldExploreController(string controllerVariableValue, HttpControllerDescriptor controllerDescriptor, IHttpRoute route)
        {
            if (controllerDescriptor == null)
            {
                throw Error.ArgumentNull("controllerDescriptor");
            }

            if (route == null)
            {
                throw Error.ArgumentNull("route");
            }

            ApiExplorerSettingsAttribute setting = controllerDescriptor.GetCustomAttributes<ApiExplorerSettingsAttribute>().FirstOrDefault();
            return (setting == null || !setting.IgnoreApi) &&
                MatchRegexConstraint(route, RouteKeys.ControllerKey, controllerVariableValue);
        }

        /// <summary>
        /// Determines whether the action should be considered for <see cref="ApiExplorer.ApiDescriptions"/> generation. Called when initializing the <see cref="ApiExplorer.ApiDescriptions"/>.
        /// </summary>
        /// <param name="actionVariableValue">The action variable value from the route.</param>
        /// <param name="actionDescriptor">The action descriptor.</param>
        /// <param name="route">The route.</param>
        /// <returns><c>true</c> if the action should be considered for <see cref="ApiExplorer.ApiDescriptions"/> generation, <c>false</c> otherwise.</returns>
        public virtual bool ShouldExploreAction(string actionVariableValue, HttpActionDescriptor actionDescriptor, IHttpRoute route)
        {
            if (actionDescriptor == null)
            {
                throw Error.ArgumentNull("actionDescriptor");
            }

            if (route == null)
            {
                throw Error.ArgumentNull("route");
            }

            ApiExplorerSettingsAttribute setting = actionDescriptor.GetCustomAttributes<ApiExplorerSettingsAttribute>().FirstOrDefault();
            NonActionAttribute nonAction = actionDescriptor.GetCustomAttributes<NonActionAttribute>().FirstOrDefault();
            return (setting == null || !setting.IgnoreApi) &&
                (nonAction == null) &&
                MatchRegexConstraint(route, RouteKeys.ActionKey, actionVariableValue);
        }

        /// <summary>
        /// Gets a collection of HttpMethods supported by the action. Called when initializing the <see cref="ApiExplorer.ApiDescriptions"/>.
        /// </summary>
        /// <param name="route">The route.</param>
        /// <param name="actionDescriptor">The action descriptor.</param>
        /// <returns>A collection of HttpMethods supported by the action.</returns>
        public virtual Collection<HttpMethod> GetHttpMethodsSupportedByAction(IHttpRoute route, HttpActionDescriptor actionDescriptor)
        {
            if (route == null)
            {
                throw Error.ArgumentNull("route");
            }

            if (actionDescriptor == null)
            {
                throw Error.ArgumentNull("actionDescriptor");
            }

            IList<HttpMethod> supportedMethods = new List<HttpMethod>();
            IList<HttpMethod> actionHttpMethods = actionDescriptor.SupportedHttpMethods;
            HttpMethodConstraint httpMethodConstraint = route.Constraints.Values.FirstOrDefault(c => typeof(HttpMethodConstraint).IsAssignableFrom(c.GetType())) as HttpMethodConstraint;

            if (httpMethodConstraint == null)
            {
                supportedMethods = actionHttpMethods;
            }
            else
            {
                supportedMethods = httpMethodConstraint.AllowedMethods.Intersect(actionHttpMethods).ToList();
            }

            return new Collection<HttpMethod>(supportedMethods);
        }

        private Collection<ApiDescription> InitializeApiDescriptions()
        {
            Collection<ApiDescription> apiDescriptions = new Collection<ApiDescription>();
            IHttpControllerSelector controllerSelector = _config.Services.GetHttpControllerSelector();
            IDictionary<string, HttpControllerDescriptor> controllerMappings = controllerSelector.GetControllerMapping();
            if (controllerMappings != null)
            {
                ApiDescriptionComparer descriptionComparer = new ApiDescriptionComparer();
                foreach (IHttpRoute route in _config.Routes)
                {
                    ReflectedHttpActionDescriptor[] actions = route.GetDirectRouteActions();
                    Collection<ApiDescription> descriptionsFromRoute =
                        (actions == null) ?
                            ExploreRouteControllers(controllerMappings, route) :
                            ExploreDirectRoute(route, actions);

                    // Remove ApiDescription that will lead to ambiguous action matching.
                    // E.g. a controller with Post() and PostComment(). When the route template is {controller}, it produces POST /controller and POST /controller.
                    descriptionsFromRoute = RemoveInvalidApiDescriptions(descriptionsFromRoute);

                    foreach (ApiDescription description in descriptionsFromRoute)
                    {
                        // Do not add the description if the previous route has a matching description with the same HTTP method and relative path.
                        // E.g. having two routes with the templates "api/Values/{id}" and "api/{controller}/{id}" can potentially produce the same
                        // relative path "api/Values/{id}" but only the first one matters.
                        if (!apiDescriptions.Contains(description, descriptionComparer))
                        {
                            apiDescriptions.Add(description);
                        }
                    }
                }
            }

            return apiDescriptions;
        }

        private Collection<ApiDescription> ExploreDirectRoute(IHttpRoute route, ReflectedHttpActionDescriptor[] actions)
        {
            Collection<ApiDescription> descriptions = new Collection<ApiDescription>();
            PopulateActionDescriptions(actions, null, route, route.RouteTemplate, descriptions);
            return descriptions;
        }

        private Collection<ApiDescription> ExploreRouteControllers(IDictionary<string, HttpControllerDescriptor> controllerMappings, IHttpRoute route)
        {
            Collection<ApiDescription> apiDescriptions = new Collection<ApiDescription>();
            string routeTemplate = route.RouteTemplate;
            string controllerVariableValue;
            if (_controllerVariableRegex.IsMatch(routeTemplate))
            {
                // unbound controller variable, {controller}
                foreach (KeyValuePair<string, HttpControllerDescriptor> controllerMapping in controllerMappings)
                {
                    controllerVariableValue = controllerMapping.Key;
                    HttpControllerDescriptor controllerDescriptor = controllerMapping.Value;
                    if (ShouldExploreController(controllerVariableValue, controllerDescriptor, route))
                    {
                        // expand {controller} variable
                        string expandedRouteTemplate = _controllerVariableRegex.Replace(routeTemplate, controllerVariableValue);
                        ExploreRouteActions(route, expandedRouteTemplate, controllerDescriptor, apiDescriptions);
                    }
                }
            }
            else if (route.Defaults.TryGetValue(RouteKeys.ControllerKey, out controllerVariableValue))
            {
                // bound controller variable, {controller = "controllerName"}
                HttpControllerDescriptor controllerDescriptor;
                if (controllerMappings.TryGetValue(controllerVariableValue, out controllerDescriptor) && ShouldExploreController(controllerVariableValue, controllerDescriptor, route))
                {
                    ExploreRouteActions(route, routeTemplate, controllerDescriptor, apiDescriptions);
                }
            }

            return apiDescriptions;
        }

        private void ExploreRouteActions(IHttpRoute route, string localPath, HttpControllerDescriptor controllerDescriptor, Collection<ApiDescription> apiDescriptions)
        {
            ServicesContainer controllerServices = controllerDescriptor.Configuration.Services;
            ILookup<string, HttpActionDescriptor> actionMappings = controllerServices.GetActionSelector().GetActionMapping(controllerDescriptor);
            string actionVariableValue;
            if (actionMappings != null)
            {
                if (_actionVariableRegex.IsMatch(localPath))
                {
                    // unbound action variable, {action}
                    foreach (IGrouping<string, HttpActionDescriptor> actionMapping in actionMappings)
                    {
                        // expand {action} variable
                        actionVariableValue = actionMapping.Key;
                        string expandedLocalPath = _actionVariableRegex.Replace(localPath, actionVariableValue);
                        PopulateActionDescriptions(actionMapping, actionVariableValue, route, expandedLocalPath, apiDescriptions);
                    }
                }
                else if (route.Defaults.TryGetValue(RouteKeys.ActionKey, out actionVariableValue))
                {
                    // bound action variable, { action = "actionName" }
                    PopulateActionDescriptions(actionMappings[actionVariableValue], actionVariableValue, route, localPath, apiDescriptions);
                }
                else
                {
                    // no {action} specified, e.g. {controller}/{id}
                    foreach (IGrouping<string, HttpActionDescriptor> actionMapping in actionMappings)
                    {
                        PopulateActionDescriptions(actionMapping, null, route, localPath, apiDescriptions);
                    }
                }
            }
        }

        private void PopulateActionDescriptions(IEnumerable<HttpActionDescriptor> actionDescriptors, string actionVariableValue, IHttpRoute route, string localPath, Collection<ApiDescription> apiDescriptions)
        {
            foreach (HttpActionDescriptor actionDescriptor in actionDescriptors)
            {
                if (ShouldExploreAction(actionVariableValue, actionDescriptor, route))
                {
                    PopulateActionDescriptions(actionDescriptor, route, localPath, apiDescriptions);
                }
            }
        }

        private void PopulateActionDescriptions(HttpActionDescriptor actionDescriptor, IHttpRoute route, string localPath, Collection<ApiDescription> apiDescriptions)
        {
            string apiDocumentation = GetApiDocumentation(actionDescriptor);

            // parameters
            IList<ApiParameterDescription> parameterDescriptions = CreateParameterDescriptions(actionDescriptor);

            // expand all parameter variables
            string finalPath;

            if (!TryExpandUriParameters(route, localPath, parameterDescriptions, out finalPath))
            {
                // the action cannot be reached due to parameter mismatch, e.g. routeTemplate = "/users/{name}" and GetUsers(id)
                return;
            }

            // request formatters
            ApiParameterDescription bodyParameter = parameterDescriptions.FirstOrDefault(description => description.Source == ApiParameterSource.FromBody);
            IEnumerable<MediaTypeFormatter> supportedRequestBodyFormatters = bodyParameter != null ?
                actionDescriptor.Configuration.Formatters.Where(f => f.CanReadType(bodyParameter.ParameterDescriptor.ParameterType)) :
                Enumerable.Empty<MediaTypeFormatter>();

            // response formatters
            ResponseDescription responseDescription = CreateResponseDescription(actionDescriptor);
            Type returnType = responseDescription.ResponseType ?? responseDescription.DeclaredType;
            IEnumerable<MediaTypeFormatter> supportedResponseFormatters = returnType != null ?
                actionDescriptor.Configuration.Formatters.Where(f => f.CanWriteType(returnType)) :
                Enumerable.Empty<MediaTypeFormatter>();

            // Replacing the formatter tracers with formatters if tracers are present.
            supportedRequestBodyFormatters = GetInnerFormatters(supportedRequestBodyFormatters);
            supportedResponseFormatters = GetInnerFormatters(supportedResponseFormatters);

            // get HttpMethods supported by an action. Usually there is one HttpMethod per action but we allow multiple of them per action as well.
            IList<HttpMethod> supportedMethods = GetHttpMethodsSupportedByAction(route, actionDescriptor);

            foreach (HttpMethod method in supportedMethods)
            {
                apiDescriptions.Add(new ApiDescription
                {
                    Documentation = apiDocumentation,
                    HttpMethod = method,
                    RelativePath = finalPath,
                    ActionDescriptor = actionDescriptor,
                    Route = route,
                    SupportedResponseFormatters = new Collection<MediaTypeFormatter>(supportedResponseFormatters.ToList()),
                    SupportedRequestBodyFormatters = new Collection<MediaTypeFormatter>(supportedRequestBodyFormatters.ToList()),
                    ParameterDescriptions = new Collection<ApiParameterDescription>(parameterDescriptions),
                    ResponseDescription = responseDescription
                });
            }
        }

        private ResponseDescription CreateResponseDescription(HttpActionDescriptor actionDescriptor)
        {
            Collection<ResponseTypeAttribute> responseTypeAttribute = actionDescriptor.GetCustomAttributes<ResponseTypeAttribute>();
            Type responseType = responseTypeAttribute.Select(attribute => attribute.ResponseType).FirstOrDefault();

            return new ResponseDescription
            {
                DeclaredType = actionDescriptor.ReturnType,
                ResponseType = responseType,
                Documentation = GetApiResponseDocumentation(actionDescriptor)
            };
        }

        private static IEnumerable<MediaTypeFormatter> GetInnerFormatters(IEnumerable<MediaTypeFormatter> mediaTypeFormatters)
        {
            foreach (MediaTypeFormatter formatter in mediaTypeFormatters)
            {
                yield return Decorator.GetInner(formatter);
            }
        }

        private static bool TryExpandUriParameters(IHttpRoute route, string routeTemplate, ICollection<ApiParameterDescription> parameterDescriptions, out string expandedRouteTemplate)
        {
            HttpParsedRoute parsedRoute = HttpRouteParser.Parse(routeTemplate);
            Dictionary<string, object> parameterValuesForRoute = new Dictionary<string, object>();
            foreach (ApiParameterDescription parameterDescriptor in parameterDescriptions)
            {
                Type parameterType = parameterDescriptor.ParameterDescriptor.ParameterType;
                if (parameterDescriptor.Source == ApiParameterSource.FromUri && TypeHelper.CanConvertFromString(parameterType))
                {
                    parameterValuesForRoute.Add(parameterDescriptor.Name, "{" + parameterDescriptor.Name + "}");
                }
            }

            BoundRouteTemplate boundRouteTemplate = parsedRoute.Bind(null, parameterValuesForRoute, new HttpRouteValueDictionary(route.Defaults), new HttpRouteValueDictionary(route.Constraints));
            if (boundRouteTemplate == null)
            {
                expandedRouteTemplate = null;
                return false;
            }

            expandedRouteTemplate = Uri.UnescapeDataString(boundRouteTemplate.BoundTemplate);
            return true;
        }

        private IList<ApiParameterDescription> CreateParameterDescriptions(HttpActionDescriptor actionDescriptor)
        {
            IList<ApiParameterDescription> parameterDescriptions = new List<ApiParameterDescription>();
            HttpActionBinding actionBinding = GetActionBinding(actionDescriptor);

            // try get parameter binding information if available
            if (actionBinding != null)
            {
                HttpParameterBinding[] parameterBindings = actionBinding.ParameterBindings;
                if (parameterBindings != null)
                {
                    foreach (HttpParameterBinding parameter in parameterBindings)
                    {
                        parameterDescriptions.Add(CreateParameterDescriptionFromBinding(parameter));
                    }
                }
            }
            else
            {
                Collection<HttpParameterDescriptor> parameters = actionDescriptor.GetParameters();
                if (parameters != null)
                {
                    foreach (HttpParameterDescriptor parameter in parameters)
                    {
                        parameterDescriptions.Add(CreateParameterDescriptionFromDescriptor(parameter));
                    }
                }
            }

            return parameterDescriptions;
        }

        private ApiParameterDescription CreateParameterDescriptionFromDescriptor(HttpParameterDescriptor parameter)
        {
            ApiParameterDescription parameterDescription = new ApiParameterDescription();
            parameterDescription.ParameterDescriptor = parameter;
            parameterDescription.Name = parameter.Prefix ?? parameter.ParameterName;
            parameterDescription.Documentation = GetApiParameterDocumentation(parameter);
            parameterDescription.Source = ApiParameterSource.Unknown;
            return parameterDescription;
        }

        private ApiParameterDescription CreateParameterDescriptionFromBinding(HttpParameterBinding parameterBinding)
        {
            ApiParameterDescription parameterDescription = CreateParameterDescriptionFromDescriptor(parameterBinding.Descriptor);
            if (parameterBinding.WillReadBody)
            {
                parameterDescription.Source = ApiParameterSource.FromBody;
            }
            else if (parameterBinding.WillReadUri())
            {
                parameterDescription.Source = ApiParameterSource.FromUri;
            }

            return parameterDescription;
        }

        private string GetApiDocumentation(HttpActionDescriptor actionDescriptor)
        {
            IDocumentationProvider documentationProvider = DocumentationProvider ?? actionDescriptor.Configuration.Services.GetDocumentationProvider();
            if (documentationProvider != null)
            {
                return documentationProvider.GetDocumentation(actionDescriptor);
            }

            return null;
        }

        private string GetApiParameterDocumentation(HttpParameterDescriptor parameterDescriptor)
        {
            IDocumentationProvider documentationProvider = DocumentationProvider ?? parameterDescriptor.Configuration.Services.GetDocumentationProvider();
            if (documentationProvider != null)
            {
                return documentationProvider.GetDocumentation(parameterDescriptor);
            }

            return null;
        }

        private string GetApiResponseDocumentation(HttpActionDescriptor actionDescriptor)
        {
            IDocumentationProvider documentationProvider = DocumentationProvider ?? actionDescriptor.Configuration.Services.GetDocumentationProvider();
            if (documentationProvider != null)
            {
                return documentationProvider.GetResponseDocumentation(actionDescriptor);
            }

            return null;
        }

        // remove ApiDescription that will lead to ambiguous action matching.
        private static Collection<ApiDescription> RemoveInvalidApiDescriptions(Collection<ApiDescription> apiDescriptions)
        {
            HashSet<string> duplicateApiDescriptionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> visitedApiDescriptionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (ApiDescription description in apiDescriptions)
            {
                string apiDescriptionId = description.ID;
                if (visitedApiDescriptionIds.Contains(apiDescriptionId))
                {
                    duplicateApiDescriptionIds.Add(apiDescriptionId);
                }
                else
                {
                    visitedApiDescriptionIds.Add(apiDescriptionId);
                }
            }

            Collection<ApiDescription> filteredApiDescriptions = new Collection<ApiDescription>();
            foreach (ApiDescription apiDescription in apiDescriptions)
            {
                string apiDescriptionId = apiDescription.ID;
                if (!duplicateApiDescriptionIds.Contains(apiDescriptionId))
                {
                    filteredApiDescriptions.Add(apiDescription);
                }
            }

            return filteredApiDescriptions;
        }

        private static bool MatchRegexConstraint(IHttpRoute route, string parameterName, string parameterValue)
        {
            IDictionary<string, object> constraints = route.Constraints;
            if (constraints != null)
            {
                object constraint;
                if (constraints.TryGetValue(parameterName, out constraint))
                {
                    // treat the constraint as a string which represents a Regex.
                    // note that we don't support custom constraint (IHttpRouteConstraint) because it might rely on the request and some runtime states
                    string constraintsRule = constraint as string;
                    if (constraintsRule != null)
                    {
                        string constraintsRegEx = "^(" + constraintsRule + ")$";
                        return parameterValue != null && Regex.IsMatch(parameterValue, constraintsRegEx, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                    }
                }
            }

            return true;
        }

        private static HttpActionBinding GetActionBinding(HttpActionDescriptor actionDescriptor)
        {
            HttpControllerDescriptor controllerDescriptor = actionDescriptor.ControllerDescriptor;
            if (controllerDescriptor == null)
            {
                return null;
            }

            ServicesContainer controllerServices = controllerDescriptor.Configuration.Services;
            IActionValueBinder actionValueBinder = controllerServices.GetActionValueBinder();
            HttpActionBinding actionBinding = actionValueBinder != null ? actionValueBinder.GetBinding(actionDescriptor) : null;
            return actionBinding;
        }

        private sealed class ApiDescriptionComparer : IEqualityComparer<ApiDescription>
        {
            public bool Equals(ApiDescription x, ApiDescription y)
            {
                return String.Equals(x.ID, y.ID, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(ApiDescription obj)
            {
                return obj.ID.ToUpperInvariant().GetHashCode();
            }
        }
    }
}