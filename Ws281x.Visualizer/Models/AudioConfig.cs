using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Spectro.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ws281x.Visualizer.Models
{
    public class AudioConfig
    {
        public AudioFormat Format { get; set; }

        public string InputDevice { get; set; }

        public string OutputDevice { get; set; }

        public string Serialize()
        {
            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            return JsonConvert.SerializeObject(this, new JsonSerializerSettings()
            {
                ContractResolver = contractResolver,
                Formatting = Formatting.Indented,
                Converters = new JsonConverter[]
                {
                    new StringEnumConverter
                    {
                        NamingStrategy = new CamelCaseNamingStrategy(),
                    },
                },
            });
        }

        public static AudioConfig Deserialize(string json)
        {
            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            return JsonConvert.DeserializeObject<AudioConfig>(json, new JsonSerializerSettings()
            {
                ContractResolver = contractResolver,
                Formatting = Formatting.Indented,
                Converters = new JsonConverter[]
                {
                    new StringEnumConverter
                    {
                        NamingStrategy = new CamelCaseNamingStrategy(),
                    },
                },
            });
        }
    }
}
