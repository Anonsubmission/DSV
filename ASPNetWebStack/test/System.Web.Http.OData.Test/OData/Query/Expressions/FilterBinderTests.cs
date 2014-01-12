// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web.Http.Dispatcher;
using System.Web.Http.OData.Builder;
using System.Xml.Linq;
using Microsoft.Data.Edm;
using Microsoft.Data.OData;
using Microsoft.Data.OData.Query;
using Microsoft.Data.OData.Query.SemanticAst;
using Microsoft.TestCommon;

namespace System.Web.Http.OData.Query.Expressions
{
    public class FilterBinderTests
    {
        private const string NotTesting = "";

        private static readonly Uri _serviceBaseUri = new Uri("http://server/service/");

        private static Dictionary<Type, IEdmModel> _modelCache = new Dictionary<Type, IEdmModel>();

        #region Inequalities
        [Theory]
        [InlineData(null, true, true)]
        [InlineData("", false, false)]
        [InlineData("Doritos", false, false)]
        public void EqualityOperatorWithNull(string productName, bool withNullPropagation, bool withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "ProductName eq null",
                "$it => ($it.ProductName == null)");

            RunFilters(filters,
                new Product { ProductName = productName },
                new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData(null, false, false)]
        [InlineData("", false, false)]
        [InlineData("Doritos", true, true)]
        public void EqualityOperator(string productName, bool withNullPropagation, bool withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "ProductName eq 'Doritos'",
                "$it => ($it.ProductName == \"Doritos\")");

            RunFilters(filters,
                new Product { ProductName = productName },
                new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData(null, true, true)]
        [InlineData("", true, true)]
        [InlineData("Doritos", false, false)]
        public void NotEqualOperator(string productName, bool withNullPropagation, bool withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "ProductName ne 'Doritos'",
                "$it => ($it.ProductName != \"Doritos\")");

            RunFilters(filters,
                new Product { ProductName = productName },
                new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData(null, false, false)]
        [InlineData(5.01, true, true)]
        [InlineData(4.99, false, false)]
        public void GreaterThanOperator(object unitPrice, bool withNullPropagation, bool withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "UnitPrice gt 5.00m",
                Error.Format("$it => ($it.UnitPrice > Convert({0:0.00}))", 5.0),
                Error.Format("$it => (($it.UnitPrice > Convert({0:0.00})) == True)", 5.0));

            RunFilters(filters,
                new Product { UnitPrice = ToNullable<decimal>(unitPrice) },
                new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData(null, false, false)]
        [InlineData(5.0, true, true)]
        [InlineData(4.99, false, false)]
        public void GreaterThanEqualOperator(object unitPrice, bool withNullPropagation, bool withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "UnitPrice ge 5.00m",
                Error.Format("$it => ($it.UnitPrice >= Convert({0:0.00}))", 5.0),
                Error.Format("$it => (($it.UnitPrice >= Convert({0:0.00})) == True)", 5.0));

            RunFilters(filters,
                new Product { UnitPrice = ToNullable<decimal>(unitPrice) },
                new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData(null, false, false)]
        [InlineData(4.99, true, true)]
        [InlineData(5.01, false, false)]
        public void LessThanOperator(object unitPrice, bool withNullPropagation, bool withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "UnitPrice lt 5.00m",
                Error.Format("$it => ($it.UnitPrice < Convert({0:0.00}))", 5.0),
                NotTesting);

            RunFilters(filters,
                new Product { UnitPrice = ToNullable<decimal>(unitPrice) },
                new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData(null, false, false)]
        [InlineData(5.0, true, true)]
        [InlineData(5.01, false, false)]
        public void LessThanOrEqualOperator(object unitPrice, bool withNullPropagation, bool withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "UnitPrice le 5.00m",
                Error.Format("$it => ($it.UnitPrice <= Convert({0:0.00}))", 5.0),
                NotTesting);

            RunFilters(filters,
               new Product { UnitPrice = ToNullable<decimal>(unitPrice) },
               new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Fact]
        public void NegativeNumbers()
        {
            VerifyQueryDeserialization(
                "UnitPrice le -5.00m",
                Error.Format("$it => ($it.UnitPrice <= Convert({0:0.00}))", -5.0),
                NotTesting);
        }

        [Theory]
        [InlineData("DateTimeOffsetProp eq DateTimeOffsetProp", "$it => ($it.DateTimeOffsetProp == $it.DateTimeOffsetProp)")]
        [InlineData("DateTimeOffsetProp ne DateTimeOffsetProp", "$it => ($it.DateTimeOffsetProp != $it.DateTimeOffsetProp)")]
        [InlineData("DateTimeOffsetProp ge DateTimeOffsetProp", "$it => ($it.DateTimeOffsetProp >= $it.DateTimeOffsetProp)")]
        [InlineData("DateTimeOffsetProp le DateTimeOffsetProp", "$it => ($it.DateTimeOffsetProp <= $it.DateTimeOffsetProp)")]
        public void DateTimeOffsetInEqualities(string clause, string expectedExpression)
        {
            // There's currently a bug here. For now, the test checks for the presence of the bug (as a reminder to fix
            // the test once the bug is fixed).
            // The following assert shows the behavior with the bug and should be removed once the bug is fixed.
            Assert.Throws<ODataException>(() => Bind("" + clause));

            // TODO: Enable once ODataUriParser handles DateTimeOffsets
            // The following call shows the behavior without the bug, and should be enabled once the bug is fixed.
            //VerifyQueryDeserialization<DataTypes>("" + clause, expectedExpression);
        }

        [Theory]
        [InlineData("DateTimeProp eq DateTimeProp", "$it => ($it.DateTimeProp == $it.DateTimeProp)")]
        [InlineData("DateTimeProp ne DateTimeProp", "$it => ($it.DateTimeProp != $it.DateTimeProp)")]
        [InlineData("DateTimeProp ge DateTimeProp", "$it => ($it.DateTimeProp >= $it.DateTimeProp)")]
        [InlineData("DateTimeProp le DateTimeProp", "$it => ($it.DateTimeProp <= $it.DateTimeProp)")]
        public void DateInEqualities(string clause, string expectedExpression)
        {
            VerifyQueryDeserialization<DataTypes>(
                "" + clause,
                expectedExpression);
        }

        #endregion

        #region Logical Operators

        [Fact]
        public void BooleanOperatorNullableTypes()
        {
            VerifyQueryDeserialization(
                "UnitPrice eq 5.00m or CategoryID eq 0",
                Error.Format("$it => (($it.UnitPrice == Convert(5.00)) OrElse ($it.CategoryID == 0))", 5.0, 0),
                NotTesting);
        }

        [Fact]
        public void BooleanComparisonOnNullableAndNonNullableType()
        {
            VerifyQueryDeserialization(
                "Discontinued eq true",
                "$it => ($it.Discontinued == Convert(True))",
                "$it => (($it.Discontinued == Convert(True)) == True)");
        }

        [Fact]
        public void BooleanComparisonOnNullableType()
        {
            VerifyQueryDeserialization(
                "Discontinued eq Discontinued",
                "$it => ($it.Discontinued == $it.Discontinued)",
                "$it => (($it.Discontinued == $it.Discontinued) == True)");
        }

        [Theory]
        [InlineData(null, null, false, false)]
        [InlineData(5.0, 0, true, true)]
        [InlineData(null, 1, false, false)]
        public void OrOperator(object unitPrice, object unitsInStock, bool withNullPropagation, bool withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "UnitPrice eq 5.00m or UnitsInStock eq 0",
                Error.Format("$it => (($it.UnitPrice == Convert({0:0.00})) OrElse (Convert($it.UnitsInStock) == Convert({1})))", 5.0, 0),
                NotTesting);

            RunFilters(filters,
                new Product { UnitPrice = ToNullable<decimal>(unitPrice), UnitsInStock = ToNullable<short>(unitsInStock) },
                new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData(null, null, false, false)]
        [InlineData(5.0, 10, true, true)]
        [InlineData(null, 1, false, false)]
        public void AndOperator(object unitPrice, object unitsInStock, bool withNullPropagation, bool withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "UnitPrice eq 5.00m and UnitsInStock eq 10.00m",
                Error.Format("$it => (($it.UnitPrice == Convert({0:0.00})) AndAlso (Convert($it.UnitsInStock) == Convert({1:0.00})))", 5.0, 10.0),
                NotTesting);

