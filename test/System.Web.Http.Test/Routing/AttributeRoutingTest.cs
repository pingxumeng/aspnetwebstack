﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading;
using Microsoft.TestCommon;

namespace System.Web.Http.Routing
{
    public class AttributeRoutingTest
    {
        [Theory]
        [InlineData("GET", "controller/42", "Get42")]
        // Tests inline route constraints
        [InlineData("GET", "controller/Ethan", "GetByNameEthan")]
        // Tests the HTTP method constraint
        [InlineData("PUT", "controller/42", "Put42")]
        // Tests optional parameters
        [InlineData("GET", "optional/1/2", "Optional12")]
        [InlineData("GET", "optional/1", "Optional1")]
        [InlineData("GET", "optional", "Optional0")]
        [InlineData("GET", "optionalwconstraint", "OptionalWithConstraint")]
        // Tests default values
        [InlineData("GET", "default/1/2", "Default12")]
        [InlineData("GET", "default/1", "Default1D2")]
        [InlineData("GET", "default", "DefaultD1D2")]
        // Test wildcard parameters
        [InlineData("GET", "wildcard/a/b/c", "Wildcarda/b/c")]
        // Test prefixes
        [InlineData("GET", "prefix", "PrefixedGet")]
        [InlineData("GET", "prefix/123", "PrefixedGetById123")]
        [InlineData("PUT", "prefix", "PrefixedPut")]
        // Test multiple routes to same action
        [InlineData("DELETE", "multi1", "multi")]
        [InlineData("DELETE", "multi2", "multi")]        
        // Test multiple verbs on the same route
        [InlineData("GET", "multiverb", "GET")]
        [InlineData("PUT", "multiverb", "PUT")]     
        // Test with default route
        [InlineData("GET", "prefix2/defaultroute/12", "get12")]
        [InlineData("PUT", "prefix2/defaultrouteoverride/12", "put12")]     
        [InlineData("POST", "prefix2", "post")]             
        public void AttributeRouting_RoutesToAction(string httpMethod, string uri, string responseBody)
        {
            var request = new HttpRequestMessage(new HttpMethod(httpMethod), "http://localhost/" + uri);

            var response = SubmitRequest(request);

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(responseBody, GetContentValue<string>(response));
        }

        [Theory]
        // default routes 
        [InlineData("GET", "prefix2/defaultroute/name", HttpStatusCode.NotFound)] // miss route constraint
        [InlineData("PUT", "prefix2/defaultroute/12", HttpStatusCode.MethodNotAllowed)] // override, different url
        [InlineData("POST", "prefix", HttpStatusCode.MethodNotAllowed)]
        // wrong verb, 405
        [InlineData("MISSING", "controller/42", HttpStatusCode.MethodNotAllowed)] 
        [InlineData("MISSING", "default/1/2", HttpStatusCode.MethodNotAllowed)] 
        [InlineData("MISSING", "controller/Ethan", HttpStatusCode.MethodNotAllowed)]
        public void AttributeRouting_Failures(string httpMethod, string uri, HttpStatusCode failureCode)
        {
            var request = new HttpRequestMessage(new HttpMethod(httpMethod), "http://localhost/" + uri);

            var response = SubmitRequest(request);

            Assert.False(response.IsSuccessStatusCode);
            Assert.Equal(failureCode, response.StatusCode);      
        }

        [Fact]
        public void RoutePrefixAttribute_IsSingleInstance()
        {
            var attr = typeof(RoutePrefixAttribute);
            var attrs = attr.GetCustomAttributes(typeof(AttributeUsageAttribute), false);
            var usage = (AttributeUsageAttribute)attrs[0];

            Assert.Equal(AttributeTargets.Class, usage.ValidOn);
            Assert.False(usage.AllowMultiple); // only 1 per class
            Assert.False(usage.Inherited); // RoutePrefix is not inherited. 
        }

        private static HttpResponseMessage SubmitRequest(HttpRequestMessage request)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();

            HttpServer server = new HttpServer(config);
            using (HttpMessageInvoker client = new HttpMessageInvoker(server))
            {
                return client.SendAsync(request, CancellationToken.None).Result;
            }
        }

        private static T GetContentValue<T>(HttpResponseMessage response)
        {
            T value;
            response.TryGetContentValue<T>(out value);
            return value;
        }
    }

    public class AttributedController : ApiController
    {
        [Route("controller/{id:int}")]
        public string Get(int id)
        {
            return "Get" + id;
        }

        [Route("controller/{name}")]
        public string GetByName(string name)
        {
            return "GetByName" + name;
        }

        [Route("controller/{id}")]
        public string Put(string id)
        {
            return "Put" + id;
        }

        [HttpGet]
        [Route("optional/{opt1?}/{opt2?}")]
        public string Optional(int opt1, string opt2)
        {
            return "Optional" + opt1 + opt2;
        }

        [HttpGet]
        [Route("optionalwconstraint/{opt:int?}")]
        public string OptionalWithConstraint(string opt)
        {
            return "OptionalWithConstraint" + opt;
        }

        [HttpGet]
        [Route("default/{default1=D1}/{default2=D2}")]
        public string Default(string default1, string default2)
        {
            return "Default" + default1 + default2;
        }

        [HttpGet]
        [Route("wildcard/{*wildcard}")]
        public string Wildcard(string wildcard)
        {
            return "Wildcard" + wildcard;
        }

        [HttpGet]
        [HttpPut]
        [Route("multiverb")]
        public string MultiVerbs()
        {
            return Request.Method.ToString();
        }

        [HttpDelete] // Pick a unique verb 
        [Route("multi1")]
        [Route("multi2")]
        public string MultiRoute()
        {
            return "multi";
        }

    }

    [RoutePrefix("prefix")]
    public class PrefixedController : ApiController
    {
        // Should not be reachable be our routes since there's no route attribute. 
        public void Post()
        {
        }

        [Route("")]
        public string Get()
        {
            return "PrefixedGet";
        }

        [Route] // same behavior as Route("")
        public string Put()
        {
            return "PrefixedPut";
        }

        [HttpGet]
        [Route("{id}")]
        public string GetById(int id)
        {
            return "PrefixedGetById" + id;
        }
    }

    [RoutePrefix("prefix2")]
    [DefaultRoute("defaultroute/{id:int}")]
    public class DefaultRouteController : ApiController
    {
        // This gets default route
        public string Get(int id)
        {
            return "get" + id;
        }

        [Route] 
        public string Post()
        {
            return "post";
        }

        [Route("defaultrouteoverride/{id}")]
        public string Put(int id)
        {
            return "put" + id;
        }
    }
}
