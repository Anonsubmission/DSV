﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Web.Mvc.Test;
using System.Web.Routing;
using Microsoft.TestCommon;
using Microsoft.Web.UnitTestUtil;

namespace System.Web.Mvc.Html.Test
{
    public class TextAreaExtensionsTest
    {
        private static readonly RouteValueDictionary _textAreaAttributesDictionary = new RouteValueDictionary(new { rows = "15", cols = "12" });
        private static readonly object _textAreaAttributesObjectDictionary = new { rows = "15", cols = "12" };
        private static readonly object _textAreaAttributesObjectUnderscoresDictionary = new { rows = "15", cols = "12", foo_bar = "baz" };

        private class TextAreaModel
        {
            public string foo { get; set; }
            public string bar { get; set; }
        }

        private static ViewDataDictionary<TextAreaModel> GetTextAreaViewData()
        {
            ViewDataDictionary<TextAreaModel> viewData = new ViewDataDictionary<TextAreaModel> { { "foo", "ViewDataFoo" } };
            viewData.Model = new TextAreaModel { foo = "ViewItemFoo", bar = "ViewItemBar" };
            return viewData;
        }

        private static ViewDataDictionary<TextAreaModel> GetTextAreaViewDataWithErrors()
        {
            ViewDataDictionary<TextAreaModel> viewData = new ViewDataDictionary<TextAreaModel> { { "foo", "ViewDataFoo" } };
            viewData.Model = new TextAreaModel { foo = "ViewItemFoo", bar = "ViewItemBar" };

            ModelState modelStateFoo = new ModelState();
            modelStateFoo.Errors.Add(new ModelError("foo error 1"));
            modelStateFoo.Errors.Add(new ModelError("foo error 2"));
            viewData.ModelState["foo"] = modelStateFoo;
            modelStateFoo.Value = HtmlHelperTest.GetValueProviderResult(new string[] { "AttemptedValueFoo" }, "AttemptedValueFoo");

            return viewData;
        }

        // TextArea

        [Fact]
        public void TextAreaParameterDictionaryMerging()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper();

            // Act
            MvcHtmlString html = helper.TextArea("foo", new { rows = "30" });

