﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Objects.DataClasses;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Web.UI.WebControls;
using System.Web.WebPages.Scope;
using Microsoft.TestCommon;
using Microsoft.Web.UnitTestUtil;
using Moq;

namespace System.Web.Mvc.Html.Test
{
    public class DefaultEditorTemplatesTest
    {
        // BooleanTemplate

        [Fact]
        public void BooleanTemplateTests()
        {
            // Boolean values

            Assert.Equal(
                "<input checked=\"checked\" class=\"check-box\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"checkbox\" value=\"true\" /><input name=\"FieldPrefix\" type=\"hidden\" value=\"false\" />",
                DefaultEditorTemplates.BooleanTemplate(MakeHtmlHelper<bool>(true)));

            Assert.Equal(
                "<input class=\"check-box\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"checkbox\" value=\"true\" /><input name=\"FieldPrefix\" type=\"hidden\" value=\"false\" />",
                DefaultEditorTemplates.BooleanTemplate(MakeHtmlHelper<bool>(false)));

            Assert.Equal(
                "<input class=\"check-box\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"checkbox\" value=\"true\" /><input name=\"FieldPrefix\" type=\"hidden\" value=\"false\" />",
                DefaultEditorTemplates.BooleanTemplate(MakeHtmlHelper<bool>(null)));

            // Nullable<Boolean> values

            Assert.Equal(
                "<select class=\"list-box tri-state\" id=\"FieldPrefix\" name=\"FieldPrefix\"><option value=\"\">Not Set</option>" + Environment.NewLine
              + "<option selected=\"selected\" value=\"true\">True</option>" + Environment.NewLine
              + "<option value=\"false\">False</option>" + Environment.NewLine
              + "</select>",
                DefaultEditorTemplates.BooleanTemplate(MakeHtmlHelper<Nullable<bool>>(true)));

            Assert.Equal(
                "<select class=\"list-box tri-state\" id=\"FieldPrefix\" name=\"FieldPrefix\"><option value=\"\">Not Set</option>" + Environment.NewLine
              + "<option value=\"true\">True</option>" + Environment.NewLine
              + "<option selected=\"selected\" value=\"false\">False</option>" + Environment.NewLine
              + "</select>",
                DefaultEditorTemplates.BooleanTemplate(MakeHtmlHelper<Nullable<bool>>(false)));

            Assert.Equal(
                "<select class=\"list-box tri-state\" id=\"FieldPrefix\" name=\"FieldPrefix\"><option selected=\"selected\" value=\"\">Not Set</option>" + Environment.NewLine
              + "<option value=\"true\">True</option>" + Environment.NewLine
              + "<option value=\"false\">False</option>" + Environment.NewLine
              + "</select>",
                DefaultEditorTemplates.BooleanTemplate(MakeHtmlHelper<Nullable<bool>>(null)));
        }

        // CollectionTemplate

        private static string CollectionSpyCallback(HtmlHelper html, ModelMetadata metadata, string htmlFieldName, string templateName, DataBoundControlMode mode, object additionalViewData)
        {
            return String.Format(CultureInfo.InvariantCulture,
                                 Environment.NewLine + "Model = {0}, ModelType = {1}, PropertyName = {2}, HtmlFieldName = {3}, TemplateName = {4}, Mode = {5}, TemplateInfo.HtmlFieldPrefix = {6}, AdditionalViewData = {7}",
                                 metadata.Model ?? "(null)",
                                 metadata.ModelType == null ? "(null)" : metadata.ModelType.FullName,
                                 metadata.PropertyName ?? "(null)",
                                 htmlFieldName == String.Empty ? "(empty)" : htmlFieldName ?? "(null)",
                                 templateName ?? "(null)",
                                 mode,
                                 html.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix,
                                 AnonymousObject.Inspect(additionalViewData));
        }

        [Fact]
        public void CollectionTemplateWithNullModel()
        {
            // Arrange
            HtmlHelper html = MakeHtmlHelper<object>(null);

            // Act
            string result = DefaultEditorTemplates.CollectionTemplate(html, CollectionSpyCallback);

            // Assert
            Assert.Equal(String.Empty, result);
        }

        [Fact]
        public void CollectionTemplateNonEnumerableModelThrows()
        {
            // Arrange
            HtmlHelper html = MakeHtmlHelper<object>(new object());

            // Act & Assert
            Assert.Throws<InvalidOperationException>(
                () => DefaultEditorTemplates.CollectionTemplate(html, CollectionSpyCallback),
                "The Collection template was used with an object of type 'System.Object', which does not implement System.IEnumerable."
                );
        }

