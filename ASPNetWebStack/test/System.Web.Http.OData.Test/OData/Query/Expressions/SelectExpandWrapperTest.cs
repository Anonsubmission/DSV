﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Web.Http.TestCommon;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;
using Microsoft.TestCommon;

namespace System.Web.Http.OData.Query.Expressions
{
    public class SelectExpandWrapperTest
    {
        private CustomersModelWithInheritance _model;
        private string _modelID;

        public SelectExpandWrapperTest()
        {
            _model = new CustomersModelWithInheritance();
            _modelID = ModelContainer.GetModelID(_model.Model);
        }

        [Fact]
        public void Property_Instance_RoundTrips()
        {
            SelectExpandWrapper<TestEntity> wrapper = new SelectExpandWrapper<TestEntity>();
            Assert.Reflection.Property(wrapper, w => w.Instance, expectedDefaultValue: null, allowNull: true, roundTripTestValue: new TestEntity());
        }

        [Fact]
        public void Property_Container_RoundTrips()
        {
            SelectExpandWrapper<TestEntity> wrapper = new SelectExpandWrapper<TestEntity>();

            Assert.Reflection.Property(
                wrapper, w => w.Container, expectedDefaultValue: null, allowNull: true, roundTripTestValue: new MockPropertyContainer());
        }

        [Fact]
        public void GetEdmType_Returns_TypeFromTypeNameIfNotNull()
        {
            SelectExpandWrapper<int> wrapper = new SelectExpandWrapper<int> { TypeName = _model.Customer.FullName(), ModelID = _modelID };

            IEdmTypeReference result = wrapper.GetEdmType();

            Assert.Same(_model.Customer, result.Definition);
        }

        [Fact]
        public void GetEdmType_ThrowsODataException_IfTypeFromTypeNameIsNotFoundInModel()
        {
            _modelID = ModelContainer.GetModelID(EdmCoreModel.Instance);
            SelectExpandWrapper<int> wrapper = new SelectExpandWrapper<int> { TypeName = _model.Customer.FullName(), ModelID = _modelID };

            Assert.Throws<InvalidOperationException>(
                () => wrapper.GetEdmType(),
                "Cannot find the entity type 'NS.Customer' in the model.");
        }

        [Fact]
        public void GetEdmType_Returns_InstanceType()
        {
            _model.Model.SetAnnotationValue(_model.Customer, new ClrTypeAnnotation(typeof(TestEntity)));
            _model.Model.SetAnnotationValue(_model.SpecialCustomer, new ClrTypeAnnotation(typeof(DerivedEntity)));
            SelectExpandWrapper<TestEntity> wrapper = new SelectExpandWrapper<TestEntity> { ModelID = _modelID };
            wrapper.Instance = new DerivedEntity();

            IEdmTypeReference edmType = wrapper.GetEdmType();

            Assert.Same(_model.SpecialCustomer, edmType.Definition);
        }

        [Fact]
        public void GetEdmType_Returns_ElementTypeIfInstanceIsNull()
        {
            _model.Model.SetAnnotationValue(_model.Customer, new ClrTypeAnnotation(typeof(TestEntity)));
            _model.Model.SetAnnotationValue(_model.SpecialCustomer, new ClrTypeAnnotation(typeof(DerivedEntity)));
            SelectExpandWrapper<TestEntity> wrapper = new SelectExpandWrapper<TestEntity> { ModelID = _modelID };

            IEdmTypeReference edmType = wrapper.GetEdmType();

            Assert.Same(_model.Customer, edmType.Definition);
        }

        [Fact]
        public void TryGetValue_ReturnsValueFromPropertyContainer_IfPresent()
        {
            object expectedPropertyValue = new object();
            MockPropertyContainer container = new MockPropertyContainer();
            container.Properties.Add("SampleProperty", expectedPropertyValue);
            SelectExpandWrapper<TestEntity> wrapper = new SelectExpandWrapper<TestEntity> { Container = container };
            wrapper.Instance = new TestEntity();

            object value;
            bool result = wrapper.TryGetPropertyValue("SampleProperty", out value);

            Assert.True(result);
            Assert.Same(expectedPropertyValue, value);
        }

