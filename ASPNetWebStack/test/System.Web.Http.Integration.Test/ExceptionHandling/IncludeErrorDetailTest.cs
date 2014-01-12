﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using Microsoft.TestCommon;
using Newtonsoft.Json.Linq;

namespace System.Web.Http
{
    public class IncludeErrorDetailTest
    {
        public static TheoryDataSet ThrowingOnActionIncludesErrorDetailData
        {
            get
            {
                return new TheoryDataSet<bool, IncludeErrorDetailPolicy, bool?, bool>()
                {
                    // isLocal, includeErrorDetail, customErrors, expectErrorDetail
                    { true, IncludeErrorDetailPolicy.LocalOnly, true, true },
                    { false, IncludeErrorDetailPolicy.LocalOnly, true, false },
                    { true, IncludeErrorDetailPolicy.LocalOnly, false, true },
                    { false, IncludeErrorDetailPolicy.LocalOnly, false, false },
                    { true, IncludeErrorDetailPolicy.LocalOnly, null, true },
                    { false, IncludeErrorDetailPolicy.LocalOnly, null, false },
                    
                    { true, IncludeErrorDetailPolicy.Always, true, true },
                    { false, IncludeErrorDetailPolicy.Always, true, true },
                    { true, IncludeErrorDetailPolicy.Always, false, true },
                    { false, IncludeErrorDetailPolicy.Always, false, true },
                    { true, IncludeErrorDetailPolicy.Always, null, true },
                    { false, IncludeErrorDetailPolicy.Always, null, true },
                    
                    { true, IncludeErrorDetailPolicy.Never, true, false },
                    { false, IncludeErrorDetailPolicy.Never, true, false },
                    { true, IncludeErrorDetailPolicy.Never, false, false },
                    { false, IncludeErrorDetailPolicy.Never, false, false },
                    { true, IncludeErrorDetailPolicy.Never, null, false },
                    { false, IncludeErrorDetailPolicy.Never, null, false },

                    { true, IncludeErrorDetailPolicy.Default, true, false },
                    { false, IncludeErrorDetailPolicy.Default, true, false },
                    { true, IncludeErrorDetailPolicy.Default, false, true },
                    { false, IncludeErrorDetailPolicy.Default, false, true },
                    { true, IncludeErrorDetailPolicy.Default, null, true },
                    { false, IncludeErrorDetailPolicy.Default, null, false }
                };
            }
        }

        [Theory]
        [PropertyData("ThrowingOnActionIncludesErrorDetailData")]
        public void ThrowingOnActionIncludesErrorDetail(bool isLocal, IncludeErrorDetailPolicy includeErrorDetail, bool? customErrors, bool expectErrorDetail)
        {
            string controllerName = "Exception";
            string requestUrl = String.Format("{0}/{1}/{2}", "http://www.foo.com", controllerName, "ArgumentNull");
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Properties["MS_IsLocal"] = new Lazy<bool>(() => isLocal);
            if (customErrors != null)
            {
                request.Properties["MS_IncludeErrorDetail"] = new Lazy<bool>(() => !(bool)customErrors);
            }

            ScenarioHelper.RunTest(
                controllerName,
                "/{action}",
                request,
                (response) =>
                {
                    if (expectErrorDetail)
                    {
                        AssertResponseIncludesErrorDetail(response);
                    }
                    else
                    {
                        AssertResponseDoesNotIncludeErrorDetail(response);
                    }
                },
                (config) =>
                {
                    config.IncludeErrorDetailPolicy = includeErrorDetail;
                }
            );
        }

        private void AssertResponseIncludesErrorDetail(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            dynamic json = JToken.Parse(response.Content.ReadAsStringAsync().Result);
            string result = json.ExceptionType;
            Assert.Equal(typeof(ArgumentNullException).FullName, result);
        }

        private void AssertResponseDoesNotIncludeErrorDetail(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            JObject json = JToken.Parse(response.Content.ReadAsStringAsync().Result) as JObject;
            Assert.Equal(1, json.Count);
            string errorMessage = ((JValue)json["Message"]).ToString();
            Assert.Equal("An error has occurred.", errorMessage);
        }
    }
}