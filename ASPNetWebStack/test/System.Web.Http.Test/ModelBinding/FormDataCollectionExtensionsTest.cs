﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http.Controllers;
using System.Web.Http.ValueProviders;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Http.ModelBinding
{
    public class FormDataCollectionExtensionsTest
    {
        [Theory]
        [InlineData("", null)]
        [InlineData("", "")] // empty 
        [InlineData("x", "x")] // normal key
        [InlineData("", "[]")] // trim []
        [InlineData("x", "x[]")] // trim []
        [InlineData("x[234]", "x[234]")] // array index
        [InlineData("x.y", "x[y]")] // field lookup
        [InlineData("x.y.z", "x[y][z]")] // nested field lookup
        [InlineData("x.y[234].x", "x[y][234][x]")] // compound
        public void TestNormalize(string expectedMvc, string jqueryString)
        {
            Assert.Equal(expectedMvc, FormDataCollectionExtensions.NormalizeJQueryToMvc(jqueryString));            
        }

        [Fact]
        public void ReadIntArray()
        {
            // No key name means the top level object is an array
            int[] result = ParseJQuery<int[]>("=30&=40&=50");

            Assert.Equal(new int[] { 30,40,50 } , result);
        }

        [Fact]
        public void ReadIntArrayWithBrackets()
        {
            // brackets for explicit array
            int[] result = ParseJQuery<int[]>("[]=30&[]=40&[]=50");

            Assert.Equal(new int[] { 30, 40, 50 }, result);
        }

        [Fact]
        public void ReadIntArrayFromSingleElement()
        {
            // No key name means the top level object is an array
            int[] result = ParseJQuery<int[]>("=30");

            Assert.Equal(new int[] { 30 }, result);
        }

        [Fact]
        public void ReadClassWithIntArray()
        {
            // specifying key name 'x=30' means that we have a field named x. 
            // multiple x keys mean that field is an array.
            var result = ParseJQuery<ClassWithArrayField>("x=30&x=40&x=50");
                        
            Assert.Equal(new int[] { 30, 40, 50 }, result.x);
        }
        
        public class ComplexType
        {
            public string Str { get; set; }
            public int I { get; set; }
            public Point P { get; set; }
        }
        public class Point
        {
            public int X { get; set; }
            public int Y { get; set; }
        }

        [Fact]
        public void ReadClassWithFields()
        {
            // Basic container class with multiple fields
            var result = ParseJQuery<Point>("X=3&Y=4");            
            Assert.Equal(3, result.X);
            Assert.Equal(4, result.Y);
        }

        [Fact]
        public void ReadClassWithFieldsFromUri()
        {
            var uri = new Uri("http://foo.com/?X=3&Y=4&Z=5");
            FormDataCollection fd = new FormDataCollection(uri);
            var result = fd.ReadAs<Point>();

            Assert.Equal(3, result.X);
            Assert.Equal(4, result.Y);
        }

        [Fact]
        public void ReadClassWithFieldsAndPartialBind()
        {
            // Basic container class with multiple fields
            // Extra Z=5 field, ignored since we're reading point. 
            var result = ParseJQuery<Point>("X=3&Y=4&Z=5");
            Assert.Equal(3, result.X);
            Assert.Equal(4, result.Y);
        }

        public class Nest
        {
            public Nest A { get; set; }
        }

        [Fact]
        public void ReadDeeplyNestedFormUrlThrows()
        {
            StringBuilder sb = new StringBuilder("A");
            for (int i = 0; i < 10000; i++)
            {
                sb.Append("[A]");
            }
            sb.Append("=1");

            Assert.Throws<InsufficientExecutionStackException>(() => ParseJQuery<Nest>(sb.ToString()));
        }

        [Fact]
        public void ReadDeeplyNestedMvcThrows()
        {
            StringBuilder sb = new StringBuilder("A");
            for (int i = 0; i < 10000; i++)
            {
                sb.Append(".A");
            }
            sb.Append("=1");

            Assert.Throws<InsufficientExecutionStackException>(() => ParseJQuery<Nest>(sb.ToString()));
        }

        public class ClassWithPointArray
        {
            public Point[] Data { get; set; }
        }

        [Fact]
        public void ReadArrayOfClasses()
        {
            // Array of classes. 
            string s = "Data[0][X]=10&Data[0][Y]=20&Data[1][X]=30&Data[1][Y]=40";
            var result = ParseJQuery<ClassWithPointArray>(s);

            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Length);
            Assert.Equal(10, result.Data[0].X);
            Assert.Equal(20, result.Data[0].Y);
            Assert.Equal(30, result.Data[1].X);
            Assert.Equal(40, result.Data[1].Y);
        }

        
        [Fact]
        public void ReadComplexNestedType()
        {
            var result = ParseJQuery<ComplexType>("Str=Hello+world&I=123&P[X]=3&P[Y]=4");
            Assert.Equal("Hello world", result.Str);
            Assert.Equal(123, result.I);
            Assert.NotNull(result.P); // failed to find P
            Assert.Equal(3, result.P.X);
            Assert.Equal(4, result.P.Y);
        }



        class ComplexType2
        {
            public class Epsilon
            {
                public int[] f { get; set; }
            }

            public class Beta
            {
                public int c { get; set; }
                public int d { get; set; }
            }

            public int[] a { get; set; }
            public Beta[] b { get; set; }
            public Epsilon e { get; set; }
        }

        [Fact]
        public void ReadComplexNestedType2()
        {
            // Jquery encoding from this JSON: "{a:[1,2],b:[{c:3,d:4},{c:5,d:6}],e:{f:[7,8,9]}}";
            string s = "a[]=1&a[]=2&b[0][c]=3&b[0][d]=4&b[1][c]=5&b[1][d]=6&e[f][]=7&e[f][]=8&e[f][]=9";
            var result = ParseJQuery<ComplexType2>(s);

            Assert.NotNull(result);
            Assert.Equal(new int[] { 1, 2 }, result.a);
            Assert.Equal(2, result.b.Length);
            Assert.Equal(3, result.b[0].c);
            Assert.Equal(4, result.b[0].d);
            Assert.Equal(5, result.b[1].c);
            Assert.Equal(6, result.b[1].d);
            Assert.Equal(new int[] { 7, 8, 9 }, result.e.f);
        }
                
        [Fact]
        public void ReadJaggedArray()
        {
            string s = "[0][]=9&[0][]=10&[1][]=11&[1][]=12&[2][]=13&[2][]=14";
            var result = ParseJQuery<int[][]>(s);

            Assert.Equal(9, result[0][0]);
            Assert.Equal(10, result[0][1]);
            Assert.Equal(11, result[1][0]);
            Assert.Equal(12, result[1][1]);
            Assert.Equal(13, result[2][0]);
            Assert.Equal(14, result[2][1]);            
        }

        [Fact]
        public void ReadMultipleParameters()
        {
            // Basic container class with multiple fields
            HttpContent content = FormContent("X=3&Y=4");
            FormDataCollection fd = content.ReadAsAsync<FormDataCollection>().Result;

            Assert.Equal(3, fd.ReadAs<int>("X", requiredMemberSelector: null, formatterLogger: null));
            Assert.Equal("3", fd.ReadAs<string>("X", requiredMemberSelector: null, formatterLogger: null));
            Assert.Equal(4, fd.ReadAs<int>("Y", requiredMemberSelector: null, formatterLogger: null));            
        }

        [Fact]
        public void ReadInvalidInt_ReturnsDefaultValue()
        {
            int result = ParseJQuery<int>("xyz");
            Assert.Equal(0, result);
        }

        [Fact]
        public void ReadForThrowingSetterTypeRecordsCorrectModelError()
        {
            HttpContent content = FormContent("Throws=text");
            FormDataCollection formData = content.ReadAsAsync<FormDataCollection>().Result;
            Mock<IFormatterLogger> mockLogger = new Mock<IFormatterLogger>();

            formData.ReadAs<ThrowingSetterType>(String.Empty, requiredMemberSelector: null, formatterLogger: mockLogger.Object);
            
            mockLogger.Verify(mock => mock.LogError("Throws", ThrowingSetterType.Exception));
        }

        [Fact]
        public void ReadAs_NullActionContextThrows()
        {
            // Arrange
            HttpContent content = FormContent("=30");
            FormDataCollection formData = content.ReadAsAsync<FormDataCollection>().Result;

            // Act/Assert
            Assert.Throws<ArgumentNullException>(() => formData.ReadAs<int>((HttpActionContext)null));
        }

        [Fact]
        public void ReadAs_WithHttpActionContext()
        {
            // Arrange
            int expected = 30;
            HttpContent content = FormContent("=30");
            FormDataCollection formData = content.ReadAsAsync<FormDataCollection>().Result;

            using (HttpConfiguration configuration = new HttpConfiguration())
            {
                HttpActionContext actionContext = CreateActionContext(configuration);

                // Act
                int actual = formData.ReadAs<int>(actionContext);

                // Assert
                Assert.Equal<int>(expected, actual);
            }
        }

        [Fact]
        public void ReadAs_WithModelNameAndHttpActionContext()
        {
            // Arrange
            int expected = 30;
            HttpContent content = FormContent("a=30");
            FormDataCollection formData = content.ReadAsAsync<FormDataCollection>().Result;

            using (HttpConfiguration configuration = new HttpConfiguration())
            {
                HttpActionContext actionContext = CreateActionContext(configuration);

                // Act
                int actual = (int)formData.ReadAs(typeof(int), "a", actionContext);

                // Assert
                Assert.Equal<int>(expected, actual);
            }
        }

        // This test verifies the user scenario behind codeplex-999 - ReadAs should take HttpActionContext
        // as a parameter to make use of ModelBinders in the configuration.
        [Fact]
        public void Read_As_WithHttpActionContextAndCustomModelBinder()
        {
            // Arrange
            int expected = 15;
            HttpContent content = FormContent("a=30");
            FormDataCollection formData = content.ReadAsAsync<FormDataCollection>().Result;

            using (HttpConfiguration configuration = new HttpConfiguration())
            {
                configuration.Services.Insert(typeof(ModelBinderProvider), 0, new CustomIntModelBinderProvider());
                
                HttpActionContext actionContext = CreateActionContext(configuration);

                // Act
                int actual = (int)formData.ReadAs(typeof(int), "a", actionContext);

                // Assert
                Assert.Equal<int>(expected, actual);
            }
        }

        private static HttpActionContext CreateActionContext(HttpConfiguration configuration)
        {
            HttpControllerContext controllerContext = new HttpControllerContext()
            { 
                Configuration = configuration,
                ControllerDescriptor = new HttpControllerDescriptor(configuration),
            };

            return new HttpActionContext { ControllerContext = controllerContext };
        }

        private static HttpContent FormContent(string s)
        {
            HttpContent content = new StringContent(s);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            return content;
        }

        private T ParseJQuery<T>(string jquery)
        {
            HttpContent content = FormContent(jquery);
            FormDataCollection fd = content.ReadAsAsync<FormDataCollection>().Result;
            T result = fd.ReadAs<T>();
            return result;
        }

        private class CustomIntModelBinder : IModelBinder
        {
            public bool BindModel(HttpActionContext actionContext, ModelBindingContext bindingContext)
            {
                ValueProviderResult valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
                int result = (int)valueResult.ConvertTo(typeof(int));

                bindingContext.Model = result / 2;
                return true;
            }
        }

        private class CustomIntModelBinderProvider : ModelBinderProvider
        {
            public override IModelBinder GetBinder(HttpConfiguration configuration, Type modelType)
            {
                if (modelType == typeof(int))
                {
                    return new CustomIntModelBinder();
                }
                else
                {
                    return null;
                }
            }
        }

        private class ThrowingSetterType
        {
            public static Exception Exception = new Exception("This setter throws");
            public string Throws { get { return null; } set { throw Exception; } }
        }

        private class ClassWithArrayField
        {
            public int[] x { get; set; }
        }
    }
}