        [Fact]
        public void CollectionTemplateWithSingleItemCollectionWithoutPrefix()
        {
            // Arrange
            HtmlHelper html = MakeHtmlHelper<List<string>>(new List<string> { "foo" });
            html.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = null;

            // Act
            string result = DefaultEditorTemplates.CollectionTemplate(html, CollectionSpyCallback);

            // Assert
            Assert.Equal(
                Environment.NewLine
              + "Model = foo, ModelType = System.String, PropertyName = (null), HtmlFieldName = [0], TemplateName = (null), Mode = Edit, TemplateInfo.HtmlFieldPrefix = , AdditionalViewData = (null)",
                result);
        }

        [Fact]
        public void CollectionTemplateWithSingleItemCollectionWithPrefix()
        {
            // Arrange
            HtmlHelper html = MakeHtmlHelper<List<string>>(new List<string> { "foo" });
            html.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "ModelProperty";

            // Act
            string result = DefaultEditorTemplates.CollectionTemplate(html, CollectionSpyCallback);

            // Assert
            Assert.Equal(
                Environment.NewLine
              + "Model = foo, ModelType = System.String, PropertyName = (null), HtmlFieldName = ModelProperty[0], TemplateName = (null), Mode = Edit, TemplateInfo.HtmlFieldPrefix = , AdditionalViewData = (null)",
                result);
        }

        [Fact]
        public void CollectionTemplateWithMultiItemCollection()
        {
            // Arrange
            HtmlHelper html = MakeHtmlHelper<List<string>>(new List<string> { "foo", "bar", "baz" });
            html.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = null;

            // Act
            string result = DefaultEditorTemplates.CollectionTemplate(html, CollectionSpyCallback);

            // Assert
            Assert.Equal(
                Environment.NewLine
              + "Model = foo, ModelType = System.String, PropertyName = (null), HtmlFieldName = [0], TemplateName = (null), Mode = Edit, TemplateInfo.HtmlFieldPrefix = , AdditionalViewData = (null)" + Environment.NewLine
              + "Model = bar, ModelType = System.String, PropertyName = (null), HtmlFieldName = [1], TemplateName = (null), Mode = Edit, TemplateInfo.HtmlFieldPrefix = , AdditionalViewData = (null)" + Environment.NewLine
              + "Model = baz, ModelType = System.String, PropertyName = (null), HtmlFieldName = [2], TemplateName = (null), Mode = Edit, TemplateInfo.HtmlFieldPrefix = , AdditionalViewData = (null)",
                result);
        }

        [Fact]
        public void CollectionTemplateNullITemInWeaklyTypedCollectionUsesModelTypeOfString()
        {
            // Arrange
            HtmlHelper html = MakeHtmlHelper<ArrayList>(new ArrayList { null });
            html.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = null;

            // Act
            string result = DefaultEditorTemplates.CollectionTemplate(html, CollectionSpyCallback);

            // Assert
            Assert.Equal(
                Environment.NewLine
              + "Model = (null), ModelType = System.String, PropertyName = (null), HtmlFieldName = [0], TemplateName = (null), Mode = Edit, TemplateInfo.HtmlFieldPrefix = , AdditionalViewData = (null)",
                result);
        }

        [Fact]
        public void CollectionTemplateNullItemInStronglyTypedCollectionUsesModelTypeFromIEnumerable()
        {
            // Arrange
            HtmlHelper html = MakeHtmlHelper<List<IHttpHandler>>(new List<IHttpHandler> { null });
            html.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = null;

            // Act
            string result = DefaultEditorTemplates.CollectionTemplate(html, CollectionSpyCallback);

            // Assert
            Assert.Equal(
                Environment.NewLine
              + "Model = (null), ModelType = System.Web.IHttpHandler, PropertyName = (null), HtmlFieldName = [0], TemplateName = (null), Mode = Edit, TemplateInfo.HtmlFieldPrefix = , AdditionalViewData = (null)",
                result);
        }

        [Fact]
        public void CollectionTemplateUsesRealObjectTypes()
        {
            // Arrange
            HtmlHelper html = MakeHtmlHelper<List<object>>(new List<object> { 1, 2.3, "Hello World" });
            html.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = null;

            // Act
            string result = DefaultEditorTemplates.CollectionTemplate(html, CollectionSpyCallback);

            // Assert
            Assert.Equal(
                Environment.NewLine
              + "Model = 1, ModelType = System.Int32, PropertyName = (null), HtmlFieldName = [0], TemplateName = (null), Mode = Edit, TemplateInfo.HtmlFieldPrefix = , AdditionalViewData = (null)" + Environment.NewLine
              + "Model = 2.3, ModelType = System.Double, PropertyName = (null), HtmlFieldName = [1], TemplateName = (null), Mode = Edit, TemplateInfo.HtmlFieldPrefix = , AdditionalViewData = (null)" + Environment.NewLine
              + "Model = Hello World, ModelType = System.String, PropertyName = (null), HtmlFieldName = [2], TemplateName = (null), Mode = Edit, TemplateInfo.HtmlFieldPrefix = , AdditionalViewData = (null)",
                result);
        }

