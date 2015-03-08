﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.TestCommon;

namespace System.Web.Http
{
    class HttpErrorKeysTest
    {
        public static TheoryDataSet<Func<string>, string> ErrorKeys
        {
            get
            {
                return new TheoryDataSet<Func<string>, string>
                {       
                    { () => HttpErrorKeys.MessageKey, "Message" },
                    { () => HttpErrorKeys.MessageDetailKey, "MessageDetail" },
                    { () => HttpErrorKeys.ModelStateKey, "ModelState" },
                    { () => HttpErrorKeys.ExceptionMessageKey, "ExceptionMessage" },
                    { () => HttpErrorKeys.ExceptionTypeKey, "ExceptionType" },
                    { () => HttpErrorKeys.StackTraceKey, "StackTrace" },
                    { () => HttpErrorKeys.InnerExceptionKey, "InnerException" },
                    { () => HttpErrorKeys.MessageLanguageKey, "MessageLanguage" },
                    { () => HttpErrorKeys.ErrorCodeKey, "ErrorCode" }
                };
            }
        }

        [Theory]
        [PropertyData("ErrorKeys")]
        public void HttpErrorKeyProperties_Returns_CorrectKeys(Func<string> productUnderTest, string expectedResult)
        {
            // Act
            string actualResult = productUnderTest.Invoke();

            // Assert
            Assert.Equal(expectedResult, actualResult);
        }
    }
}
