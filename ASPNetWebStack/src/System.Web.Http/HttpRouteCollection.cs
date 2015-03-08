﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Net.Http;
using System.Web.Http.Properties;
using System.Web.Http.Routing;

namespace System.Web.Http
{
    public class HttpRouteCollection : ICollection<IHttpRoute>, IDisposable
    {
        // Arbitrary base address for evaluating the root virtual path
        private static readonly Uri _referenceBaseAddress = new Uri("http://localhost");

        private readonly string _virtualPathRoot;
        private readonly List<IHttpRoute> _collection = new List<IHttpRoute>();
        private readonly IDictionary<string, IHttpRoute> _dictionary = new Dictionary<string, IHttpRoute>(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRouteCollection"/> class with a <see cref="M:VirtualPathRoot"/>
        /// value of "/".
        /// </summary>
        public HttpRouteCollection()
            : this("/")
        {
        }

        [SuppressMessage("Microsoft.Usage", "CA2234:PassSystemUriObjectsInsteadOfStrings", Justification = "Relative URIs are not URIs")]
        public HttpRouteCollection(string virtualPathRoot)
        {
            if (virtualPathRoot == null)
            {
                throw Error.ArgumentNull("virtualPathRoot");
            }

            // Validate virtual path
            Uri address = new Uri(_referenceBaseAddress, virtualPathRoot);
            _virtualPathRoot = "/" + address.GetComponents(UriComponents.Path, UriFormat.Unescaped);
        }

        public virtual string VirtualPathRoot
        {
            get { return _virtualPathRoot; }
        }

        public virtual int Count
        {
            get { return _collection.Count; }
        }

        public virtual bool IsReadOnly
        {
            get { return false; }
        }

        public virtual IHttpRoute this[int index]
        {
            get { return _collection[index]; }
        }

        public virtual IHttpRoute this[string name]
        {
            get { return _dictionary[name]; }
        }

        public virtual IHttpRouteData GetRouteData(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw Error.ArgumentNull("request");
            }

            for (int i = 0; i < _collection.Count; i++)
            {
                string virtualPathRoot = GetVirtualPathRoot(request);
                IHttpRouteData routeData = _collection[i].GetRouteData(virtualPathRoot, request);
                if (routeData != null)
                {
                    return routeData;
                }
            }

            return null;
        }

        public virtual IHttpVirtualPathData GetVirtualPath(HttpRequestMessage request, string name, IDictionary<string, object> values)
        {
            if (request == null)
            {
                throw Error.ArgumentNull("request");
            }

            if (name == null)
            {
                throw Error.ArgumentNull("name");
            }

            IHttpRoute route;
            if (!_dictionary.TryGetValue(name, out route))
            {
                throw Error.Argument("name", SRResources.RouteCollection_NameNotFound, name);
            }
            IHttpVirtualPathData virtualPath = route.GetVirtualPath(request, values);
            if (virtualPath == null)
            {
                return null;
            }

            // Construct a new VirtualPathData with the resolved app path
            string virtualPathRoot = GetVirtualPathRoot(request);
            if (!virtualPathRoot.EndsWith("/", StringComparison.Ordinal))
            {
                virtualPathRoot += "/";
            }

            // Note: The virtual path root here always ends with a "/" and the
            // virtual path never starts with a "/" (that's how routes work).
            return new HttpVirtualPathData(virtualPath.Route, virtualPathRoot + virtualPath.VirtualPath);
        }

        // Returns the virtual path root on the request if one is specified
        // Otherwise, fall back on the virtual path root for the route collection
        private string GetVirtualPathRoot(HttpRequestMessage request)
        {
            return request.GetVirtualPathRoot() ?? _virtualPathRoot;
        }

        public IHttpRoute CreateRoute(string routeTemplate, object defaults, object constraints)
        {
            IDictionary<string, object> dataTokens = new Dictionary<string, object>();

            return CreateRoute(routeTemplate, new HttpRouteValueDictionary(defaults), new HttpRouteValueDictionary(constraints), dataTokens, handler: null);
        }

        public IHttpRoute CreateRoute(string routeTemplate, IDictionary<string, object> defaults, IDictionary<string, object> constraints, IDictionary<string, object> dataTokens)
        {
            return CreateRoute(routeTemplate, defaults, constraints, dataTokens, handler: null);
        }

        public virtual IHttpRoute CreateRoute(string routeTemplate, IDictionary<string, object> defaults, IDictionary<string, object> constraints, IDictionary<string, object> dataTokens, HttpMessageHandler handler)
        {
            HttpRouteValueDictionary routeDefaults = new HttpRouteValueDictionary(defaults);
            HttpRouteValueDictionary routeConstraints = new HttpRouteValueDictionary(constraints);
            HttpRouteValueDictionary routeDataTokens = new HttpRouteValueDictionary(dataTokens);
            return new HttpRoute(routeTemplate, routeDefaults, routeConstraints, routeDataTokens, handler);
        }

        void ICollection<IHttpRoute>.Add(IHttpRoute route)
        {
            throw Error.NotSupported(SRResources.Route_AddRemoveWithNoKeyNotSupported, typeof(HttpRouteCollection).Name);
        }

        public virtual void Add(string name, IHttpRoute route)
        {
            if (name == null)
            {
                throw Error.ArgumentNull("name");
            }

            if (route == null)
            {
                throw Error.ArgumentNull("route");
            }

            _dictionary.Add(name, route);
            _collection.Add(route);
        }

        public virtual void Clear()
        {
            _dictionary.Clear();
            _collection.Clear();
        }

        public virtual bool Contains(IHttpRoute item)
        {
            if (item == null)
            {
                throw Error.ArgumentNull("item");
            }

            return _collection.Contains(item);
        }

        public virtual bool ContainsKey(string name)
        {
            if (name == null)
            {
                throw Error.ArgumentNull("name");
            }

            return _dictionary.ContainsKey(name);
        }

        public virtual void CopyTo(IHttpRoute[] array, int arrayIndex)
        {
            _collection.CopyTo(array, arrayIndex);
        }

        public virtual void CopyTo(KeyValuePair<string, IHttpRoute>[] array, int arrayIndex)
        {
            _dictionary.CopyTo(array, arrayIndex);
        }

        public virtual void Insert(int index, string name, IHttpRoute value)
        {
            if (name == null)
            {
                throw Error.ArgumentNull("name");
            }

            if (value == null)
            {
                throw Error.ArgumentNull("value");
            }

            // Check that index is valid
            if (_collection[index] != null)
            {
                _dictionary.Add(name, value);
                _collection.Insert(index, value);
            }
        }

        bool ICollection<IHttpRoute>.Remove(IHttpRoute route)
        {
            throw Error.NotSupported(SRResources.Route_AddRemoveWithNoKeyNotSupported, typeof(HttpRouteCollection).Name);
        }

        public virtual bool Remove(string name)
        {
            if (name == null)
            {
                throw Error.ArgumentNull("name");
            }

            IHttpRoute value;
            if (_dictionary.TryGetValue(name, out value))
            {
                bool dictionaryRemove = _dictionary.Remove(name);
                bool collectionRemove = _collection.Remove(value);
                Contract.Assert(dictionaryRemove == collectionRemove);
                return dictionaryRemove;
            }

            return false;
        }

        public virtual IEnumerator<IHttpRoute> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return OnGetEnumerator();
        }

        protected virtual IEnumerator OnGetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        public virtual bool TryGetValue(string name, out IHttpRoute route)
        {
            return _dictionary.TryGetValue(name, out route);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