            RunFilters(filters,
                new Product { UnitPrice = ToNullable<decimal>(unitPrice), UnitsInStock = ToNullable<short>(unitsInStock) },
                new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData(null, false, true)] // This is an interesting cas for null propagation.
        [InlineData(5.0, false, false)]
        [InlineData(5.5, true, true)]
        public void Negation(object unitPrice, bool withNullPropagation, bool withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "not (UnitPrice eq 5.00m)",
                Error.Format("$it => Not(($it.UnitPrice == Convert({0:0.00})))", 5.0),
                NotTesting);

            RunFilters(filters,
                new Product { UnitPrice = ToNullable<decimal>(unitPrice) },
                new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData(null, true, true)] // This is an interesting cas for null propagation.
        [InlineData(true, false, false)]
        [InlineData(false, true, true)]
        public void BoolNegation(bool discontinued, bool withNullPropagation, bool withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "not Discontinued",
                "$it => Convert(Not($it.Discontinued))",
                "$it => (Not($it.Discontinued) == True)");

            RunFilters(filters,
                new Product { Discontinued = ToNullable<bool>(discontinued) },
                new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Fact]
        public void NestedNegation()
        {
            VerifyQueryDeserialization(
                "not (not(not    (Discontinued)))",
                "$it => Convert(Not(Not(Not($it.Discontinued))))",
                "$it => (Not(Not(Not($it.Discontinued))) == True)");
        }
        #endregion

        #region Arithmetic Operators
        [Theory]
        [InlineData(null, false, false)]
        [InlineData(5.0, true, true)]
        [InlineData(15.01, false, false)]
        public void Subtraction(object unitPrice, bool withNullPropagation, bool withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "UnitPrice sub 1.00m lt 5.00m",
                Error.Format("$it => (($it.UnitPrice - Convert({0:0.00})) < Convert({1:0.00}))", 1.0, 5.0),
                Error.Format("$it => ((($it.UnitPrice - Convert({0:0.00})) < Convert({1:0.00})) == True)", 1.0, 5.0));

            RunFilters(filters,
               new Product { UnitPrice = ToNullable<decimal>(unitPrice) },
               new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Fact]
        public void Addition()
        {
            VerifyQueryDeserialization(
                "UnitPrice add 1.00m lt 5.00m",
                Error.Format("$it => (($it.UnitPrice + Convert({0:0.00})) < Convert({1:0.00}))", 1.0, 5.0),
                NotTesting);
        }

        [Fact]
        public void Multiplication()
        {
            VerifyQueryDeserialization(
                "UnitPrice mul 1.00m lt 5.00m",
                Error.Format("$it => (($it.UnitPrice * Convert({0:0.00})) < Convert({1:0.00}))", 1.0, 5.0),
                NotTesting);
        }

        [Fact]
        public void Division()
        {
            VerifyQueryDeserialization(
                "UnitPrice div 1.00m lt 5.00m",
                Error.Format("$it => (($it.UnitPrice / Convert({0:0.00})) < Convert({1:0.00}))", 1.0, 5.0),
                NotTesting);
        }

        [Fact]
        public void Modulo()
        {
            VerifyQueryDeserialization(
                "UnitPrice mod 1.00m lt 5.00m",
                Error.Format("$it => (($it.UnitPrice % Convert({0:0.00})) < Convert({1:0.00}))", 1.0, 5.0),
                NotTesting);
        }
        #endregion