        [Fact]
        public void CollectionTemplateNullItemInCollectionOfNullableValueTypesDoesNotDiscardNullable()
        {
            // Arrange
            HtmlHelper html = MakeHtmlHelper<List<int?>>(new List<int?> { 1, null, 2 });
            html.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = null;

            // Act
            string result = DefaultEditorTemplates.CollectionTemplate(html, CollectionSpyCallback);

            // Assert
            Assert.Equal(
                Environment.NewLine
              + "Model = 1, ModelType = System.Nullable`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], PropertyName = (null), HtmlFieldName = [0], TemplateName = (null), Mode = Edit, TemplateInfo.HtmlFieldPrefix = , AdditionalViewData = (null)" + Environment.NewLine
              + "Model = (null), ModelType = System.Nullable`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], PropertyName = (null), HtmlFieldName = [1], TemplateName = (null), Mode = Edit, TemplateInfo.HtmlFieldPrefix = , AdditionalViewData = (null)" + Environment.NewLine
              + "Model = 2, ModelType = System.Nullable`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], PropertyName = (null), HtmlFieldName = [2], TemplateName = (null), Mode = Edit, TemplateInfo.HtmlFieldPrefix = , AdditionalViewData = (null)",
                result);
        }

        // DecimalTemplate

        [Fact]
        public void DecimalTemplateTests()
        {
            Assert.Equal(
                String.Format(
                    CultureInfo.CurrentCulture,
                    "<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"text\" value=\"{0:0.00}\" />",
                    12.35M),
                DefaultEditorTemplates.DecimalTemplate(MakeHtmlHelper<decimal>(12.3456M)));

            Assert.Equal(
                "<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"text\" value=\"Formatted Value\" />",
                DefaultEditorTemplates.DecimalTemplate(MakeHtmlHelper<decimal>(12.3456M, "Formatted Value")));

            Assert.Equal(
                "<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"text\" value=\"&lt;script>alert(&#39;XSS!&#39;)&lt;/script>\" />",
                DefaultEditorTemplates.DecimalTemplate(MakeHtmlHelper<decimal>(12.3456M, "<script>alert('XSS!')</script>")));
        }

        // HiddenInputTemplate

        [Fact]
        public void HiddenInputTemplateTests()
        {
            Assert.Equal(
                "Hidden Value<input id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"hidden\" value=\"Hidden Value\" />",
                DefaultEditorTemplates.HiddenInputTemplate(MakeHtmlHelper<string>("Hidden Value")));

            Assert.Equal(
                "&lt;script&gt;alert(&#39;XSS!&#39;)&lt;/script&gt;<input id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"hidden\" value=\"&lt;script>alert(&#39;XSS!&#39;)&lt;/script>\" />",
                DefaultEditorTemplates.HiddenInputTemplate(MakeHtmlHelper<string>("<script>alert('XSS!')</script>")));

            var helperWithInvisibleHtml = MakeHtmlHelper<string>("<script>alert('XSS!')</script>", "<b>Encode me!</b>");
            helperWithInvisibleHtml.ViewData.ModelMetadata.HideSurroundingHtml = true;
            Assert.Equal(
                "<input id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"hidden\" value=\"&lt;script>alert(&#39;XSS!&#39;)&lt;/script>\" />",
                DefaultEditorTemplates.HiddenInputTemplate(helperWithInvisibleHtml));

            byte[] byteValues = { 1, 2, 3, 4, 5 };

            Assert.Equal(
                "&quot;AQIDBAU=&quot;<input id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"hidden\" value=\"AQIDBAU=\" />",
                DefaultEditorTemplates.HiddenInputTemplate(MakeHtmlHelper<Binary>(new Binary(byteValues))));

            Assert.Equal(
                "System.Byte[]<input id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"hidden\" value=\"AQIDBAU=\" />",
                DefaultEditorTemplates.HiddenInputTemplate(MakeHtmlHelper<byte[]>(byteValues)));
        }

        // MultilineText

        [Fact]
        public void MultilineTextTemplateTests()
        {
            Assert.Equal(
                "<textarea class=\"text-box multi-line\" id=\"FieldPrefix\" name=\"FieldPrefix\">" + Environment.NewLine
              + "Multiple" + Environment.NewLine
              + "Line" + Environment.NewLine
              + "Value!</textarea>",
                DefaultEditorTemplates.MultilineTextTemplate(MakeHtmlHelper<string>("", "Multiple" + Environment.NewLine + "Line" + Environment.NewLine + "Value!")));

            Assert.Equal(
                "<textarea class=\"text-box multi-line\" id=\"FieldPrefix\" name=\"FieldPrefix\">" + Environment.NewLine
              + "&lt;script&gt;alert(&#39;XSS!&#39;)&lt;/script&gt;</textarea>",
                DefaultEditorTemplates.MultilineTextTemplate(MakeHtmlHelper<string>("", "<script>alert('XSS!')</script>")));
        }

