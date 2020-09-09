using IFSEngine.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Serialization
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// Avoid annotating model classes with serialization attributes by defining a resolver here.
    /// </remarks>
    internal class IfsContractResolver : DefaultContractResolver
    {
        private readonly TransformFunctionConverter tfConv;

        public IfsContractResolver(bool ignoreTransformVersions)
        {
            tfConv = new TransformFunctionConverter(ignoreTransformVersions);
        }

        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (objectType == typeof(TransformFunction))
                return tfConv;
            return base.ResolveContractConverter(objectType);
        }
    }
}