        [Fact]
        public void TryGetValue_ReturnsValueFromInstance_IfNotPresentInContainer()
        {
            object expectedPropertyValue = new object();
            MockPropertyContainer container = new MockPropertyContainer();
            SelectExpandWrapper<TestEntity> wrapper = new SelectExpandWrapper<TestEntity> { Container = container };
            wrapper.Instance = new TestEntity { SampleProperty = expectedPropertyValue };

            object value;
            bool result = wrapper.TryGetPropertyValue("SampleProperty", out value);

            Assert.True(result);
            Assert.Same(expectedPropertyValue, value);
        }

        [Fact]
        public void TryGetValue_ReturnsValueFromInstance_IfContainerIsNull()
        {
            object expectedPropertyValue = new object();
            SelectExpandWrapper<TestEntity> wrapper = new SelectExpandWrapper<TestEntity>();
            wrapper.Instance = new TestEntity { SampleProperty = expectedPropertyValue };

            object value;
            bool result = wrapper.TryGetPropertyValue("SampleProperty", out value);

            Assert.True(result);
            Assert.Same(expectedPropertyValue, value);
        }

        [Fact]
        public void TryGetValue_ReturnsFalse_IfContainerAndInstanceAreNull()
        {
            SelectExpandWrapper<TestEntity> wrapper = new SelectExpandWrapper<TestEntity>();

            object value;
            bool result = wrapper.TryGetPropertyValue("SampleProperty", out value);

            Assert.False(result);
        }

        [Fact]
        public void TryGetValue_ReturnsFalse_IfPropertyNotPresentInElement()
        {
            SelectExpandWrapper<TestEntity> wrapper = new SelectExpandWrapper<TestEntity>();

            object value;
            bool result = wrapper.TryGetPropertyValue("SampleNotPresentProperty", out value);

            Assert.False(result);
        }

        [Fact]
        public void ToDictionary_ContainsAllStructuralProperties_IfInstanceIsNotNull()
        {
            // Arrange
            EdmModel model = new EdmModel();
            EdmEntityType entityType = new EdmEntityType("NS", "Name");
            model.AddElement(entityType);
            model.SetAnnotationValue(entityType, new ClrTypeAnnotation(typeof(TestEntity)));
            entityType.AddStructuralProperty("SampleProperty", EdmPrimitiveTypeKind.Int32);
            IEdmTypeReference edmType = new EdmEntityTypeReference(entityType, isNullable: false);
            SelectExpandWrapper<TestEntity> testWrapper = new SelectExpandWrapper<TestEntity>
            {
                Instance = new TestEntity { SampleProperty = 42 },
                ModelID = ModelContainer.GetModelID(model)
            };

            // Act
            var result = testWrapper.ToDictionary();

            // Assert
            Assert.Equal(42, result["SampleProperty"]);
        }

        [Fact]
        public void ToDictionary_ContainsAllProperties_FromContainer()
        {
            // Arrange
            MockPropertyContainer container = new MockPropertyContainer();
            container.Properties.Add("Property", 42);
            SelectExpandWrapper<TestEntity> wrapper = new SelectExpandWrapper<TestEntity> { Container = container };

            // Act
            var result = wrapper.ToDictionary();

            // Assert
            Assert.Equal(42, result["Property"]);
        }

        private class MockPropertyContainer : PropertyContainer
        {
            public MockPropertyContainer()
            {
                Properties = new Dictionary<string, object>();
            }

            public Dictionary<string, object> Properties { get; private set; }

            public override void ToDictionaryCore(Dictionary<string, object> dictionary, bool includeAutoSelected)
            {
                foreach (var kvp in Properties)
                {
                    dictionary.Add(kvp.Key, kvp.Value);
                }
            }
        }

        private class TestEntity
        {
            public object SampleProperty { get; set; }
        }

        private class DerivedEntity : TestEntity
        {
        }
    }
}
