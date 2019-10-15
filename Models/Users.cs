using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RemapService.Models.Users
{

    public partial class UserList
    {
        [JsonProperty("Users")]
        public List<User> Users { get; set; }

        [JsonProperty("TotalCount")]
        public long? TotalCount { get; set; }
    }

    public partial class User
    {
        [JsonProperty("Id")] public long Id { get; set; }

    }

    public partial class UserList
    {
        public static UserList FromJson(string json) => JsonConvert.DeserializeObject<UserList>(json, RemapService.Models.Users.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this UserList self) => JsonConvert.SerializeObject(self, RemapService.Models.Users.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },

        };
    }


}