        // ObjectTemplate

        private static string SpyCallback(HtmlHelper html, ModelMetadata metadata, string htmlFieldName, string templateName, DataBoundControlMode mode, object additionalViewData)
        {
            return String.Format("Model = {0}, ModelType = {1}, PropertyName = {2}, HtmlFieldName = {3}, TemplateName = {4}, Mode = {5}, AdditionalViewData = {6}",
                                 metadata.Model ?? "(null)",
                                 metadata.ModelType == null ? "(null)" : metadata.ModelType.FullName,
                                 metadata.PropertyName ?? "(null)",
                                 htmlFieldName == String.Empty ? "(empty)" : htmlFieldName ?? "(null)",
                                 templateName ?? "(null)",
                                 mode,
                                 AnonymousObject.Inspect(additionalViewData));
        }

        class ObjectTemplateModel
        {
            public ObjectTemplateModel()
            {
                ComplexInnerModel = new object();
            }

            public string Property1 { get; set; }
            public string Property2 { get; set; }
            public object ComplexInnerModel { get; set; }
        }

        [Fact]
        public void ObjectTemplateEditsSimplePropertiesOnObjectByDefault()
        {
            string expected =
                "<div class=\"editor-label\"><label for=\"FieldPrefix_Property1\">Property1</label></div>" + Environment.NewLine
              + "<div class=\"editor-field\">Model = p1, ModelType = System.String, PropertyName = Property1, HtmlFieldName = Property1, TemplateName = (null), Mode = Edit, AdditionalViewData = (null) </div>" + Environment.NewLine
              + "<div class=\"editor-label\"><label for=\"FieldPrefix_Property2\">Property2</label></div>" + Environment.NewLine
              + "<div class=\"editor-field\">Model = (null), ModelType = System.String, PropertyName = Property2, HtmlFieldName = Property2, TemplateName = (null), Mode = Edit, AdditionalViewData = (null) </div>" + Environment.NewLine;

            // Arrange
            ObjectTemplateModel model = new ObjectTemplateModel { Property1 = "p1", Property2 = null };
            HtmlHelper html = MakeHtmlHelper<ObjectTemplateModel>(model);

            // Act
            string result = DefaultEditorTemplates.ObjectTemplate(html, SpyCallback);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ObjectTemplateWithModelError()
        {
            string expected =
                "<div class=\"editor-label\"><label for=\"FieldPrefix_Property1\">Property1</label></div>" + Environment.NewLine
              + "<div class=\"editor-field\">Model = p1, ModelType = System.String, PropertyName = Property1, HtmlFieldName = Property1, TemplateName = (null), Mode = Edit, AdditionalViewData = (null) <span class=\"field-validation-error\">Error Message</span></div>" + Environment.NewLine
              + "<div class=\"editor-label\"><label for=\"FieldPrefix_Property2\">Property2</label></div>" + Environment.NewLine
              + "<div class=\"editor-field\">Model = (null), ModelType = System.String, PropertyName = Property2, HtmlFieldName = Property2, TemplateName = (null), Mode = Edit, AdditionalViewData = (null) </div>" + Environment.NewLine;

            // Arrange
            ObjectTemplateModel model = new ObjectTemplateModel { Property1 = "p1", Property2 = null };
            HtmlHelper html = MakeHtmlHelper<ObjectTemplateModel>(model);
            html.ViewData.ModelState.AddModelError("FieldPrefix.Property1", "Error Message");

            // Act
            string result = DefaultEditorTemplates.ObjectTemplate(html, SpyCallback);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ObjectTemplateWithDisplayNameMetadata()
        {
            string expected =
                "<div class=\"editor-field\">Model = (null), ModelType = System.String, PropertyName = Property1, HtmlFieldName = Property1, TemplateName = (null), Mode = Edit, AdditionalViewData = (null) </div>" + Environment.NewLine
              + "<div class=\"editor-label\"><label for=\"FieldPrefix_Property2\">Custom display name</label></div>" + Environment.NewLine
              + "<div class=\"editor-field\">Model = (null), ModelType = System.String, PropertyName = Property2, HtmlFieldName = Property2, TemplateName = (null), Mode = Edit, AdditionalViewData = (null) </div>" + Environment.NewLine;

            // Arrange
            ObjectTemplateModel model = new ObjectTemplateModel();
            HtmlHelper html = MakeHtmlHelper<ObjectTemplateModel>(model);
            Mock<ModelMetadataProvider> provider = new Mock<ModelMetadataProvider>();
            Func<object> accessor = () => model;
            Mock<ModelMetadata> metadata = new Mock<ModelMetadata>(provider.Object, null, accessor, typeof(ObjectTemplateModel), null);
            ModelMetadata prop1Metadata = new ModelMetadata(provider.Object, typeof(ObjectTemplateModel), null, typeof(string), "Property1") { DisplayName = String.Empty };
            ModelMetadata prop2Metadata = new ModelMetadata(provider.Object, typeof(ObjectTemplateModel), null, typeof(string), "Property2") { DisplayName = "Custom display name" };
            html.ViewData.ModelMetadata = metadata.Object;
            metadata.Setup(p => p.Properties).Returns(() => new[] { prop1Metadata, prop2Metadata });

            // Act
            string result = DefaultEditorTemplates.ObjectTemplate(html, SpyCallback);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ObjectTemplateWithShowForEditorMetadata()
        {
            string expected =
                "<div class=\"editor-label\"><label for=\"FieldPrefix_Property1\">Property1</label></div>" + Environment.NewLine
              + "<div class=\"editor-field\">Model = (null), ModelType = System.String, PropertyName = Property1, HtmlFieldName = Property1, TemplateName = (null), Mode = Edit, AdditionalViewData = (null) </div>" + Environment.NewLine;

            // Arrange
            ObjectTemplateModel model = new ObjectTemplateModel();
            HtmlHelper html = MakeHtmlHelper<ObjectTemplateModel>(model);
            Mock<ModelMetadataProvider> provider = new Mock<ModelMetadataProvider>();
            Func<object> accessor = () => model;
            Mock<ModelMetadata> metadata = new Mock<ModelMetadata>(provider.Object, null, accessor, typeof(ObjectTemplateModel), null);
            ModelMetadata prop1Metadata = new ModelMetadata(provider.Object, typeof(ObjectTemplateModel), null, typeof(string), "Property1") { ShowForEdit = true };
            ModelMetadata prop2Metadata = new ModelMetadata(provider.Object, typeof(ObjectTemplateModel), null, typeof(string), "Property2") { ShowForEdit = false };
            html.ViewData.ModelMetadata = metadata.Object;
            metadata.Setup(p => p.Properties).Returns(() => new[] { prop1Metadata, prop2Metadata });

            // Act
            string result = DefaultEditorTemplates.ObjectTemplate(html, SpyCallback);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ObjectTemplatePreventsRecursionOnModelValue()
        {
            string expected =
                "<div class=\"editor-label\"><label for=\"FieldPrefix_Property2\">Property2</label></div>" + Environment.NewLine
              + "<div class=\"editor-field\">Model = propValue2, ModelType = System.String, PropertyName = Property2, HtmlFieldName = Property2, TemplateName = (null), Mode = Edit, AdditionalViewData = (null) </div>" + Environment.NewLine;

            // Arrange
            ObjectTemplateModel model = new ObjectTemplateModel();
            HtmlHelper html = MakeHtmlHelper<ObjectTemplateModel>(model);
            Mock<ModelMetadataProvider> provider = new Mock<ModelMetadataProvider>();
            Func<object> accessor = () => model;
            Mock<ModelMetadata> metadata = new Mock<ModelMetadata>(provider.Object, null, accessor, typeof(ObjectTemplateModel), null);
            ModelMetadata prop1Metadata = new ModelMetadata(provider.Object, typeof(ObjectTemplateModel), () => "propValue1", typeof(string), "Property1");
            ModelMetadata prop2Metadata = new ModelMetadata(provider.Object, typeof(ObjectTemplateModel), () => "propValue2", typeof(string), "Property2");
            html.ViewData.ModelMetadata = metadata.Object;
            metadata.Setup(p => p.Properties).Returns(() => new[] { prop1Metadata, prop2Metadata });
            html.ViewData.TemplateInfo.VisitedObjects.Add("propValue1");

            // Act
            string result = DefaultEditorTemplates.ObjectTemplate(html, SpyCallback);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ObjectTemplatePreventsRecursionOnModelTypeForNullModelValues()
        {
            string expected =
                "<div class=\"editor-label\"><label for=\"FieldPrefix_Property2\">Property2</label></div>" + Environment.NewLine
              + "<div class=\"editor-field\">Model = propValue2, ModelType = System.String, PropertyName = Property2, HtmlFieldName = Property2, TemplateName = (null), Mode = Edit, AdditionalViewData = (null) </div>" + Environment.NewLine;

            // Arrange
            ObjectTemplateModel model = new ObjectTemplateModel();
            HtmlHelper html = MakeHtmlHelper<ObjectTemplateModel>(model);
            Mock<ModelMetadataProvider> provider = new Mock<ModelMetadataProvider>();
            Func<object> accessor = () => model;
            Mock<ModelMetadata> metadata = new Mock<ModelMetadata>(provider.Object, null, accessor, typeof(ObjectTemplateModel), null);
            ModelMetadata prop1Metadata = new ModelMetadata(provider.Object, typeof(ObjectTemplateModel), null, typeof(string), "Property1");
            ModelMetadata prop2Metadata = new ModelMetadata(provider.Object, typeof(ObjectTemplateModel), () => "propValue2", typeof(string), "Property2");
            html.ViewData.ModelMetadata = metadata.Object;
            metadata.Setup(p => p.Properties).Returns(() => new[] { prop1Metadata, prop2Metadata });
            html.ViewData.TemplateInfo.VisitedObjects.Add(typeof(string));

            // Act
            string result = DefaultEditorTemplates.ObjectTemplate(html, SpyCallback);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ObjectTemplateDisplaysNullDisplayTextWithNullModelAndTemplateDepthGreaterThanOne()
        {
            // Arrange
            HtmlHelper html = MakeHtmlHelper<ObjectTemplateModel>(null);
            ModelMetadata metadata = ModelMetadataProviders.Current.GetMetadataForType(null, typeof(ObjectTemplateModel));
            metadata.NullDisplayText = "Null Display Text";
            metadata.SimpleDisplayText = "Simple Display Text";
            html.ViewData.ModelMetadata = metadata;
            html.ViewData.TemplateInfo.VisitedObjects.Add("foo");
            html.ViewData.TemplateInfo.VisitedObjects.Add("bar");

            // Act
            string result = DefaultEditorTemplates.ObjectTemplate(html, SpyCallback);

            // Assert
            Assert.Equal(metadata.NullDisplayText, result);
        }

        [Fact]
        public void ObjectTemplateDisplaysSimpleDisplayTextWithNonNullModelTemplateDepthGreaterThanOne()
        {
            // Arrange
            ObjectTemplateModel model = new ObjectTemplateModel();
            HtmlHelper html = MakeHtmlHelper<ObjectTemplateModel>(model);
            ModelMetadata metadata = ModelMetadataProviders.Current.GetMetadataForType(() => model, typeof(ObjectTemplateModel));
            html.ViewData.ModelMetadata = metadata;
            metadata.NullDisplayText = "Null Display Text";
            metadata.SimpleDisplayText = "Simple Display Text";
            html.ViewData.TemplateInfo.VisitedObjects.Add("foo");
            html.ViewData.TemplateInfo.VisitedObjects.Add("bar");

            // Act
            string result = DefaultEditorTemplates.ObjectTemplate(html, SpyCallback);

            // Assert
            Assert.Equal(metadata.SimpleDisplayText, result);
        }

        // PasswordTemplate

        [Fact]
        public void PasswordTemplateTests()
        {
            Assert.Equal(
                "<input class=\"text-box single-line password\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"password\" value=\"Value\" />",
                DefaultEditorTemplates.PasswordTemplate(MakeHtmlHelper<string>("Value")));

            Assert.Equal(
                "<input class=\"text-box single-line password\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"password\" value=\"&lt;script>alert(&#39;XSS!&#39;)&lt;/script>\" />",
                DefaultEditorTemplates.PasswordTemplate(MakeHtmlHelper<string>("<script>alert('XSS!')</script>")));
        }

        [Fact]
        public void ObjectTemplateWithHiddenHtml()
        {
            string expected = "Model = propValue1, ModelType = System.String, PropertyName = Property1, HtmlFieldName = Property1, TemplateName = (null), Mode = Edit, AdditionalViewData = (null)";

            // Arrange
            ObjectTemplateModel model = new ObjectTemplateModel();
            HtmlHelper html = MakeHtmlHelper<ObjectTemplateModel>(model);
            Mock<ModelMetadataProvider> provider = new Mock<ModelMetadataProvider>();
            Func<object> accessor = () => model;
            Mock<ModelMetadata> metadata = new Mock<ModelMetadata>(provider.Object, null, accessor, typeof(ObjectTemplateModel), null);
            ModelMetadata prop1Metadata = new ModelMetadata(provider.Object, typeof(ObjectTemplateModel), () => "propValue1", typeof(string), "Property1") { HideSurroundingHtml = true };
            html.ViewData.ModelMetadata = metadata.Object;
            metadata.Setup(p => p.Properties).Returns(() => new[] { prop1Metadata });

            // Act
            string result = DefaultEditorTemplates.ObjectTemplate(html, SpyCallback);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ObjectTemplateAllPropertiesFromEntityObjectAreHidden()
        {
            // Arrange
            HtmlHelper html = MakeHtmlHelper<ObjectTemplateModel>(new MyEntityObject());

            // Act
            string result = DefaultEditorTemplates.ObjectTemplate(html, SpyCallback);

            // Assert
            Assert.Equal(String.Empty, result);
        }

        private class MyEntityObject : EntityObject
        {
        }

        // StringTemplate

        [Fact]
        public void StringTemplateTests()
        {
            Assert.Equal(
                "<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"text\" value=\"Value\" />",
                DefaultEditorTemplates.StringTemplate(MakeHtmlHelper<string>("Value")));

            Assert.Equal(
                "<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"text\" value=\"&lt;script>alert(&#39;XSS!&#39;)&lt;/script>\" />",
                DefaultEditorTemplates.StringTemplate(MakeHtmlHelper<string>("<script>alert('XSS!')</script>")));
        }

        // PhoneNumberInputTemplate

        [Fact]
        public void PhoneNumberInputTemplateTests()
        {
            Assert.Equal(
                "<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"tel\" value=\"Value\" />",
                DefaultEditorTemplates.PhoneNumberInputTemplate(MakeHtmlHelper<string>("Value")));

            Assert.Equal(
                "<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"tel\" value=\"&lt;script>alert(&#39;XSS!&#39;)&lt;/script>\" />",
                DefaultEditorTemplates.PhoneNumberInputTemplate(MakeHtmlHelper<string>("<script>alert('XSS!')</script>")));
        }

        // UrlInputTemplate

        [Fact]
        public void UrlInputTemplateTests()
        {
            Assert.Equal(
                "<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"url\" value=\"Value\" />",
                DefaultEditorTemplates.UrlInputTemplate(MakeHtmlHelper<string>("Value")));

            Assert.Equal(
                "<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"url\" value=\"&lt;script>alert(&#39;XSS!&#39;)&lt;/script>\" />",
                DefaultEditorTemplates.UrlInputTemplate(MakeHtmlHelper<string>("<script>alert('XSS!')</script>")));
        }

        // EmailAddressInputTemplate

        [Fact]
        public void EmailAddressTemplateTests()
        {
            Assert.Equal(
                "<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"email\" value=\"Value\" />",
                DefaultEditorTemplates.EmailAddressInputTemplate(MakeHtmlHelper<string>("Value")));

            Assert.Equal(
                "<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"email\" value=\"&lt;script>alert(&#39;XSS!&#39;)&lt;/script>\" />",
                DefaultEditorTemplates.EmailAddressInputTemplate(MakeHtmlHelper<string>("<script>alert('XSS!')</script>")));
        }

        [Fact]
        public void DateTimeInputTemplateTests()
        {
            var type = "datetime";
            Assert.Equal(
                GetExpectedInputTag(type, "Value"),
                DefaultEditorTemplates.DateTimeInputTemplate(MakeHtmlHelper<string>("Value")));

            Assert.Equal(
                GetExpectedInputTag(type, "&lt;script>alert(&#39;XSS!&#39;)&lt;/script>"),
                DefaultEditorTemplates.DateTimeInputTemplate(MakeHtmlHelper<string>("<script>alert('XSS!')</script>")));


            var epocInLocalTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();
            var helper = MakeHtmlHelper<DateTime>(epocInLocalTime);

            Assert.Equal(
                GetExpectedInputTag(type, epocInLocalTime.ToString(CultureInfo.CurrentCulture)),
                DefaultEditorTemplates.DateTimeInputTemplate(helper));

            helper.Html5DateRenderingMode = Html5DateRenderingMode.Rfc3339;

            Assert.Equal(
                GetExpectedInputTag(type, epocInLocalTime.ToString("yyyy-MM-ddTHH:mm:ss.fffK")),
                DefaultEditorTemplates.DateTimeInputTemplate(helper));
        }

        [Fact]
        public void DateTimeLocalInputTemplateTests()
        {
            var type = "datetime-local";
            Assert.Equal(
                GetExpectedInputTag(type, "Value"),
                DefaultEditorTemplates.DateTimeLocalInputTemplate(MakeHtmlHelper<string>("Value")));

            Assert.Equal(
                GetExpectedInputTag(type, "&lt;script>alert(&#39;XSS!&#39;)&lt;/script>"),
                DefaultEditorTemplates.DateTimeLocalInputTemplate(MakeHtmlHelper<string>("<script>alert('XSS!')</script>")));

            var epocInLocalTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();
            var helper = MakeHtmlHelper<DateTime>(epocInLocalTime);

            Assert.Equal(
                GetExpectedInputTag(type, epocInLocalTime.ToString(CultureInfo.CurrentCulture)),
                DefaultEditorTemplates.DateTimeLocalInputTemplate(helper));

            helper.Html5DateRenderingMode = Html5DateRenderingMode.Rfc3339;

            Assert.Equal(
                GetExpectedInputTag(type, epocInLocalTime.ToString("yyyy-MM-ddTHH:mm:ss.fff")),
                DefaultEditorTemplates.DateTimeLocalInputTemplate(helper));
        }

        [Fact]
        public void DateInputTemplateTests()
        {
            var type = "date";
            Assert.Equal(
                GetExpectedInputTag(type, "Value"),
                DefaultEditorTemplates.DateInputTemplate(MakeHtmlHelper<string>("Value")));

            Assert.Equal(
                GetExpectedInputTag(type, "&lt;script>alert(&#39;XSS!&#39;)&lt;/script>"),
                DefaultEditorTemplates.DateInputTemplate(MakeHtmlHelper<string>("<script>alert('XSS!')</script>")));

            var epocInLocalTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();
            var helper = MakeHtmlHelper<DateTime>(epocInLocalTime);

            Assert.Equal(
                GetExpectedInputTag(type, epocInLocalTime.ToString(CultureInfo.CurrentCulture)),
                DefaultEditorTemplates.DateInputTemplate(helper));

            helper.Html5DateRenderingMode = Html5DateRenderingMode.Rfc3339;

            Assert.Equal(
                GetExpectedInputTag(type, epocInLocalTime.ToString("yyyy-MM-dd")),
                DefaultEditorTemplates.DateInputTemplate(helper));
        }

        [Fact]
        public void TimeInputTemplateTests()
        {
            var type = "time";
            Assert.Equal(
                GetExpectedInputTag(type, "Value"),
                DefaultEditorTemplates.TimeInputTemplate(MakeHtmlHelper<string>("Value")));

            Assert.Equal(
                GetExpectedInputTag(type, "&lt;script>alert(&#39;XSS!&#39;)&lt;/script>"),
                DefaultEditorTemplates.TimeInputTemplate(MakeHtmlHelper<string>("<script>alert('XSS!')</script>")));

            var epocInLocalTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();
            var helper = MakeHtmlHelper<DateTime>(epocInLocalTime);

            Assert.Equal(
                GetExpectedInputTag(type, epocInLocalTime.ToString(CultureInfo.CurrentCulture)),
                DefaultEditorTemplates.TimeInputTemplate(helper));

            helper.Html5DateRenderingMode = Html5DateRenderingMode.Rfc3339;

            Assert.Equal(
                GetExpectedInputTag(type, epocInLocalTime.ToString("HH:mm:ss.fff")),
                DefaultEditorTemplates.TimeInputTemplate(helper));
        }

        // NumberInputTemplate

        [Fact]
        public void NumberInputTemplateTests()
        {
            Assert.Equal(
                "<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"number\" value=\"Value\" />",
                DefaultEditorTemplates.NumberInputTemplate(MakeHtmlHelper<string>("Value")));

            Assert.Equal(
                "<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"number\" value=\"&lt;script>alert(&#39;XSS!&#39;)&lt;/script>\" />",
                DefaultEditorTemplates.NumberInputTemplate(MakeHtmlHelper<string>("<script>alert('XSS!')</script>")));
        }

        // ColorInputTemplate

        [Fact]
        public void ColorInputTemplateTests()
        {
            Assert.Equal(
                "<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"color\" value=\"#33F4CC\" />",
                DefaultEditorTemplates.ColorInputTemplate(MakeHtmlHelper<string>("#33F4CC")));

            var color = Color.FromArgb(0x33, 0xf4, 0xcc);
            Assert.Equal(
                "<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"color\" value=\"#33F4CC\" />",
                DefaultEditorTemplates.ColorInputTemplate(MakeHtmlHelper<Color>(color)));

            Assert.Equal(
                "<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"color\" value=\"&lt;script>alert(&#39;XSS!&#39;)&lt;/script>\" />",
                DefaultEditorTemplates.ColorInputTemplate(MakeHtmlHelper<string>("<script>alert('XSS!')</script>")));
        }

        // Helpers

        static string GetExpectedInputTag(string type, string value)
        {
            return string.Format("<input class=\"text-box single-line\" id=\"FieldPrefix\" name=\"FieldPrefix\" type=\"{0}\" value=\"{1}\" />", type, value);
        }

        private HtmlHelper MakeHtmlHelper<TModel>(object model)
        {
            return MakeHtmlHelper<TModel>(model, model);
        }

        private HtmlHelper MakeHtmlHelper<TModel>(object model, object formattedModelValue)
        {
            ViewDataDictionary viewData = new ViewDataDictionary(model);
            viewData.TemplateInfo.HtmlFieldPrefix = "FieldPrefix";
            viewData.TemplateInfo.FormattedModelValue = formattedModelValue;
            viewData.ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(() => model, typeof(TModel));

            Mock<HttpContextBase> mockHttpContext = new Mock<HttpContextBase>();
            mockHttpContext.Setup(o => o.Items).Returns(new Hashtable());

            ViewContext viewContext = new ViewContext(new ControllerContext(), new DummyView(), viewData, new TempDataDictionary(), new StringWriter())
            {
                HttpContext = mockHttpContext.Object
            };

            // A new helper instance, is executing within a new scope, so it needs to be reset.
            ScopeStorage.CurrentProvider.CurrentScope = new ScopeStorageDictionary();

            return new HtmlHelper(viewContext, new SimpleViewDataContainer(viewData));
        }

        private class DummyView : IView
        {
            public void Render(ViewContext viewContext, TextWriter writer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
