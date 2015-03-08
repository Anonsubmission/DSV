﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Mvc.Routing;
using System.Web.Routing;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Mvc.Async.Test
{
    public class AsyncActionMethodSelectorTest
    {
        [Fact]
        public void AliasedMethodsProperty()
        {
            // Arrange
            Type controllerType = typeof(MethodLocatorController);

            // Act
            AsyncActionMethodSelector selector = new AsyncActionMethodSelector(controllerType);

            // Assert
            Assert.Equal(3, selector.AliasedMethods.Length);

            List<MethodInfo> sortedAliasedMethods = selector.AliasedMethods.OrderBy(methodInfo => methodInfo.Name).ToList();
            Assert.Equal("Bar", sortedAliasedMethods[0].Name);
            Assert.Equal("FooRenamed", sortedAliasedMethods[1].Name);
            Assert.Equal("Renamed", sortedAliasedMethods[2].Name);
        }

        [Fact]
        public void ControllerTypeProperty()
        {
            // Arrange
            Type controllerType = typeof(MethodLocatorController);
            AsyncActionMethodSelector selector = new AsyncActionMethodSelector(controllerType);

            // Act & Assert
            Assert.Same(controllerType, selector.ControllerType);
        }

        [Fact]
        public void FindAction_DoesNotMatchAsyncMethod()
        {
            // Arrange
            Type controllerType = typeof(MethodLocatorController);
            AsyncActionMethodSelector selector = new AsyncActionMethodSelector(controllerType);

            // Act
            ActionDescriptorCreator creator = selector.FindAction(new ControllerContext(), "EventPatternAsync");

            // Assert
            Assert.Null(creator);
        }

        [Fact]
        public void FindAction_DoesNotMatchCompletedMethod()
        {
            // Arrange
            Type controllerType = typeof(MethodLocatorController);
            AsyncActionMethodSelector selector = new AsyncActionMethodSelector(controllerType);

            // Act
            ActionDescriptorCreator creator = selector.FindAction(new ControllerContext(), "EventPatternCompleted");

            // Assert
            Assert.Null(creator);
        }

        [Fact]
        public void FindAction_ReturnsMatchingMethodIfOneMethodMatches()
        {
            // Arrange
            Type controllerType = typeof(SelectionAttributeController);
            AsyncActionMethodSelector selector = new AsyncActionMethodSelector(controllerType);

            // Act
            ActionDescriptorCreator creator = selector.FindAction(new ControllerContext(), "OneMatch");
            ActionDescriptor actionDescriptor = creator("someName", new Mock<ControllerDescriptor>().Object);

            // Assert
            var castActionDescriptor = Assert.IsType<ReflectedActionDescriptor>(actionDescriptor);
            Assert.Equal("OneMatch", castActionDescriptor.MethodInfo.Name);
            Assert.Equal(typeof(string), castActionDescriptor.MethodInfo.GetParameters()[0].ParameterType);
        }

        [Fact]
        public void FindAction_ReturnsMethodWithActionSelectionAttributeIfMultipleMethodsMatchRequest()
        {
            // DevDiv Bugs 212062: If multiple action methods match a request, we should match only the methods with an
            // [ActionMethod] attribute since we assume those methods are more specific.

            // Arrange
            Type controllerType = typeof(SelectionAttributeController);
            AsyncActionMethodSelector selector = new AsyncActionMethodSelector(controllerType);

            // Act
            ActionDescriptorCreator creator = selector.FindAction(new ControllerContext(), "ShouldMatchMethodWithSelectionAttribute");
            ActionDescriptor actionDescriptor = creator("someName", new Mock<ControllerDescriptor>().Object);

            // Assert
            var castActionDescriptor = Assert.IsType<ReflectedActionDescriptor>(actionDescriptor);
            Assert.Equal("MethodHasSelectionAttribute1", castActionDescriptor.MethodInfo.Name);
        }

        [Fact]
        public void FindAction_ReturnsNullIfNoMethodMatches()
        {
            // Arrange
            Type controllerType = typeof(SelectionAttributeController);
            AsyncActionMethodSelector selector = new AsyncActionMethodSelector(controllerType);

            // Act
            ActionDescriptorCreator creator = selector.FindAction(new ControllerContext(), "ZeroMatch");

            // Assert
            Assert.Null(creator);
        }

        [Fact]
        public void FindAction_ThrowsIfMultipleMethodsMatch()
        {
            // Arrange
            Type controllerType = typeof(SelectionAttributeController);
            AsyncActionMethodSelector selector = new AsyncActionMethodSelector(controllerType);

            // Act & veriy
            Assert.Throws<AmbiguousMatchException>(
                delegate { selector.FindAction(new ControllerContext(), "TwoMatch"); },
                "The current request for action 'TwoMatch' on controller type 'SelectionAttributeController' is ambiguous between the following action methods:" + Environment.NewLine
              + "Void TwoMatch2() on type System.Web.Mvc.Async.Test.AsyncActionMethodSelectorTest+SelectionAttributeController" + Environment.NewLine
              + "Void TwoMatch() on type System.Web.Mvc.Async.Test.AsyncActionMethodSelectorTest+SelectionAttributeController");
        }

        [Fact]
        public void FindActionMethod_Asynchronous()
        {
            // Arrange
            Type controllerType = typeof(MethodLocatorController);
            AsyncActionMethodSelector selector = new AsyncActionMethodSelector(controllerType);

            // Act
            ActionDescriptorCreator creator = selector.FindAction(new ControllerContext(), "EventPattern");
            ActionDescriptor actionDescriptor = creator("someName", new Mock<ControllerDescriptor>().Object);

            // Assert
            var castActionDescriptor = Assert.IsType<ReflectedAsyncActionDescriptor>(actionDescriptor);
            Assert.Equal("EventPatternAsync", castActionDescriptor.AsyncMethodInfo.Name);
            Assert.Equal("EventPatternCompleted", castActionDescriptor.CompletedMethodInfo.Name);
        }

        [Fact]
        public void FindActionMethod_Task()
        {
            // Arrange
            Type controllerType = typeof(MethodLocatorController);
            AsyncActionMethodSelector selector = new AsyncActionMethodSelector(controllerType);

            // Act
            ActionDescriptorCreator creator = selector.FindAction(new ControllerContext(), "TaskPattern");
            ActionDescriptor actionDescriptor = creator("someName", new Mock<ControllerDescriptor>().Object);

            // Assert
            var castActionDescriptor = Assert.IsType<TaskAsyncActionDescriptor>(actionDescriptor);
            Assert.Equal("TaskPattern", castActionDescriptor.TaskMethodInfo.Name);
            Assert.Equal(typeof(Task), castActionDescriptor.TaskMethodInfo.ReturnType);
        }

        [Fact]
        public void FindActionMethod_GenericTask()
        {
            // Arrange
            Type controllerType = typeof(MethodLocatorController);
            AsyncActionMethodSelector selector = new AsyncActionMethodSelector(controllerType);

            // Act
            ActionDescriptorCreator creator = selector.FindAction(new ControllerContext(), "GenericTaskPattern");
            ActionDescriptor actionDescriptor = creator("someName", new Mock<ControllerDescriptor>().Object);

            // Assert
            var castActionDescriptor = Assert.IsType<TaskAsyncActionDescriptor>(actionDescriptor);
            Assert.Equal("GenericTaskPattern", castActionDescriptor.TaskMethodInfo.Name);
            Assert.Equal(typeof(Task<string>), castActionDescriptor.TaskMethodInfo.ReturnType);
        }

        [Fact]
        public void FindActionMethod_Asynchronous_ThrowsIfCompletionMethodNotFound()
        {
            // Arrange
            Type controllerType = typeof(MethodLocatorController);
            AsyncActionMethodSelector selector = new AsyncActionMethodSelector(controllerType);

            // Act & assert
            Assert.Throws<InvalidOperationException>(
                delegate { ActionDescriptorCreator creator = selector.FindAction(new ControllerContext(), "EventPatternWithoutCompletionMethod"); },
                @"Could not locate a method named 'EventPatternWithoutCompletionMethodCompleted' on controller type System.Web.Mvc.Async.Test.AsyncActionMethodSelectorTest+MethodLocatorController.");
        }

        [Fact]
        public void FindActionMethod_Asynchronous_ThrowsIfMultipleCompletedMethodsMatched()
        {
            // Arrange
            Type controllerType = typeof(MethodLocatorController);
            AsyncActionMethodSelector selector = new AsyncActionMethodSelector(controllerType);

            // Act & assert
            Assert.Throws<AmbiguousMatchException>(
                delegate { ActionDescriptorCreator creator = selector.FindAction(new ControllerContext(), "EventPatternAmbiguous"); },
                "Lookup for method 'EventPatternAmbiguousCompleted' on controller type 'MethodLocatorController' failed because of an ambiguity between the following methods:" + Environment.NewLine
              + "Void EventPatternAmbiguousCompleted(Int32) on type System.Web.Mvc.Async.Test.AsyncActionMethodSelectorTest+MethodLocatorController" + Environment.NewLine
              + "Void EventPatternAmbiguousCompleted(System.String) on type System.Web.Mvc.Async.Test.AsyncActionMethodSelectorTest+MethodLocatorController");
        }

        [Fact]
        public void FindAction_NullContext_Throws()
        {
            // Arrange
            Type controllerType = typeof(WithRoutingAttributeController);
            AsyncActionMethodSelector selector = new AsyncActionMethodSelector(controllerType);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => selector.FindAction(null, "Action"));
        }

        [Fact]
        public void FindAction_MultipleMethodsSameActionOneWithRouteAttributeAndRouteWasMatched_ReturnsMethodWithRoutingAttribute()
        {
            // Arrange
            Type controllerType = typeof(WithRoutingAttributeController);
            AsyncActionMethodSelector selector = new AsyncActionMethodSelector(controllerType);

            var context = new ControllerContext();
            context.RouteData = new RouteData();
            context.RouteData.Route = DirectRouteTestHelpers.BuildDirectRouteStubsFrom<WithRoutingAttributeController>(c => c.Action())[0];

            // Act
            ActionDescriptor matchedAction = selector.FindAction(context, "Action")("Action", new ReflectedAsyncControllerDescriptor(controllerType));
            var matchedMethod = ((ReflectedActionDescriptor)matchedAction).MethodInfo;

            // Assert
            Assert.Equal(context.RouteData.GetTargetActionMethod(), matchedMethod);
        }


        [Fact]
        public void NonAliasedMethodsProperty()
        {
            // Arrange
            Type controllerType = typeof(MethodLocatorController);

            // Act
            AsyncActionMethodSelector selector = new AsyncActionMethodSelector(controllerType);

            // Assert
            Assert.Equal(6, selector.NonAliasedMethods.Count);

            List<MethodInfo> sortedMethods = selector.NonAliasedMethods["foo"].OrderBy(methodInfo => methodInfo.GetParameters().Length).ToList();
            Assert.Equal("Foo", sortedMethods[0].Name);
            Assert.Empty(sortedMethods[0].GetParameters());
            Assert.Equal("Foo", sortedMethods[1].Name);
            Assert.Equal(typeof(string), sortedMethods[1].GetParameters()[0].ParameterType);

            Assert.Equal(1, selector.NonAliasedMethods["EventPattern"].Count());
            Assert.Equal("EventPatternAsync", selector.NonAliasedMethods["EventPattern"].First().Name);
            Assert.Equal(1, selector.NonAliasedMethods["EventPatternAmbiguous"].Count());
            Assert.Equal("EventPatternAmbiguousAsync", selector.NonAliasedMethods["EventPatternAmbiguous"].First().Name);
            Assert.Equal(1, selector.NonAliasedMethods["EventPatternWithoutCompletionMethod"].Count());
            Assert.Equal("EventPatternWithoutCompletionMethodAsync", selector.NonAliasedMethods["EventPatternWithoutCompletionMethod"].First().Name);

            Assert.Equal(1, selector.NonAliasedMethods["TaskPattern"].Count());
            Assert.Equal("TaskPattern", selector.NonAliasedMethods["TaskPattern"].First().Name);
            Assert.Equal(1, selector.NonAliasedMethods["GenericTaskPattern"].Count());
            Assert.Equal("GenericTaskPattern", selector.NonAliasedMethods["GenericTaskPattern"].First().Name);
        }

        private class MethodLocatorController : Controller
        {
            public void Foo()
            {
            }

            public void Foo(string s)
            {
            }

            [ActionName("Foo")]
            public void FooRenamed()
            {
            }

            [ActionName("Bar")]
            public void Bar()
            {
            }

            [ActionName("PrivateVoid")]
            private void PrivateVoid()
            {
            }

            protected void ProtectedVoidAction()
            {
            }

            public static void StaticMethod()
            {
            }

            public void EventPatternAsync()
            {
            }

            public void EventPatternCompleted()
            {
            }

            public void EventPatternWithoutCompletionMethodAsync()
            {
            }

            public void EventPatternAmbiguousAsync()
            {
            }

            public void EventPatternAmbiguousCompleted(int i)
            {
            }

            public void EventPatternAmbiguousCompleted(string s)
            {
            }

            public Task TaskPattern()
            {
                return Task.Factory.StartNew(() => "foo");
            }

            public Task<string> GenericTaskPattern()
            {
                return Task.Factory.StartNew(() => "foo");
            }

            [ActionName("RenamedCompleted")]
            public void Renamed()
            {
            }

            // ensure that methods inheriting from Controller or a base class are not matched
            [ActionName("Blah")]
            protected override void ExecuteCore()
            {
                throw new NotImplementedException();
            }

            public string StringProperty { get; set; }

#pragma warning disable 0067
            public event EventHandler<EventArgs> SomeEvent;
#pragma warning restore 0067
        }

        private class SelectionAttributeController : Controller
        {
            [Match(false)]
            public void OneMatch()
            {
            }

            public void OneMatch(string s)
            {
            }

            public void TwoMatch()
            {
            }

            [ActionName("TwoMatch")]
            public void TwoMatch2()
            {
            }

            [Match(true), ActionName("ShouldMatchMethodWithSelectionAttribute")]
            public void MethodHasSelectionAttribute1()
            {
            }

            [ActionName("ShouldMatchMethodWithSelectionAttribute")]
            public void MethodDoesNotHaveSelectionAttribute1()
            {
            }

            private class MatchAttribute : ActionMethodSelectorAttribute
            {
                private bool _match;

                public MatchAttribute(bool match)
                {
                    _match = match;
                }

                public override bool IsValidForRequest(ControllerContext controllerContext, MethodInfo methodInfo)
                {
                    return _match;
                }
            }
        }

        private class WithRoutingAttributeController : Controller
        {
            [HttpRoute("route")]
            [ActionName("Action")] // to make things confusing
            public void ActionWithoutRoute()
            {
            }

            [HttpRoute("route")]
            public void Action()
            {
            }
        }
    }
}
