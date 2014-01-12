﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Mvc
{
    internal sealed class ControllerDescriptorCache : ReaderWriterCache<Type, ControllerDescriptor>
    {
        public ControllerDescriptorCache()
        {
        }

        public ControllerDescriptor GetDescriptor(Type controllerType, Func<ControllerDescriptor> creator)
        {
            return FetchOrCreateItem(controllerType, creator);
        }

        internal ControllerDescriptor GetDescriptor<TArgument>(Type controllerType, Func<TArgument, ControllerDescriptor> creator, TArgument state)
        {
            return FetchOrCreateItem(controllerType, creator, state);
        }
    }
}
