﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.Contracts;
using System.Web.Hosting;
using System.Web.WebPages;

namespace System.Web.Mvc
{
    public abstract class BuildManagerViewEngine : VirtualPathProviderViewEngine
    {
        private static FileExistenceCache _sharedFileExistsCache;

        private IBuildManager _buildManager;
        private IViewPageActivator _viewPageActivator;
        private IResolver<IViewPageActivator> _activatorResolver;
        private FileExistenceCache _fileExistsCache;

        protected BuildManagerViewEngine()
            : this(null, null, null, null)
        {
        }

        protected BuildManagerViewEngine(IViewPageActivator viewPageActivator)
            : this(viewPageActivator, null, null, null)
        {
        }

        internal BuildManagerViewEngine(IViewPageActivator viewPageActivator, IResolver<IViewPageActivator> activatorResolver, IDependencyResolver dependencyResolver, VirtualPathProvider pathProvider)
        {
            if (viewPageActivator != null)
            {
                _viewPageActivator = viewPageActivator;
            }
            else
            {
                _activatorResolver = activatorResolver ?? new SingleServiceResolver<IViewPageActivator>(
                                                              () => null,
                                                              new DefaultViewPageActivator(dependencyResolver),
                                                              "BuildManagerViewEngine constructor");
            }
            if (pathProvider != null)
            {
                _fileExistsCache = new FileExistenceCache(pathProvider);
            }
            else
            {
                if (_sharedFileExistsCache == null)
                {
                    VirtualPathProvider defaultPathProvider = HostingEnvironment.VirtualPathProvider;
                    // Path provider may not be present in test context.
                    if (defaultPathProvider != null)
                    {
                        // Startup initialization race is OK providing service remains read-only
                        _sharedFileExistsCache = new FileExistenceCache(HostingEnvironment.VirtualPathProvider);
                    }
                }
                _fileExistsCache = _sharedFileExistsCache;
            }
        }

        internal IBuildManager BuildManager
        {
            get
            {
                if (_buildManager == null)
                {
                    _buildManager = new BuildManagerWrapper();
                }
                return _buildManager;
            }
            set { _buildManager = value; }
        }

        protected IViewPageActivator ViewPageActivator
        {
            get
            {
                if (_viewPageActivator != null)
                {
                    return _viewPageActivator;
                }
                _viewPageActivator = _activatorResolver.Current;
                return _viewPageActivator;
            }
        }

        protected override bool FileExists(ControllerContext controllerContext, string virtualPath)
        {
            Contract.Assert(_fileExistsCache != null);
            return _fileExistsCache.FileExists(virtualPath);
        }

        internal class DefaultViewPageActivator : IViewPageActivator
        {
            private Func<IDependencyResolver> _resolverThunk;

            public DefaultViewPageActivator()
                : this(null)
            {
            }

            public DefaultViewPageActivator(IDependencyResolver resolver)
            {
                if (resolver == null)
                {
                    _resolverThunk = () => DependencyResolver.Current;
                }
                else
                {
                    _resolverThunk = () => resolver;
                }
            }

            public object Create(ControllerContext controllerContext, Type type)
            {
                return _resolverThunk().GetService(type) ?? Activator.CreateInstance(type);
            }
        }
    }
}