            // Assert
            Assert.Equal(
                "<textarea cols=\"20\" id=\"foo\" name=\"foo\" rows=\"30\">" + Environment.NewLine
              + "</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaParameterDictionaryMerging_Unobtrusive()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper();
            helper.ViewContext.ClientValidationEnabled = true;
            helper.ViewContext.UnobtrusiveJavaScriptEnabled = true;
            helper.ViewContext.FormContext = new FormContext();
            helper.ClientValidationRuleFactory = (name, metadata) => new[] { new ModelClientValidationRule { ValidationType = "type", ErrorMessage = "error" } };

            // Act
            MvcHtmlString html = helper.TextArea("foo", new { rows = "30" });

            // Assert
            Assert.Equal(
                "<textarea cols=\"20\" data-val=\"true\" data-val-type=\"error\" id=\"foo\" name=\"foo\" rows=\"30\">" + Environment.NewLine
              + "</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaParameterDictionaryMergingExplicitParameters()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper();

            // Act
            MvcHtmlString html = helper.TextArea("foo", "bar", 10, 25, new { rows = "30" });

            // Assert
            Assert.Equal(
                "<textarea cols=\"25\" id=\"foo\" name=\"foo\" rows=\"10\">" + Environment.NewLine
              + "bar</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaParameterDictionaryMergingExplicitParametersWithUnderscores()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper();

            // Act
            MvcHtmlString html = helper.TextArea("foo", "bar", 10, 25, new { rows = "30", foo_bar = "baz" });

            // Assert
            Assert.Equal(
                "<textarea cols=\"25\" foo-bar=\"baz\" id=\"foo\" name=\"foo\" rows=\"10\">" + Environment.NewLine
              + "bar</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaWithEmptyNameThrows()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper();

            // Act & Assert
            Assert.ThrowsArgumentNullOrEmpty(
                delegate { helper.TextArea(String.Empty); },
                "name");
        }

        [Fact]
        public void TextAreaWithOutOfRangeColsThrows()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper();

            // Act & Assert
            Assert.ThrowsArgumentOutOfRange(
                delegate { helper.TextArea("Foo", null /* value */, 0, -1, null /* htmlAttributes */); },
                "columns",
                "The value must be greater than or equal to zero.");
        }

        [Fact]
        public void TextAreaWithOutOfRangeRowsThrows()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper();

            // Act & Assert
            Assert.ThrowsArgumentOutOfRange(
                delegate { helper.TextArea("Foo", null /* value */, -1, 0, null /* htmlAttributes */); },
                "rows",
                "The value must be greater than or equal to zero.");
        }

        [Fact]
        public void TextAreaWithExplicitValue()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper();

            // Act
            MvcHtmlString html = helper.TextArea("foo", "bar");

            // Assert
            Assert.Equal(
                "<textarea cols=\"20\" id=\"foo\" name=\"foo\" rows=\"2\">" + Environment.NewLine
              + "bar</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaWithDefaultAttributes()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextArea("foo");

            // Assert
            Assert.Equal(
                "<textarea cols=\"20\" id=\"foo\" name=\"foo\" rows=\"2\">" + Environment.NewLine
              + "ViewDataFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaWithZeroRowsAndColumns()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextArea("foo", null, 0, 0, null);

            // Assert
            Assert.Equal(
                "<textarea id=\"foo\" name=\"foo\">" + Environment.NewLine
              + "ViewDataFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaWithDotReplacementForId()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextArea("foo.bar.baz");

            // Assert
            Assert.Equal(
                "<textarea cols=\"20\" id=\"foo_bar_baz\" name=\"foo.bar.baz\" rows=\"2\">" + Environment.NewLine
              + "</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaWithObjectAttributes()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextArea("foo", _textAreaAttributesObjectDictionary);

            // Assert
            Assert.Equal(
                "<textarea cols=\"12\" id=\"foo\" name=\"foo\" rows=\"15\">" + Environment.NewLine
              + "ViewDataFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaWithObjectAttributesWithUnderscores()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextArea("foo", _textAreaAttributesObjectUnderscoresDictionary);

            // Assert
            Assert.Equal(
                "<textarea cols=\"12\" foo-bar=\"baz\" id=\"foo\" name=\"foo\" rows=\"15\">" + Environment.NewLine
              + "ViewDataFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaWithDictionaryAttributes()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextArea("foo", _textAreaAttributesDictionary);

            // Assert
            Assert.Equal(
                "<textarea cols=\"12\" id=\"foo\" name=\"foo\" rows=\"15\">" + Environment.NewLine
              + "ViewDataFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaWithExplicitValueAndObjectAttributes()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextArea("foo", "Hello World", _textAreaAttributesObjectDictionary);

            // Assert
            Assert.Equal(
                "<textarea cols=\"12\" id=\"foo\" name=\"foo\" rows=\"15\">" + Environment.NewLine
              + "Hello World</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaWithExplicitValueAndObjectAttributesWithUnderscores()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextArea("foo", "Hello World", _textAreaAttributesObjectUnderscoresDictionary);

            // Assert
            Assert.Equal(
                "<textarea cols=\"12\" foo-bar=\"baz\" id=\"foo\" name=\"foo\" rows=\"15\">" + Environment.NewLine
              + "Hello World</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaWithExplicitValueAndDictionaryAttributes()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextArea("foo", "<Hello World>", _textAreaAttributesDictionary);

            // Assert
            Assert.Equal(
                "<textarea cols=\"12\" id=\"foo\" name=\"foo\" rows=\"15\">" + Environment.NewLine
              + "&lt;Hello World&gt;</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaWithNoValueAndObjectAttributes()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextArea("baz", _textAreaAttributesObjectDictionary);

            // Assert
            Assert.Equal(
                "<textarea cols=\"12\" id=\"baz\" name=\"baz\" rows=\"15\">" + Environment.NewLine
              + "</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaWithNullValue()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextArea("foo", null, null);

            // Assert
            Assert.Equal(
                "<textarea cols=\"20\" id=\"foo\" name=\"foo\" rows=\"2\">" + Environment.NewLine
              + "ViewDataFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaWithViewDataErrors()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper(GetTextAreaViewDataWithErrors());

            // Act
            MvcHtmlString html = helper.TextArea("foo", _textAreaAttributesObjectDictionary);

            // Assert
            Assert.Equal(
                "<textarea class=\"input-validation-error\" cols=\"12\" id=\"foo\" name=\"foo\" rows=\"15\">" + Environment.NewLine
              + "AttemptedValueFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaWithViewDataErrorsAndCustomClass()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper(GetTextAreaViewDataWithErrors());

            // Act
            MvcHtmlString html = helper.TextArea("foo", new { @class = "foo-class" });

            // Assert
            Assert.Equal(
                "<textarea class=\"input-validation-error foo-class\" cols=\"20\" id=\"foo\" name=\"foo\" rows=\"2\">" + Environment.NewLine
              + "AttemptedValueFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaWithPrefix()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper();
            helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";

            // Act
            MvcHtmlString html = helper.TextArea("foo", "bar");

            // Assert
            Assert.Equal(
                "<textarea cols=\"20\" id=\"MyPrefix_foo\" name=\"MyPrefix.foo\" rows=\"2\">" + Environment.NewLine
              + "bar</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaWithPrefixAndEmptyName()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper();
            helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";

            // Act
            MvcHtmlString html = helper.TextArea("", "bar");

            // Assert
            Assert.Equal(
                "<textarea cols=\"20\" id=\"MyPrefix\" name=\"MyPrefix\" rows=\"2\">" + Environment.NewLine
              + "bar</textarea>",
                html.ToHtmlString());
        }

        // TextAreaFor

        [Fact]
        public void TextAreaForWithNullExpression()
        {
            // Arrange
            HtmlHelper<TextAreaModel> helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act & Assert
            Assert.ThrowsArgumentNull(
                () => helper.TextAreaFor<TextAreaModel, object>(null),
                "expression"
                );
        }

        [Fact]
        public void TextAreaForWithOutOfRangeColsThrows()
        {
            // Arrange
            HtmlHelper<TextAreaModel> helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act & Assert
            Assert.ThrowsArgumentOutOfRange(
                () => helper.TextAreaFor(m => m.foo, 0, -1, null /* htmlAttributes */),
                "columns",
                "The value must be greater than or equal to zero."
                );
        }

        [Fact]
        public void TextAreaForWithOutOfRangeRowsThrows()
        {
            // Arrange
            HtmlHelper<TextAreaModel> helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act & Assert
            Assert.ThrowsArgumentOutOfRange(
                () => helper.TextAreaFor(m => m.foo, -1, 0, null /* htmlAttributes */),
                "rows",
                "The value must be greater than or equal to zero."
                );
        }

        [Fact]
        public void TextAreaForParameterDictionaryMerging()
        {
            // Arrange
            HtmlHelper<TextAreaModel> helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextAreaFor(m => m.foo, new { rows = "30" });

            // Assert
            Assert.Equal(
                "<textarea cols=\"20\" id=\"foo\" name=\"foo\" rows=\"30\">" + Environment.NewLine
              + "ViewItemFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaForParameterDictionaryMerging_Unobtrusive()
        {
            // Arrange
            HtmlHelper<TextAreaModel> helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());
            helper.ViewContext.ClientValidationEnabled = true;
            helper.ViewContext.UnobtrusiveJavaScriptEnabled = true;
            helper.ViewContext.FormContext = new FormContext();
            ModelMetadata modelMetadata = null;
            helper.ClientValidationRuleFactory = (name, metadata) => {
                modelMetadata = metadata;
                return new[] { new ModelClientValidationRule { ValidationType = "type", ErrorMessage = "error" } };
            };

            // Act
            MvcHtmlString html = helper.TextAreaFor(m => m.foo, new { rows = "30" });

            // Assert
            Assert.NotNull(modelMetadata);
            Assert.Equal("foo", modelMetadata.PropertyName);
            Assert.Equal(
                "<textarea cols=\"20\" data-val=\"true\" data-val-type=\"error\" id=\"foo\" name=\"foo\" rows=\"30\">" + Environment.NewLine
              + "ViewItemFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaForWithDefaultAttributes()
        {
            // Arrange
            HtmlHelper<TextAreaModel> helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextAreaFor(m => m.foo);

            // Assert
            Assert.Equal(
                "<textarea cols=\"20\" id=\"foo\" name=\"foo\" rows=\"2\">" + Environment.NewLine
              + "ViewItemFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaForWithZeroRowsAndColumns()
        {
            // Arrange
            HtmlHelper<TextAreaModel> helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextAreaFor(m => m.foo, 0, 0, null);

            // Assert
            Assert.Equal(
                "<textarea id=\"foo\" name=\"foo\">" + Environment.NewLine
              + "ViewItemFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaForWithObjectAttributes()
        {
            // Arrange
            HtmlHelper<TextAreaModel> helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextAreaFor(m => m.foo, _textAreaAttributesObjectDictionary);

            // Assert
            Assert.Equal(
                "<textarea cols=\"12\" id=\"foo\" name=\"foo\" rows=\"15\">" + Environment.NewLine
              + "ViewItemFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaForWithObjectAttributesWithUnderscores()
        {
            // Arrange
            HtmlHelper<TextAreaModel> helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextAreaFor(m => m.foo, _textAreaAttributesObjectUnderscoresDictionary);

            // Assert
            Assert.Equal(
                "<textarea cols=\"12\" foo-bar=\"baz\" id=\"foo\" name=\"foo\" rows=\"15\">" + Environment.NewLine
              + "ViewItemFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaForWithDictionaryAttributes()
        {
            // Arrange
            HtmlHelper<TextAreaModel> helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextAreaFor(m => m.foo, _textAreaAttributesDictionary);

            // Assert
            Assert.Equal(
                "<textarea cols=\"12\" id=\"foo\" name=\"foo\" rows=\"15\">" + Environment.NewLine
              + "ViewItemFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaForWithViewDataErrors()
        {
            // Arrange
            HtmlHelper<TextAreaModel> helper = MvcHelper.GetHtmlHelper(GetTextAreaViewDataWithErrors());

            // Act
            MvcHtmlString html = helper.TextAreaFor(m => m.foo, _textAreaAttributesObjectDictionary);

            // Assert
            Assert.Equal(
                "<textarea class=\"input-validation-error\" cols=\"12\" id=\"foo\" name=\"foo\" rows=\"15\">" + Environment.NewLine
              + "AttemptedValueFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaForWithViewDataErrorsAndCustomClass()
        {
            // Arrange
            HtmlHelper<TextAreaModel> helper = MvcHelper.GetHtmlHelper(GetTextAreaViewDataWithErrors());

            // Act
            MvcHtmlString html = helper.TextAreaFor(m => m.foo, new { @class = "foo-class" });

            // Assert
            Assert.Equal(
                "<textarea class=\"input-validation-error foo-class\" cols=\"20\" id=\"foo\" name=\"foo\" rows=\"2\">" + Environment.NewLine
              + "AttemptedValueFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaForWithPrefix()
        {
            // Arrange
            HtmlHelper<TextAreaModel> helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());
            helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";

            // Act
            MvcHtmlString html = helper.TextAreaFor(m => m.foo);

            // Assert
            Assert.Equal(
                "<textarea cols=\"20\" id=\"MyPrefix_foo\" name=\"MyPrefix.foo\" rows=\"2\">" + Environment.NewLine
              + "ViewItemFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaForWithPrefixAndEmptyName()
        {
            // Arrange
            HtmlHelper<TextAreaModel> helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());
            helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";

            // Act
            MvcHtmlString html = helper.TextAreaFor(m => m);

            // Assert
            Assert.Equal(
                "<textarea cols=\"20\" id=\"MyPrefix\" name=\"MyPrefix\" rows=\"2\">" + Environment.NewLine
              + "System.Web.Mvc.Html.Test.TextAreaExtensionsTest+TextAreaModel</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaForParameterDictionaryMergingWithObjectValues()
        {
            // Arrange
            HtmlHelper<TextAreaModel> helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextAreaFor(m => m.foo, 10, 25, new { rows = "30" });

            // Assert
            Assert.Equal(
                "<textarea cols=\"25\" id=\"foo\" name=\"foo\" rows=\"10\">" + Environment.NewLine
              + "ViewItemFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaForParameterDictionaryMergingWithObjectValuesWithUnderscores()
        {
            // Arrange
            HtmlHelper<TextAreaModel> helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextAreaFor(m => m.foo, 10, 25, new { rows = "30", foo_bar = "baz" });

            // Assert
            Assert.Equal(
                "<textarea cols=\"25\" foo-bar=\"baz\" id=\"foo\" name=\"foo\" rows=\"10\">" + Environment.NewLine
              + "ViewItemFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaForParameterDictionaryMergingWithDictionaryValues()
        {
            // Arrange
            HtmlHelper<TextAreaModel> helper = MvcHelper.GetHtmlHelper(GetTextAreaViewData());

            // Act
            MvcHtmlString html = helper.TextAreaFor(m => m.foo, 10, 25, new RouteValueDictionary(new { rows = "30" }));

            // Assert
            Assert.Equal(
                "<textarea cols=\"25\" id=\"foo\" name=\"foo\" rows=\"10\">" + Environment.NewLine
              + "ViewItemFoo</textarea>",
                html.ToHtmlString());
        }

        [Fact]
        public void TextAreaHelperDoesNotEncodeInnerHtmlPrefix()
        {
            // Arrange
            HtmlHelper helper = MvcHelper.GetHtmlHelper();
            ModelMetadata metadata = ModelMetadata.FromStringExpression("foo", helper.ViewData);
            metadata.Model = "<model>";

            // Act
            MvcHtmlString html = TextAreaExtensions.TextAreaHelper(helper, metadata, "testEncoding", rowsAndColumns: null,
                                                                   htmlAttributes: null, innerHtmlPrefix: "<prefix>");

            // Assert
            Assert.Equal("<textarea id=\"testEncoding\" name=\"testEncoding\"><prefix>&lt;model&gt;</textarea>", html.ToHtmlString());
        }
    }
}