        # region NULL  handling
        [Theory]
        [InlineData("UnitsInStock eq UnitsOnOrder", null, null, false, true)]
        [InlineData("UnitsInStock ne UnitsOnOrder", null, null, false, false)]
        [InlineData("UnitsInStock gt UnitsOnOrder", null, null, false, false)]
        [InlineData("UnitsInStock ge UnitsOnOrder", null, null, false, false)]
        [InlineData("UnitsInStock lt UnitsOnOrder", null, null, false, false)]
        [InlineData("UnitsInStock le UnitsOnOrder", null, null, false, false)]
        [InlineData("(UnitsInStock add UnitsOnOrder) eq UnitsInStock", null, null, false, true)]
        [InlineData("(UnitsInStock sub UnitsOnOrder) eq UnitsInStock", null, null, false, true)]
        [InlineData("(UnitsInStock mul UnitsOnOrder) eq UnitsInStock", null, null, false, true)]
        [InlineData("(UnitsInStock div UnitsOnOrder) eq UnitsInStock", null, null, false, true)]
        [InlineData("(UnitsInStock mod UnitsOnOrder) eq UnitsInStock", null, null, false, true)]
        [InlineData("UnitsInStock eq UnitsOnOrder", 1, null, false, false)]
        [InlineData("UnitsInStock eq UnitsOnOrder", 1, 1, true, true)]
        public void NullHandling(string filter, object unitsInStock, object unitsOnOrder, bool withNullPropagation, bool withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization("" + filter);

            RunFilters(filters,
                new Product { UnitsInStock = ToNullable<short>(unitsInStock), UnitsOnOrder = ToNullable<short>(unitsOnOrder) },
                new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData("UnitsInStock eq null", null, true, true)] // NULL == constant NULL is true when null propagation is enabled
        [InlineData("UnitsInStock ne null", null, false, false)]  // NULL != constant NULL is false when null propagation is enabled
        public void NullHandling_LiteralNull(string filter, object unitsInStock, bool withNullPropagation, bool withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization("" + filter);

            RunFilters(filters,
                new Product { UnitsInStock = ToNullable<short>(unitsInStock) },
                new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }
        #endregion

        [Theory]
        [InlineData("StringProp gt 'Middle'", "Middle", false)]
        [InlineData("StringProp ge 'Middle'", "Middle", true)]
        [InlineData("StringProp lt 'Middle'", "Middle", false)]
        [InlineData("StringProp le 'Middle'", "Middle", true)]
        [InlineData("StringProp ge StringProp", "", true)]
        [InlineData("StringProp gt null", "", true)]
        [InlineData("null gt StringProp", "", false)]
        [InlineData("'Middle' gt StringProp", "Middle", false)]
        [InlineData("'a' lt 'b'", "", true)]
        public void StringComparisons_Work(string filter, string value, bool expectedResult)
        {
            var filters = VerifyQueryDeserialization<DataTypes>(filter);
            var result = RunFilter(filters.WithoutNullPropagation, new DataTypes { StringProp = value });
            
            Assert.Equal(result, expectedResult);
        }

        // Issue: 477
        [Theory]
        [InlineData("indexof('hello', StringProp) gt UIntProp")]
        [InlineData("indexof('hello', StringProp) gt ULongProp")]
        [InlineData("indexof('hello', StringProp) gt UShortProp")]
        [InlineData("indexof('hello', StringProp) gt NullableUShortProp")]
        [InlineData("indexof('hello', StringProp) gt NullableUIntProp")]
        [InlineData("indexof('hello', StringProp) gt NullableULongProp")]
        public void ComparisonsInvolvingCastsAndNullableValues(string filter)
        {
            var filters = VerifyQueryDeserialization<DataTypes>(filter);

            RunFilters(filters,
              new DataTypes(),
              new { WithNullPropagation = false, WithoutNullPropagation = typeof(ArgumentNullException) });
        }

        [Theory]
        [InlineData(null, null, true, true)]
        [InlineData("not doritos", 0, true, true)]
        [InlineData("Doritos", 1, false, false)]
        public void Grouping(string productName, object unitsInStock, bool withNullPropagation, bool withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "((ProductName ne 'Doritos') or (UnitPrice lt 5.00m))",
                Error.Format("$it => (($it.ProductName != \"Doritos\") OrElse ($it.UnitPrice < Convert({0:0.00})))", 5.0),
                NotTesting);

            RunFilters(filters,
                new Product { ProductName = productName, UnitsInStock = ToNullable<short>(unitsInStock) },
                new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Fact]
        public void MemberExpressions()
        {
            var filters = VerifyQueryDeserialization(
                "Category/CategoryName eq 'Snacks'",
                "$it => ($it.Category.CategoryName == \"Snacks\")",
                "$it => (IIF(($it.Category == null), null, $it.Category.CategoryName) == \"Snacks\")");

            RunFilters(filters,
                new Product { },
                new { WithNullPropagation = false, WithoutNullPropagation = typeof(NullReferenceException) });

            RunFilters(filters,
                new Product { Category = new Category { CategoryName = "Snacks" } },
                new { WithNullPropagation = true, WithoutNullPropagation = true });
        }

        [Fact]
        public void MemberExpressionsRecursive()
        {
            var filters = VerifyQueryDeserialization(
                "Category/Product/Category/CategoryName eq 'Snacks'",
                "$it => ($it.Category.Product.Category.CategoryName == \"Snacks\")",
                NotTesting);

            RunFilters(filters,
               new Product { },
               new { WithNullPropagation = false, WithoutNullPropagation = typeof(NullReferenceException) });
        }

        [Fact]
        public void ComplexPropertyNavigation()
        {
            var filters = VerifyQueryDeserialization(
                "SupplierAddress/City eq 'Redmond'",
                "$it => ($it.SupplierAddress.City == \"Redmond\")",
                "$it => (IIF(($it.SupplierAddress == null), null, $it.SupplierAddress.City) == \"Redmond\")");

            RunFilters(filters,
               new Product { },
               new { WithNullPropagation = false, WithoutNullPropagation = typeof(NullReferenceException) });

            RunFilters(filters,
               new Product { SupplierAddress = new Address { City = "Redmond" } },
               new { WithNullPropagation = true, WithoutNullPropagation = true });
        }

        #region Any/All

        [Fact]
        public void AnyOnNavigationEnumerableCollections()
        {
            var filters = VerifyQueryDeserialization(
               "Category/EnumerableProducts/any(P: P/ProductName eq 'Snacks')",
               "$it => $it.Category.EnumerableProducts.Any(P => (P.ProductName == \"Snacks\"))",
               NotTesting);

            RunFilters(filters,
                     new Product
                     {
                         Category = new Category
                         {
                             EnumerableProducts = new Product[] 
                        { 
                            new Product { ProductName = "Snacks" }, 
                            new Product { ProductName = "NonSnacks" } 
                        }
                         }
                     },
                     new { WithNullPropagation = true, WithoutNullPropagation = true });

            RunFilters(filters,
                new Product
                {
                    Category = new Category
                    {
                        EnumerableProducts = new Product[] 
                        { 
                            new Product { ProductName = "NonSnacks" } 
                        }
                    }
                },
                new { WithNullPropagation = false, WithoutNullPropagation = false });
        }

        [Fact]
        public void AnyOnNavigationQueryableCollections()
        {
            var filters = VerifyQueryDeserialization(
               "Category/QueryableProducts/any(P: P/ProductName eq 'Snacks')",
               "$it => $it.Category.QueryableProducts.Any(P => (P.ProductName == \"Snacks\"))",
               NotTesting);

            RunFilters(filters,
                    new Product
                    {
                        Category = new Category
                        {
                            QueryableProducts = new Product[] 
                        { 
                            new Product { ProductName = "Snacks" }, 
                            new Product { ProductName = "NonSnacks" } 
                        }.AsQueryable()
                        }
                    },
                new { WithNullPropagation = true, WithoutNullPropagation = true });

            RunFilters(filters,
                new Product
                {
                    Category = new Category
                    {
                        QueryableProducts = new Product[] 
                        { 
                            new Product { ProductName = "NonSnacks" } 
                        }.AsQueryable()
                    }
                },
            new { WithNullPropagation = false, WithoutNullPropagation = false });
        }

        [Fact]
        public void AnyOnNavigation_NullCollection()
        {
            var filters = VerifyQueryDeserialization(
               "Category/EnumerableProducts/any(P: P/ProductName eq 'Snacks')",
               "$it => $it.Category.EnumerableProducts.Any(P => (P.ProductName == \"Snacks\"))",
               NotTesting);

            RunFilters(filters,
                     new Product
                     {
                         Category = new Category
                         {
                         }
                     },
                     new { WithNullPropagation = false, WithoutNullPropagation = typeof(ArgumentNullException) });

            RunFilters(filters,
                new Product
                {
                    Category = new Category
                    {
                        EnumerableProducts = new Product[] 
                        { 
                            new Product { ProductName = "Snacks" } 
                        }
                    }
                },
                new { WithNullPropagation = true, WithoutNullPropagation = true });
        }

        [Fact]
        public void AllOnNavigation_NullCollection()
        {
            var filters = VerifyQueryDeserialization(
               "Category/EnumerableProducts/all(P: P/ProductName eq 'Snacks')",
               "$it => $it.Category.EnumerableProducts.All(P => (P.ProductName == \"Snacks\"))",
               NotTesting);

            RunFilters(filters,
                     new Product
                     {
                         Category = new Category
                         {
                         }
                     },
                     new { WithNullPropagation = false, WithoutNullPropagation = typeof(ArgumentNullException) });

            RunFilters(filters,
                new Product
                {
                    Category = new Category
                    {
                        EnumerableProducts = new Product[] 
                        { 
                            new Product { ProductName = "Snacks" } 
                        }
                    }
                },
                new { WithNullPropagation = true, WithoutNullPropagation = true });
        }

        [Fact]
        public void AnyOnNavigationEnumerableCollections_EmptyFilter()
        {
            VerifyQueryDeserialization(
               "Category/EnumerableProducts/any()",
               "$it => $it.Category.EnumerableProducts.Any()",
               NotTesting);
        }

        [Fact]
        public void AnyOnNavigationQueryableCollections_EmptyFilter()
        {
            VerifyQueryDeserialization(
               "Category/QueryableProducts/any()",
               "$it => $it.Category.QueryableProducts.Any()",
               NotTesting);
        }

        [Fact]
        public void AllOnNavigationEnumerableCollections()
        {
            VerifyQueryDeserialization(
               "Category/EnumerableProducts/all(P: P/ProductName eq 'Snacks')",
               "$it => $it.Category.EnumerableProducts.All(P => (P.ProductName == \"Snacks\"))",
               NotTesting);
        }

        [Fact]
        public void AllOnNavigationQueryableCollections()
        {
            VerifyQueryDeserialization(
               "Category/QueryableProducts/all(P: P/ProductName eq 'Snacks')",
               "$it => $it.Category.QueryableProducts.All(P => (P.ProductName == \"Snacks\"))",
               NotTesting);
        }

        [Fact]
        public void AnyInSequenceNotNested()
        {
            VerifyQueryDeserialization(
               "Category/QueryableProducts/any(P: P/ProductName eq 'Snacks') or Category/QueryableProducts/any(P2: P2/ProductName eq 'Snacks')",
               "$it => ($it.Category.QueryableProducts.Any(P => (P.ProductName == \"Snacks\")) OrElse $it.Category.QueryableProducts.Any(P2 => (P2.ProductName == \"Snacks\")))",
               NotTesting);
        }

        [Fact]
        public void AllInSequenceNotNested()
        {
            VerifyQueryDeserialization(
               "Category/QueryableProducts/all(P: P/ProductName eq 'Snacks') or Category/QueryableProducts/all(P2: P2/ProductName eq 'Snacks')",
               "$it => ($it.Category.QueryableProducts.All(P => (P.ProductName == \"Snacks\")) OrElse $it.Category.QueryableProducts.All(P2 => (P2.ProductName == \"Snacks\")))",
               NotTesting);
        }

        [Fact]
        public void AnyOnPrimitiveCollection()
        {
            var filters = VerifyQueryDeserialization(
               "AlternateIDs/any(id: id eq 42)",
               "$it => $it.AlternateIDs.Any(id => (id == 42))",
               NotTesting);

            RunFilters(
                filters,
                new Product { AlternateIDs = new[] { 1, 2, 42 } },
                new { WithNullPropagation = true, WithoutNullPropagation = true });

            RunFilters(
                filters,
                new Product { AlternateIDs = new[] { 1, 2 } },
                new { WithNullPropagation = false, WithoutNullPropagation = false });
        }

        [Fact]
        public void AllOnPrimitiveCollection()
        {
            VerifyQueryDeserialization(
               "AlternateIDs/all(id: id eq 42)",
               "$it => $it.AlternateIDs.All(id => (id == 42))",
               NotTesting);
        }

        [Fact]
        public void AnyOnComplexCollection()
        {
            var filters = VerifyQueryDeserialization(
               "AlternateAddresses/any(address: address/City eq 'Redmond')",
               "$it => $it.AlternateAddresses.Any(address => (address.City == \"Redmond\"))",
               NotTesting);

            RunFilters(
                filters,
                new Product { AlternateAddresses = new[] { new Address { City = "Redmond" } } },
                new { WithNullPropagation = true, WithoutNullPropagation = true });

            RunFilters(
                filters,
                new Product(),
                new { WithNullPropagation = false, WithoutNullPropagation = typeof(ArgumentNullException) });
        }

        [Fact]
        public void AllOnComplexCollection()
        {
            VerifyQueryDeserialization(
               "AlternateAddresses/all(address: address/City eq 'Redmond')",
               "$it => $it.AlternateAddresses.All(address => (address.City == \"Redmond\"))",
               NotTesting);
        }

        [Fact]
        public void RecursiveAllAny()
        {
            VerifyQueryDeserialization(
               "Category/QueryableProducts/all(P: P/Category/EnumerableProducts/any(PP: PP/ProductName eq 'Snacks'))",
               "$it => $it.Category.QueryableProducts.All(P => P.Category.EnumerableProducts.Any(PP => (PP.ProductName == \"Snacks\")))",
               NotTesting);
        }

        #endregion

        #region String Functions

        [Theory]
        [InlineData("Abcd", -1, "Abcd", true, typeof(ArgumentOutOfRangeException))]
        [InlineData("Abcd", 0, "Abcd", true, true)]
        [InlineData("Abcd", 1, "bcd", true, true)]
        [InlineData("Abcd", 3, "d", true, true)]
        [InlineData("Abcd", 4, "", true, true)]
        [InlineData("Abcd", 5, "", true, typeof(ArgumentOutOfRangeException))]
        public void StringSubstringStart(string productName, int startIndex, string compareString, bool withNullPropagation, object withoutNullPropagation)
        {
            string filter = String.Format("substring(ProductName, {0}) eq '{1}'", startIndex, compareString);
            var filters = VerifyQueryDeserialization(filter);

            RunFilters(filters,
              new Product { ProductName = productName },
              new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData("Abcd", -1, 4, "Abcd", true, typeof(ArgumentOutOfRangeException))]
        [InlineData("Abcd", -1, 3, "Abc", true, typeof(ArgumentOutOfRangeException))]
        [InlineData("Abcd", 0, 1, "A", true, true)]
        [InlineData("Abcd", 0, 4, "Abcd", true, true)]
        [InlineData("Abcd", 0, 3, "Abc", true, true)]
        [InlineData("Abcd", 0, 5, "Abcd", true, typeof(ArgumentOutOfRangeException))]
        [InlineData("Abcd", 1, 3, "bcd", true, true)]
        [InlineData("Abcd", 1, 5, "bcd", true, typeof(ArgumentOutOfRangeException))]
        [InlineData("Abcd", 2, 1, "c", true, true)]
        [InlineData("Abcd", 3, 1, "d", true, true)]
        [InlineData("Abcd", 4, 1, "", true, typeof(ArgumentOutOfRangeException))]
        [InlineData("Abcd", 0, -1, "", true, typeof(ArgumentOutOfRangeException))]
        [InlineData("Abcd", 5, -1, "", true, typeof(ArgumentOutOfRangeException))]
        public void StringSubstringStartAndLength(string productName, int startIndex, int length, string compareString, bool withNullPropagation, object withoutNullPropagation)
        {
            string filter = String.Format("substring(ProductName, {0}, {1}) eq '{2}'", startIndex, length, compareString);
            var filters = VerifyQueryDeserialization(filter);

            RunFilters(filters,
              new Product { ProductName = productName },
              new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData(null, false, typeof(NullReferenceException))]
        [InlineData("Abcd", true, true)]
        [InlineData("Abd", false, false)]
        public void StringSubstringOf(string productName, bool withNullPropagation, object withoutNullPropagation)
        {
            // In OData, the order of parameters is actually reversed in the resulting
            // String.Contains expression

            var filters = VerifyQueryDeserialization(
                "substringof('Abc', ProductName)",
                "$it => $it.ProductName.Contains(\"Abc\")",
                NotTesting);

            RunFilters(filters,
              new Product { ProductName = productName },
              new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });

            filters = VerifyQueryDeserialization(
                "substringof(ProductName, 'Abc')",
                "$it => \"Abc\".Contains($it.ProductName)",
                NotTesting);
        }

        [Theory]
        [InlineData(null, false, typeof(NullReferenceException))]
        [InlineData("Abcd", true, true)]
        [InlineData("Abd", false, false)]
        public void StringStartsWith(string productName, bool withNullPropagation, object withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "startswith(ProductName, 'Abc')",
                "$it => $it.ProductName.StartsWith(\"Abc\")",
                NotTesting);

            RunFilters(filters,
              new Product { ProductName = productName },
              new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData(null, false, typeof(NullReferenceException))]
        [InlineData("AAbc", true, true)]
        [InlineData("Abcd", false, false)]
        public void StringEndsWith(string productName, bool withNullPropagation, object withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "endswith(ProductName, 'Abc')",
                "$it => $it.ProductName.EndsWith(\"Abc\")",
                NotTesting);

            RunFilters(filters,
              new Product { ProductName = productName },
              new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData(null, false, typeof(NullReferenceException))]
        [InlineData("AAbc", true, true)]
        [InlineData("", false, false)]
        public void StringLength(string productName, bool withNullPropagation, object withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "length(ProductName) gt 0",
                "$it => ($it.ProductName.Length > 0)",
                "$it => ((IIF(($it.ProductName == null), null, Convert($it.ProductName.Length)) > Convert(0)) == True)");

            RunFilters(filters,
              new Product { ProductName = productName },
              new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData(null, false, typeof(NullReferenceException))]
        [InlineData("12345Abc", true, true)]
        [InlineData("1234Abc", false, false)]
        public void StringIndexOf(string productName, bool withNullPropagation, object withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "indexof(ProductName, 'Abc') eq 5",
                "$it => ($it.ProductName.IndexOf(\"Abc\") == 5)",
                NotTesting);

            RunFilters(filters,
              new Product { ProductName = productName },
              new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData(null, false, typeof(NullReferenceException))]
        [InlineData("123uctName", true, true)]
        [InlineData("1234Abc", false, false)]
        public void StringSubstring(string productName, bool withNullPropagation, object withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "substring(ProductName, 3) eq 'uctName'",
                "$it => ($it.ProductName.Substring(3) == \"uctName\")",
                NotTesting);

            RunFilters(filters,
              new Product { ProductName = productName },
              new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });

            VerifyQueryDeserialization(
                "substring(ProductName, 3, 4) eq 'uctN'",
                "$it => ($it.ProductName.Substring(3, 4) == \"uctN\")",
                NotTesting);
        }

        [Theory]
        [InlineData(null, false, typeof(NullReferenceException))]
        [InlineData("Tasty Treats", true, true)]
        [InlineData("Tasty Treatss", false, false)]
        public void StringToLower(string productName, bool withNullPropagation, object withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "tolower(ProductName) eq 'tasty treats'",
                "$it => ($it.ProductName.ToLower() == \"tasty treats\")",
                NotTesting);

            RunFilters(filters,
              new Product { ProductName = productName },
              new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData(null, false, typeof(NullReferenceException))]
        [InlineData("Tasty Treats", true, true)]
        [InlineData("Tasty Treatss", false, false)]
        public void StringToUpper(string productName, bool withNullPropagation, object withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "toupper(ProductName) eq 'TASTY TREATS'",
                "$it => ($it.ProductName.ToUpper() == \"TASTY TREATS\")",
                NotTesting);

            RunFilters(filters,
              new Product { ProductName = productName },
              new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData(null, false, typeof(NullReferenceException))]
        [InlineData("Tasty Treats", true, true)]
        [InlineData("Tasty Treatss", false, false)]
        public void StringTrim(string productName, bool withNullPropagation, object withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "trim(ProductName) eq 'Tasty Treats'",
                "$it => ($it.ProductName.Trim() == \"Tasty Treats\")",
                NotTesting);

            RunFilters(filters,
              new Product { ProductName = productName },
              new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Fact]
        public void StringConcat()
        {
            var filters = VerifyQueryDeserialization(
                "concat('Food', 'Bar') eq 'FoodBar'",
                "$it => (\"Food\".Concat(\"Bar\") == \"FoodBar\")",
                NotTesting);

            RunFilters(filters,
              new Product { },
              new { WithNullPropagation = true, WithoutNullPropagation = true });
        }

        [Fact]
        public void RecursiveMethodCall()
        {
            var filters = VerifyQueryDeserialization(
                "floor(floor(UnitPrice)) eq 123m",
                "$it => ($it.UnitPrice.Value.Floor().Floor() == 123)",
                NotTesting);

            RunFilters(filters,
              new Product { },
              new { WithNullPropagation = false, WithoutNullPropagation = typeof(InvalidOperationException) });
        }

        #endregion

        #region Date Functions
        [Fact]
        public void DateDay()
        {
            var filters = VerifyQueryDeserialization(
                "day(DiscontinuedDate) eq 8",
                "$it => ($it.DiscontinuedDate.Value.Day == 8)",
                NotTesting);

            RunFilters(filters,
               new Product { },
               new { WithNullPropagation = false, WithoutNullPropagation = typeof(InvalidOperationException) });

            RunFilters(filters,
               new Product { DiscontinuedDate = new DateTime(2000, 10, 8) },
               new { WithNullPropagation = true, WithoutNullPropagation = true });
        }

        public void DateDayNonNullable()
        {
            VerifyQueryDeserialization(
                "day(NonNullableDiscontinuedDate) eq 8",
                "$it => ($it.NonNullableDiscontinuedDate.Day == 8)");
        }

        [Fact]
        public void DateMonth()
        {
            VerifyQueryDeserialization(
                "month(DiscontinuedDate) eq 8",
                "$it => ($it.DiscontinuedDate.Value.Month == 8)",
                NotTesting);
        }

        [Fact]
        public void DateYear()
        {
            VerifyQueryDeserialization(
                "year(DiscontinuedDate) eq 1974",
                "$it => ($it.DiscontinuedDate.Value.Year == 1974)",
                NotTesting);
        }

        [Fact]
        public void DateHour()
        {
            VerifyQueryDeserialization("hour(DiscontinuedDate) eq 8",
                "$it => ($it.DiscontinuedDate.Value.Hour == 8)",
                NotTesting);
        }

        [Fact]
        public void DateMinute()
        {
            VerifyQueryDeserialization(
                "minute(DiscontinuedDate) eq 12",
                "$it => ($it.DiscontinuedDate.Value.Minute == 12)",
                NotTesting);
        }

        [Fact]
        public void DateSecond()
        {
            VerifyQueryDeserialization(
                "second(DiscontinuedDate) eq 33",
                "$it => ($it.DiscontinuedDate.Value.Second == 33)",
                NotTesting);
        }

        [Theory]
        [InlineData("year(DiscontinuedOffset) eq 100", "$it => ($it.DiscontinuedOffset.Year == 100)")]
        [InlineData("month(DiscontinuedOffset) eq 100", "$it => ($it.DiscontinuedOffset.Month == 100)")]
        [InlineData("day(DiscontinuedOffset) eq 100", "$it => ($it.DiscontinuedOffset.Day == 100)")]
        [InlineData("hour(DiscontinuedOffset) eq 100", "$it => ($it.DiscontinuedOffset.Hour == 100)")]
        [InlineData("minute(DiscontinuedOffset) eq 100", "$it => ($it.DiscontinuedOffset.Minute == 100)")]
        [InlineData("second(DiscontinuedOffset) eq 100", "$it => ($it.DiscontinuedOffset.Second == 100)")]
        public void DateTimeOffsetFunctions(string filter, string expression)
        {
            VerifyQueryDeserialization(filter, expression);
        }

        [Theory]
        [InlineData("years(DiscontinuedSince) eq 100", "$it => $it.DiscontinuedSince.Years == 100")]
        [InlineData("months(DiscontinuedSince) eq 100", "$it => $it.DiscontinuedSince.Months == 100")]
        [InlineData("days(DiscontinuedSince) eq 100", "$it => $it.DiscontinuedSince.Days == 100")]
        [InlineData("hours(DiscontinuedSince) eq 100", "$it => $it.DiscontinuedSince.Hours == 100")]
        [InlineData("minutes(DiscontinuedSince) eq 100", "$it => $it.DiscontinuedSince.Minutes == 100")]
        [InlineData("seconds(DiscontinuedSince) eq 100", "$it => $it.DiscontinuedSince.Seconds == 100")]
        public void TimespanFunctions(string filter, string expression)
        {
            // There's currently a bug here. For now, the test checks for the presence of the bug (as a reminder to fix
            // the test once the bug is fixed).
            // The following assert shows the behavior with the bug and should be removed once the bug is fixed.
            Assert.Throws<ODataException>(() => Bind(filter));

            // TODO: Timespans are not handled well in the uri parser
            // The following call shows the behavior without the bug, and should be enabled once the bug is fixed.
            //VerifyQueryDeserialization(filter, expression);
        }

        #endregion

        #region Math Functions
        [Theory]
        [InlineData(null, false, typeof(InvalidOperationException))]
        [InlineData(5.9, true, true)]
        [InlineData(5.4, false, false)]
        public void MathRound(object unitPrice, bool withNullPropagation, object withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "round(UnitPrice) gt 5.00m",
                Error.Format("$it => ($it.UnitPrice.Value.Round() > {0:0.00})", 5.0),
                NotTesting);

            RunFilters(filters,
               new Product { UnitPrice = ToNullable<decimal>(unitPrice) },
               new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData(null, false, typeof(InvalidOperationException))]
        [InlineData(5.4, true, true)]
        [InlineData(4.4, false, false)]
        public void MathFloor(object unitPrice, bool withNullPropagation, object withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "floor(UnitPrice) eq 5m",
                "$it => ($it.UnitPrice.Value.Floor() == 5)",
                NotTesting);

            RunFilters(filters,
               new Product { UnitPrice = ToNullable<decimal>(unitPrice) },
               new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData(null, false, typeof(InvalidOperationException))]
        [InlineData(4.1, true, true)]
        [InlineData(5.9, false, false)]
        public void MathCeiling(object unitPrice, bool withNullPropagation, object withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization(
                "ceiling(UnitPrice) eq 5m",
                "$it => ($it.UnitPrice.Value.Ceiling() == 5)",
                NotTesting);

            RunFilters(filters,
               new Product { UnitPrice = ToNullable<decimal>(unitPrice) },
               new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData("floor(FloatProp) eq floor(FloatProp)")]
        [InlineData("round(FloatProp) eq round(FloatProp)")]
        [InlineData("ceiling(FloatProp) eq ceiling(FloatProp)")]
        [InlineData("floor(DoubleProp) eq floor(DoubleProp)")]
        [InlineData("round(DoubleProp) eq round(DoubleProp)")]
        [InlineData("ceiling(DoubleProp) eq ceiling(DoubleProp)")]
        [InlineData("floor(DecimalProp) eq floor(DecimalProp)")]
        [InlineData("round(DecimalProp) eq round(DecimalProp)")]
        [InlineData("ceiling(DecimalProp) eq ceiling(DecimalProp)")]
        public void MathFunctions_VariousTypes(string filter)
        {
            var filters = VerifyQueryDeserialization<DataTypes>(filter);
            RunFilters(filters, new DataTypes(), new { WithNullPropagation = true, WithoutNullPropagation = true });
        }
        #endregion

        #region Data Types
        [Fact]
        public void GuidExpression()
        {
            VerifyQueryDeserialization<DataTypes>(
                "GuidProp eq guid'0EFDAECF-A9F0-42F3-A384-1295917AF95E'",
                "$it => ($it.GuidProp == 0efdaecf-a9f0-42f3-a384-1295917af95e)");

            // verify case insensitivity
            VerifyQueryDeserialization<DataTypes>(
                "GuidProp eq GuiD'0EFDAECF-A9F0-42F3-A384-1295917AF95E'",
                "$it => ($it.GuidProp == 0efdaecf-a9f0-42f3-a384-1295917af95e)");
        }

        [Theory]
        [InlineData("DateTimeProp eq datetime'2000-12-12T12:00:00'", "$it => ($it.DateTimeProp == {0})")]
        [InlineData("DateTimeProp lt datetime'2000-12-12T12:00:00'", "$it => ($it.DateTimeProp < {0})")]
        // TODO: [InlineData("DateTimeProp ge datetime'2000-12-12T12:00'", "$it => ($it.DateTimeProp >= {0})")] (uriparser fails on optional seconds)
        public void DateTimeExpression(string clause, string expectedExpression)
        {
            var dateTime = new DateTime(2000, 12, 12, 12, 0, 0);
            VerifyQueryDeserialization<DataTypes>(
                "" + clause,
                Error.Format(expectedExpression, dateTime));
        }

        [Theory]
        [InlineData("DateTimeOffsetProp eq datetimeoffset'2002-10-10T17:00:00Z'", "$it => ($it.DateTimeOffsetProp == {0})", 0)]
        [InlineData("DateTimeOffsetProp ge datetimeoffset'2002-10-10T17:00:00Z'", "$it => ($it.DateTimeOffsetProp >= {0})", 0)]
        [InlineData("DateTimeOffsetProp le datetimeoffset'2002-10-10T17:00:00-07:00'", "$it => ($it.DateTimeOffsetProp <= {0})", -7)]
        [InlineData("DateTimeOffsetProp eq datetimeoffset'2002-10-10T17:00:00-0600'", "$it => ($it.DateTimeOffsetProp == {0})", -6)]
        [InlineData("DateTimeOffsetProp lt datetimeoffset'2002-10-10T17:00:00-05'", "$it => ($it.DateTimeOffsetProp < {0})", -5)]
        [InlineData("DateTimeOffsetProp ne datetimeoffset'2002-10-10T17:00:00%2B09:30'", "$it => ($it.DateTimeOffsetProp != {0})", 9.5)]
        [InlineData("DateTimeOffsetProp gt datetimeoffset'2002-10-10T17:00:00%2B0545'", "$it => ($it.DateTimeOffsetProp > {0})", 5.75)]
        public void DateTimeOffsetExpression(string clause, string expectedExpression, double offsetHours)
        {
            var dateTimeOffset = new DateTimeOffset(2002, 10, 10, 17, 0, 0, TimeSpan.FromHours(offsetHours));

            // There's currently a bug here. For now, the test checks for the presence of the bug (as a reminder to fix
            // the test once the bug is fixed).
            // The following assert shows the behavior with the bug and should be removed once the bug is fixed.
            Assert.Throws<ODataException>(() => Bind("" + clause));

            // TODO: No DateTimeOffset parsing in ODataUriParser
            // The following call shows the behavior without the bug, and should be enabled once the bug is fixed.
            //VerifyQueryDeserialization<DataTypes>(
            //    "" + clause,
            //    Error.Format(expectedExpression, dateTimeOffset));
        }

        [Fact]
        public void IntegerLiteralSuffix()
        {
            // long L
            VerifyQueryDeserialization<DataTypes>(
                "LongProp lt 987654321L and LongProp gt 123456789l",
                "$it => (($it.LongProp < 987654321) AndAlso ($it.LongProp > 123456789))");

            VerifyQueryDeserialization<DataTypes>(
                "LongProp lt -987654321L and LongProp gt -123456789l",
                "$it => (($it.LongProp < -987654321) AndAlso ($it.LongProp > -123456789))");
        }

        [Fact]
        public void RealLiteralSuffixes()
        {
            // Float F
            VerifyQueryDeserialization<DataTypes>(
                "FloatProp lt 4321.56F and FloatProp gt 1234.56f",
                Error.Format("$it => (($it.FloatProp < {0:0.00}) AndAlso ($it.FloatProp > {1:0.00}))", 4321.56, 1234.56));

            // Decimal M
            VerifyQueryDeserialization<DataTypes>(
                "DecimalProp lt 4321.56M and DecimalProp gt 1234.56m",
                Error.Format("$it => (($it.DecimalProp < {0:0.00}) AndAlso ($it.DecimalProp > {1:0.00}))", 4321.56, 1234.56));
        }

        [Theory]
        [InlineData("'hello,world'", "hello,world")]
        [InlineData("'''hello,world'", "'hello,world")]
        [InlineData("'hello,world'''", "hello,world'")]
        [InlineData("'hello,''wor''ld'", "hello,'wor'ld")]
        [InlineData("'hello,''''''world'", "hello,'''world")]
        [InlineData("'\"hello,world\"'", "\"hello,world\"")]
        [InlineData("'\"hello,world'", "\"hello,world")]
        [InlineData("'hello,world\"'", "hello,world\"")]
        [InlineData("'hello,\"world'", "hello,\"world")]
        [InlineData("'México D.F.'", "México D.F.")]
        [InlineData("'æææøøøååå'", "æææøøøååå")]
        [InlineData("'いくつかのテキスト'", "いくつかのテキスト")]
        public void StringLiterals(string literal, string expected)
        {
            VerifyQueryDeserialization<Product>(
                "ProductName eq " + literal,
                String.Format("$it => ($it.ProductName == \"{0}\")", expected));
        }

        [Theory]
        [InlineData('$')]
        [InlineData('&')]
        [InlineData('+')]
        [InlineData(',')]
        [InlineData('/')]
        [InlineData(':')]
        [InlineData(';')]
        [InlineData('=')]
        [InlineData('?')]
        [InlineData('@')]
        [InlineData(' ')]
        [InlineData('<')]
        [InlineData('>')]
        [InlineData('#')]
        [InlineData('%')]
        [InlineData('{')]
        [InlineData('}')]
        [InlineData('|')]
        [InlineData('\\')]
        [InlineData('^')]
        [InlineData('~')]
        [InlineData('[')]
        [InlineData(']')]
        [InlineData('`')]
        public void SpecialCharactersInStringLiteral(char c)
        {
            var filters = VerifyQueryDeserialization<Product>(
                "ProductName eq '" + c + "'",
                String.Format("$it => ($it.ProductName == \"{0}\")", c));

            RunFilters(
                filters,
                new Product { ProductName = c.ToString() },
                new { WithNullPropagation = true, WithoutNullPropagation = true });
        }

        #endregion

        #region Casts

        [Fact]
        public void NSCast_OnEnumerableEntityCollection_GeneratesExpression_WithOfTypeOnEnumerable()
        {
            var filters = VerifyQueryDeserialization(
                "Category/EnumerableProducts/System.Web.Http.OData.Query.Expressions.DerivedProduct/any(p: p/ProductName eq 'ProductName')",
                "$it => $it.Category.EnumerableProducts.OfType().Any(p => (p.ProductName == \"ProductName\"))",
                NotTesting);

            Assert.NotNull(filters.WithoutNullPropagation);
        }

        [Fact]
        public void NSCast_OnQueryableEntityCollection_GeneratesExpression_WithOfTypeOnQueryable()
        {
            var filters = VerifyQueryDeserialization(
                "Category/QueryableProducts/System.Web.Http.OData.Query.Expressions.DerivedProduct/any(p: p/ProductName eq 'ProductName')",
                "$it => $it.Category.QueryableProducts.OfType().Any(p => (p.ProductName == \"ProductName\"))",
                NotTesting);
        }

        [Fact]
        public void NSCast_OnEntityCollection_CanAccessDerivedInstanceProperty()
        {
            var filters = VerifyQueryDeserialization(
                "Category/Products/System.Web.Http.OData.Query.Expressions.DerivedProduct/any(p: p/DerivedProductName eq 'DerivedProductName')");

            RunFilters(
                filters,
                new Product { Category = new Category { Products = new Product[] { new DerivedProduct { DerivedProductName = "DerivedProductName" } } } },
                new { WithNullPropagation = true, WithoutNullPropagation = true });

            RunFilters(
                filters,
                new Product { Category = new Category { Products = new Product[] { new DerivedProduct { DerivedProductName = "NotDerivedProductName" } } } },
                new { WithNullPropagation = false, WithoutNullPropagation = false });
        }

        [Fact]
        public void NSCast_OnSingleEntity_GeneratesExpression_WithAsOperator()
        {
            var filters = VerifyQueryDeserialization(
                "System.Web.Http.OData.Query.Expressions.Product/ProductName eq 'ProductName'",
                "$it => (($it As Product).ProductName == \"ProductName\")",
                NotTesting);
        }

        [Theory]
        [InlineData("System.Web.Http.OData.Query.Expressions.Product/ProductName eq 'ProductName'")]
        [InlineData("System.Web.Http.OData.Query.Expressions.DerivedProduct/DerivedProductName eq 'DerivedProductName'")]
        [InlineData("System.Web.Http.OData.Query.Expressions.DerivedProduct/Category/CategoryID eq 123")]
        [InlineData("System.Web.Http.OData.Query.Expressions.DerivedProduct/Category/System.Web.Http.OData.Query.Expressions.DerivedCategory/CategoryID eq 123")]
        public void Inheritance_WithDerivedInstance(string filter)
        {
            var filters = VerifyQueryDeserialization<DerivedProduct>(filter);

            RunFilters<DerivedProduct>(filters,
              new DerivedProduct { Category = new DerivedCategory { CategoryID = 123 }, ProductName = "ProductName", DerivedProductName = "DerivedProductName" },
              new { WithNullPropagation = true, WithoutNullPropagation = true });
        }

        [Theory]
        [InlineData("System.Web.Http.OData.Query.Expressions.DerivedProduct/DerivedProductName eq 'ProductName'")]
        [InlineData("System.Web.Http.OData.Query.Expressions.DerivedProduct/Category/CategoryID eq 123")]
        [InlineData("System.Web.Http.OData.Query.Expressions.DerivedProduct/Category/System.Web.Http.OData.Query.Expressions.DerivedCategory/CategoryID eq 123")]
        public void Inheritance_WithBaseInstance(string filter)
        {
            var filters = VerifyQueryDeserialization<Product>(filter);

            RunFilters<Product>(filters,
              new Product(),
              new { WithNullPropagation = false, WithoutNullPropagation = typeof(NullReferenceException) });
        }

        [Fact]
        public void CastToNonDerivedType_Throws()
        {
            Assert.Throws<ODataException>(
                () => VerifyQueryDeserialization<Product>("System.Web.Http.OData.Query.Expressions.DerivedCategory/CategoryID eq 123"),
                "Encountered invalid type cast. 'System.Web.Http.OData.Query.Expressions.DerivedCategory' is not assignable from 'System.Web.Http.OData.Query.Expressions.Product'.");
        }

        [Theory]
        [InlineData("Edm.Int32 eq 123", "Edm.Int32")]
        [InlineData("ProductName/Edm.String eq 123", "Edm.String")]
        public void CastToNonEntityType_Throws(string filter, string cast)
        {
            Assert.Throws<ODataException>(
                () => VerifyQueryDeserialization<Product>(filter),
                Error.Format("The child type '{0}' in a cast was not an entity type. Casts can only be performed on entity types.", cast));
        }

        [Theory]
        [InlineData("Edm.NonExistentType eq 123")]
        [InlineData("Category/Edm.NonExistentType eq 123")]
        [InlineData("Category/Products/Edm.NonExistentType eq 123")]
        public void CastToNonExistantType_Throws(string filter)
        {
            Assert.Throws<ODataException>(
                () => VerifyQueryDeserialization<Product>(filter),
                "The child type 'Edm.NonExistentType' in a cast was not an entity type. Casts can only be performed on entity types.");
        }

        #endregion

        [Theory]
        [InlineData("UShortProp eq 12", "$it => (Convert($it.UShortProp) == 12)")]
        [InlineData("ULongProp eq 12L", "$it => (Convert($it.ULongProp) == 12)")]
        [InlineData("UIntProp eq 12", "$it => (Convert($it.UIntProp) == Convert(12))")]
        [InlineData("CharProp eq 'a'", "$it => (Convert($it.CharProp.ToString()) == \"a\")")]
        [InlineData("CharArrayProp eq 'a'", "$it => (new String($it.CharArrayProp) == \"a\")")]
        [InlineData("BinaryProp eq binary'23ABFF'", "$it => ($it.BinaryProp.ToArray() == System.Byte[])")]
        [InlineData("XElementProp eq '<name />'", "$it => ($it.XElementProp.ToString() == \"<name />\")")]
        public void NonstandardEdmPrimtives(string filter, string expression)
        {
            var filters = VerifyQueryDeserialization<DataTypes>(filter, expression, NotTesting);

            RunFilters(filters,
                new DataTypes
                {
                    UShortProp = 12,
                    ULongProp = 12,
                    UIntProp = 12,
                    CharProp = 'a',
                    CharArrayProp = new[] { 'a' },
                    BinaryProp = new Binary(new byte[] { 35, 171, 255 }),
                    XElementProp = new XElement("name")
                },
                new { WithNullPropagation = true, WithoutNullPropagation = true });
        }

        [Theory]
        [InlineData("BinaryProp eq binary'23ABFF'", "$it => ($it.BinaryProp.ToArray() == System.Byte[])", true, true)]
        [InlineData("BinaryProp ne binary'23ABFF'", "$it => ($it.BinaryProp.ToArray() != System.Byte[])", false, false)]
        [InlineData("ByteArrayProp eq binary'23ABFF'", "$it => ($it.ByteArrayProp == System.Byte[])", true, true)]
        [InlineData("ByteArrayProp ne binary'23ABFF'", "$it => ($it.ByteArrayProp != System.Byte[])", false, false)]
        [InlineData("binary'23ABFF' eq binary'23ABFF'", "$it => (System.Byte[] == System.Byte[])", true, true)]
        [InlineData("binary'23ABFF' ne binary'23ABFF'", "$it => (System.Byte[] != System.Byte[])", false, false)]
        [InlineData("ByteArrayPropWithNullValue ne binary'23ABFF'", "$it => ($it.ByteArrayPropWithNullValue != System.Byte[])", true, true)]
        [InlineData("ByteArrayPropWithNullValue ne ByteArrayPropWithNullValue", "$it => ($it.ByteArrayPropWithNullValue != $it.ByteArrayPropWithNullValue)", false, false)]
        [InlineData("ByteArrayPropWithNullValue ne null", "$it => ($it.ByteArrayPropWithNullValue != null)", false, false)]
        [InlineData("ByteArrayPropWithNullValue eq null", "$it => ($it.ByteArrayPropWithNullValue == null)", true, true)]
        [InlineData("null ne ByteArrayPropWithNullValue", "$it => (null != $it.ByteArrayPropWithNullValue)", false, false)]
        [InlineData("null eq ByteArrayPropWithNullValue", "$it => (null == $it.ByteArrayPropWithNullValue)", true, true)]
        public void ByteArrayComparisons(string filter, string expression, bool withNullPropagation, object withoutNullPropagation)
        {
            var filters = VerifyQueryDeserialization<DataTypes>(filter, expression, NotTesting);
            RunFilters(filters,
                new DataTypes
                {
                    BinaryProp = new Binary(new byte[] { 35, 171, 255 }),
                    ByteArrayProp = new byte[] { 35, 171, 255 }
                },
                new { WithNullPropagation = withNullPropagation, WithoutNullPropagation = withoutNullPropagation });
        }

        [Theory]
        [InlineData("binary'23ABFF' ge binary'23ABFF'", "GreaterThanOrEqual")]
        [InlineData("binary'23ABFF' le binary'23ABFF'", "LessThanOrEqual")]
        [InlineData("binary'23ABFF' lt binary'23ABFF'", "LessThan")]
        [InlineData("binary'23ABFF' gt binary'23ABFF'", "GreaterThan")]
        [InlineData("binary'23ABFF' add binary'23ABFF'", "Add")]
        [InlineData("binary'23ABFF' sub binary'23ABFF'", "Subtract")]
        [InlineData("binary'23ABFF' mul binary'23ABFF'", "Multiply")]
        [InlineData("binary'23ABFF' div binary'23ABFF'", "Divide")]
        public void DisAllowed_ByteArrayComparisons(string filter, string op)
        {
            Assert.Throws<ODataException>(
                () => Bind<DataTypes>(filter),
                Error.Format("A binary operator with incompatible types was detected. Found operand types 'Edm.Binary' and 'Edm.Binary' for operator kind '{0}'.", op));
        }

        [Theory]
        [InlineData("NullableUShortProp eq 12", "$it => (Convert($it.NullableUShortProp.Value) == Convert(12))")]
        [InlineData("NullableULongProp eq 12L", "$it => (Convert($it.NullableULongProp.Value) == Convert(12))")]
        [InlineData("NullableUIntProp eq 12", "$it => (Convert($it.NullableUIntProp.Value) == Convert(12))")]
        [InlineData("NullableCharProp eq 'a'", "$it => ($it.NullableCharProp.Value.ToString() == \"a\")")]
        public void Nullable_NonstandardEdmPrimitives(string filter, string expression)
        {
            var filters = VerifyQueryDeserialization<DataTypes>(filter, expression, NotTesting);

            RunFilters(filters,
                new DataTypes(),
                new { WithNullPropagation = false, WithoutNullPropagation = typeof(InvalidOperationException) });
        }

        [Fact]
        public void MultipleConstants_Are_Parameterized()
        {
            VerifyQueryDeserialization("ProductName eq '1' or ProductName eq '2' or ProductName eq '3' or ProductName eq '4'",
                "$it => (((($it.ProductName == \"1\") OrElse ($it.ProductName == \"2\")) OrElse ($it.ProductName == \"3\")) OrElse ($it.ProductName == \"4\"))",
                NotTesting);
        }

        [Fact]
        public void Constants_Are_Not_Parameterized_IfDisabled()
        {
            var filters = VerifyQueryDeserialization("ProductName eq '1'", settingsCustomizer: (settings) =>
                {
                    settings.EnableConstantParameterization = false;
                });

            Assert.Equal("$it => ($it.ProductName == \"1\")", (filters.WithoutNullPropagation as Expression).ToString());
        }

        #region Negative Tests

        [Fact]
        public void TypeMismatchInComparison()
        {
            Assert.Throws<ODataException>(() => Bind("length(123) eq 12"));
        }

        #endregion

        private Expression<Func<Product, bool>> Bind(string filter, ODataQuerySettings querySettings = null)
        {
            return Bind<Product>(filter, querySettings);
        }

        private Expression<Func<T, bool>> Bind<T>(string filter, ODataQuerySettings querySettings = null) where T : class
        {
            IEdmModel model = GetModel<T>();
            FilterClause filterNode = CreateFilterNode(filter, model, typeof(T));

            if (querySettings == null)
            {
                querySettings = CreateSettings();
            }

            return Bind<T>(filterNode, model, CreateFakeAssembliesResolver(), querySettings);
        }

        private static Expression<Func<TEntityType, bool>> Bind<TEntityType>(FilterClause filterNode, IEdmModel model, IAssembliesResolver assembliesResolver, ODataQuerySettings querySettings)
        {
            return FilterBinder.Bind<TEntityType>(filterNode, model, assembliesResolver, querySettings);
        }

        private IAssembliesResolver CreateFakeAssembliesResolver()
        {
            return new NoAssembliesResolver();
        }

        private FilterClause CreateFilterNode(string filter, IEdmModel model, Type entityType)
        {
            var queryUri = new Uri(_serviceBaseUri, String.Format("Products?$filter={0}", Uri.EscapeDataString(filter)));
            IEdmEntityType productType = model.SchemaElements.OfType<IEdmEntityType>().Single(t => t.Name == entityType.Name);
            return ODataUriParser.ParseFilter(filter, model, productType);
        }

        private static ODataQuerySettings CreateSettings()
        {
            return new ODataQuerySettings
            {
                HandleNullPropagation = HandleNullPropagationOption.False // A value other than Default is required for calls to Bind.
            };
        }

        private void RunFilters<T>(dynamic filters, T product, dynamic expectedValue)
        {
            var filterWithNullPropagation = filters.WithNullPropagation as Expression<Func<T, bool>>;
            if (expectedValue.WithNullPropagation is Type)
            {
                Assert.Throws(expectedValue.WithNullPropagation as Type, () => RunFilter(filterWithNullPropagation, product));
            }
            else
            {
                Assert.Equal(RunFilter(filterWithNullPropagation, product), expectedValue.WithNullPropagation);
            }

            var filterWithoutNullPropagation = filters.WithoutNullPropagation as Expression<Func<T, bool>>;
            if (expectedValue.WithoutNullPropagation is Type)
            {
                Assert.Throws(expectedValue.WithoutNullPropagation as Type, () => RunFilter(filterWithoutNullPropagation, product));
            }
            else
            {
                Assert.Equal(RunFilter(filterWithoutNullPropagation, product), expectedValue.WithoutNullPropagation);
            }
        }

        private bool RunFilter<T>(Expression<Func<T, bool>> filter, T instance)
        {
            return filter.Compile().Invoke(instance);
        }

        private dynamic VerifyQueryDeserialization(string filter, string expectedResult = null, string expectedResultWithNullPropagation = null, Action<ODataQuerySettings> settingsCustomizer = null)
        {
            return VerifyQueryDeserialization<Product>(filter, expectedResult, expectedResultWithNullPropagation, settingsCustomizer);
        }

        private dynamic VerifyQueryDeserialization<T>(string filter, string expectedResult = null, string expectedResultWithNullPropagation = null, Action<ODataQuerySettings> settingsCustomizer = null) where T : class
        {
            IEdmModel model = GetModel<T>();
            FilterClause filterNode = CreateFilterNode(filter, model, typeof(T));
            IAssembliesResolver assembliesResolver = CreateFakeAssembliesResolver();

            Func<ODataQuerySettings, ODataQuerySettings> customizeSettings = (settings) =>
            {
                if (settingsCustomizer != null)
                {
                    settingsCustomizer.Invoke(settings);
                }

                return settings;
            };

            var filterExpr = Bind<T>(
                filterNode,
                model,
                assembliesResolver,
                customizeSettings(new ODataQuerySettings { HandleNullPropagation = HandleNullPropagationOption.False }));

            if (!String.IsNullOrEmpty(expectedResult))
            {
                VerifyExpression(filterExpr, expectedResult);
            }

            expectedResultWithNullPropagation = expectedResultWithNullPropagation ?? expectedResult;

            var filterExprWithNullPropagation = Bind<T>(
                filterNode,
                model,
                assembliesResolver,
                customizeSettings(new ODataQuerySettings { HandleNullPropagation = HandleNullPropagationOption.True }));

            if (!String.IsNullOrEmpty(expectedResultWithNullPropagation))
            {
                VerifyExpression(filterExprWithNullPropagation, expectedResultWithNullPropagation ?? expectedResult);
            }

            return new
            {
                WithNullPropagation = filterExprWithNullPropagation,
                WithoutNullPropagation = filterExpr
            };
        }

        private void VerifyExpression(Expression filter, string expectedExpression)
        {
            // strip off the beginning part of the expression to get to the first
            // actual query operator
            string resultExpression = ExpressionStringBuilder.ToString(filter);
            Assert.True(resultExpression == expectedExpression,
                String.Format("Expected expression '{0}' but the deserializer produced '{1}'", expectedExpression, resultExpression));
        }

        private IEdmModel GetModel<T>() where T : class
        {
            Type key = typeof(T);
            IEdmModel value;

            if (!_modelCache.TryGetValue(key, out value))
            {
                ODataModelBuilder model = new ODataConventionModelBuilder();
                model.EntitySet<T>("Products");
                value = _modelCache[key] = model.GetEdmModel();
            }
            return value;
        }

        private T? ToNullable<T>(object value) where T : struct
        {
            return value == null ? null : (T?)Convert.ChangeType(value, typeof(T));
        }

        private class NoAssembliesResolver : IAssembliesResolver
        {
            public ICollection<Assembly> GetAssemblies()
            {
                return new Assembly[0];
            }
        }
    }
}
