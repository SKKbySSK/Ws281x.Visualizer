using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using rpi_ws281x;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ws281x.Visualizer.Models
{
    public class LedConfig
    {
        public int LedCount { get; set; }

        public Pin Pin { get; set; }

        public StripType StripType { get; set; }

        public ControllerType ControllerType { get; set; }

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

        public static LedConfig Deserialize(string json)
        {
            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            return JsonConvert.DeserializeObject<LedConfig>(json, new JsonSerializerSettings()
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
