using System;
using System.Collections.Generic;
using System.Linq;
using Hypermedia.Json;
using JsonLite.Ast;

namespace Hypermedia.JsonApi
{
    public sealed class JsonApiErrorSerializer
    {
        /// <summary>
        /// Serialize a list of errors as an error response.
        /// </summary>
        /// <param name="errors">The list of errors to serialize as an error response.</param>
        /// <returns>The JSON object that represents the serialized error response.</returns>
        public JsonObject Serialize(params JsonApiError[] errors)
        {
            return Serialize((IEnumerable<JsonApiError>)errors);
        }

        /// <summary>
        /// Serialize a list of errors as an error response.
        /// </summary>
        /// <param name="errors">The list of errors to serialize as an error response.</param>
        /// <returns>The JSON object that represents the serialized error response.</returns>
        public JsonObject Serialize(IEnumerable<JsonApiError> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            return new JsonObject(new JsonMember("errors", new JsonArray(errors.Select(SerializeError).ToList())));
        }

        /// <summary>
        /// Serialize an error.
        /// </summary>
        /// <param name="error">The error to serialize.</param>
        /// <returns>The JSON object that represents the serialized error.</returns>
        JsonObject SerializeError(JsonApiError error)
        {
            var serializer = new JsonSerializer(new JsonConverterFactory());

            return new JsonObject(SerializeMembers(serializer, error).ToList());
        }

        /// <summary>
        /// Returns a list of JSON members that make up the error.
        /// </summary>
        /// <param name="serializer">The JSON serializer to use when serializing the values.</param>
        /// <param name="error">The error to serialized into its members.</param>
        /// <returns>The list of JSON members that represent the JSON API error.</returns>
        IEnumerable<JsonMember> SerializeMembers(JsonSerializer serializer, JsonApiError error)
        {
            if (error.Status != null)
            {
                yield return new JsonMember("status", serializer.SerializeValue(error.Status));
            }

            if (error.Code != null)
            {
                yield return new JsonMember("code", serializer.SerializeValue(error.Code));
            }

            if (error.Title != null)
            {
                yield return new JsonMember("title", serializer.SerializeValue(error.Title));
            }

            if (error.Detail != null)
            {
                yield return new JsonMember("detail", serializer.SerializeValue(error.Detail));
            }
        }
    }
}