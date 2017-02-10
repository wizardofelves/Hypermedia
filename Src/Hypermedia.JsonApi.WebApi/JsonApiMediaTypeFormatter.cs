﻿using System;
using System.Collections;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Web.Http;
using Hypermedia.Json;
using Hypermedia.Metadata;
using Hypermedia.WebApi;
using Hypermedia.WebApi.Json;
using JsonLite.Ast;

namespace Hypermedia.JsonApi.WebApi
{
    public class JsonApiMediaTypeFormatter : Hypermedia.WebApi.Json.JsonMediaTypeFormatter
    {
        const string Name = "jsonapi";
        const string MediaTypeName = "application/vnd.api+json";
        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="contractResolver">The resource contract resolver used to resolve the contracts at runtime.</param>
        public JsonApiMediaTypeFormatter(IContractResolver contractResolver) : this(contractResolver, DasherizedFieldNamingStrategy.Instance, DefaultJsonOutputFormatter.Instance) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="contractResolver">The resource contract resolver used to resolve the contracts at runtime.</param>
        /// <param name="fieldNamingStratgey">The field naming strategy to use.</param>
        public JsonApiMediaTypeFormatter(
            IContractResolver contractResolver, 
            IFieldNamingStrategy fieldNamingStratgey) : this(contractResolver, fieldNamingStratgey, DefaultJsonOutputFormatter.Instance) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="contractResolver">The resource contract resolver used to resolve the contracts at runtime.</param>
        /// <param name="fieldNamingStratgey">The field naming strategy to use.</param>
        /// <param name="outputFormatter">The output formatter to apply when writing the output.</param>
        JsonApiMediaTypeFormatter(
            IContractResolver contractResolver, 
            IFieldNamingStrategy fieldNamingStratgey, 
            IJsonOutputFormatter outputFormatter) : base(Name, MediaTypeName, contractResolver, fieldNamingStratgey, outputFormatter) { }

        /// <summary>
        /// Returns a specialized instance of the <see cref="T:System.Net.Http.Formatting.MediaTypeFormatter"/> that can format a response for the given parameters.
        /// </summary>
        /// <param name="type">The type to format.</param>
        /// <param name="request">The request.</param>
        /// <param name="mediaType">The media type.</param>
        /// <returns>Returns <see cref="T:System.Net.Http.Formatting.MediaTypeFormatter"/>.</returns>
        public override MediaTypeFormatter GetPerRequestFormatterInstance(Type type, HttpRequestMessage request, MediaTypeHeaderValue mediaType)
        {
            return new JsonApiMediaTypeFormatter(ContractResolver, GetPerRequestFieldNamingStrategy(request), GetPerRequestOutputFormatter(request));
        }

        /// <summary>
        /// Creates an instance of the patch object for the media type.
        /// </summary>
        /// <param name="type">The type of the inner instance that is being patched.</param>
        /// <param name="contractResolver">The contract resolver.</param>
        /// <param name="jsonValue">The JSON value that represents the patch values.</param>
        /// <returns>The instance of the patch.</returns>
        protected override IPatch CreatePatch(Type type, IContractResolver contractResolver, JsonValue jsonValue)
        {
            var patch = typeof(JsonApiPatch<>).MakeGenericType(type.GenericTypeArguments[0]);

            var constructor = patch.GetConstructor(new[] { typeof(IContractResolver), typeof(IFieldNamingStrategy), typeof(JsonObject) });
            Debug.Assert(constructor != null);

            return (IPatch)constructor.Invoke(new object[] { ContractResolver, FieldNamingStrategy, jsonValue });
        }

        /// <summary>
        /// Deserialize an object.
        /// </summary>
        /// <param name="type">The type of the object to deserialize.</param>
        /// <param name="jsonValue">The JSON value that represents the object to deserialize.</param>
        protected override object DeserializeValue(Type type, JsonValue jsonValue)
        {
            var jsonObject = jsonValue as JsonObject;

            if (jsonObject == null)
            {
                throw new HypermediaWebApiException("The top level JSON value must be an Object.");
            }

            var serializer = new JsonApiSerializer(ContractResolver, FieldNamingStrategy);

            if (TypeHelper.IsEnumerable(type))
            {
                return serializer.DeserializeMany(jsonObject);
            }

            return serializer.Deserialize(jsonObject);
        }

        /// <summary>
        /// Serialize the value into an JSON AST.
        /// </summary>
        /// <param name="type">The type to serialize from.</param>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The JSON object that represents the serialized value.</returns>
        protected override JsonValue SerializeValue(Type type, object value)
        {
            var serializer = CreateSerializer(type);

            if (TypeHelper.IsEnumerable(type))
            {
                return serializer.SerializeMany((IEnumerable)value);
            }

            return serializer.Serialize(value);
        }

        /// <summary>
        /// Create the appropriate serializer instance.
        /// </summary>
        /// <param name="type">The element type that is to be serialized.</param>
        /// <returns>The serializer to use for the given type.</returns>
        IJsonApiSerializer CreateSerializer(Type type)
        {
            if (ContractResolver.CanResolve(TypeHelper.GetUnderlyingType(type)))
            {
                return new JsonApiSerializer(ContractResolver, FieldNamingStrategy);
            }

            if (TypeHelper.GetUnderlyingType(type) == typeof(JsonApiError))
            {
                return JsonApiErrorSerializer.Instance;
            }

            return JsonApiHttpErrorSerializer.Instance;
        }

        /// <summary>
        /// Returns a value indicating whether or not the dictionary has a metadata mapping for the given type.
        /// </summary>
        /// <param name="type">The element type to test for a mapping.</param>
        /// <returns>true if the given type has a mapping, false if not.</returns>
        protected override bool CanReadOrWrite(Type type)
        {
            return ContractResolver.CanResolve(TypeHelper.GetUnderlyingType(type))
                || TypeHelper.GetUnderlyingType(type) == typeof(JsonApiError)
                || type == typeof(HttpError);
        }
    }
}