using IFSEngine.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IFSEngine.Serialization
{
    public static class IfsSerializer
    {
        private static JsonSerializerSettings defaultSettings = new JsonSerializerSettings 
        { 
            ContractResolver = new IfsContractResolver(false),
            ObjectCreationHandling = ObjectCreationHandling.Replace//replace default palette
            //TypeNameHandling = TypeNameHandling.Auto
        };

        //TODO: with enum
        //private static JsonSerializerSettings ignoreTransformVersionSettings = new JsonSerializerSettings
        //{
        //    ContractResolver = new IfsContractResolver(true),
        //    ObjectCreationHandling = ObjectCreationHandling.Replace
        //    //TypeNameHandling = TypeNameHandling.Auto
        //};

        public static IFS LoadJson(string path)
        {
            var ifs = JsonConvert.DeserializeObject<IFS>(File.ReadAllText(path, Encoding.UTF8), defaultSettings);
            return ifs;
        }

        public static void SaveJson(IFS ifs, string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(ifs, defaultSettings), Encoding.UTF8);
        }

    }
}
