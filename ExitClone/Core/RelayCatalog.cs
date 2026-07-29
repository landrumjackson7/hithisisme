using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ExitClone.Core
{
    /// <summary>
    /// The relay fleet. Built-in entries point at publicly reachable regional edges so route
    /// probing works out of the box; a fleet operator can override the whole list by dropping a
    /// relays.json next to the settings file.
    /// </summary>
    public static class RelayCatalog
    {
        public static string OverridePath => Path.Combine(SettingsStore.DataDirectory, "relays.json");

        private static readonly Relay[] BuiltIn =
        {
            R("us-east", "US East", "Ashburn", "United States", "North America", "ec2.us-east-1.amazonaws.com"),
            R("us-east2", "US East 2", "Columbus", "United States", "North America", "ec2.us-east-2.amazonaws.com"),
            R("us-central", "US Central", "Dallas", "United States", "North America", "speedtest.dallas.linode.com"),
            R("us-central2", "US Central 2", "Chicago", "United States", "North America", "speedtest.chicago.linode.com"),
            R("us-west", "US West", "San Jose", "United States", "North America", "ec2.us-west-1.amazonaws.com"),
            R("us-west2", "US West 2", "Portland", "United States", "North America", "ec2.us-west-2.amazonaws.com"),
            R("ca-central", "Canada Central", "Montreal", "Canada", "North America", "ec2.ca-central-1.amazonaws.com"),
            R("sa-east", "South America", "Sao Paulo", "Brazil", "South America", "ec2.sa-east-1.amazonaws.com"),
            R("eu-west", "EU West", "Dublin", "Ireland", "Europe", "ec2.eu-west-1.amazonaws.com"),
            R("eu-central", "EU Central", "Frankfurt", "Germany", "Europe", "ec2.eu-central-1.amazonaws.com"),
            R("eu-south", "EU South", "Milan", "Italy", "Europe", "ec2.eu-south-1.amazonaws.com"),
            R("eu-north", "EU North", "Stockholm", "Sweden", "Europe", "ec2.eu-north-1.amazonaws.com"),
            R("uk-south", "UK South", "London", "United Kingdom", "Europe", "ec2.eu-west-2.amazonaws.com"),
            R("me-south", "Middle East", "Bahrain", "Bahrain", "Middle East", "ec2.me-south-1.amazonaws.com"),
            R("af-south", "Africa South", "Cape Town", "South Africa", "Africa", "ec2.af-south-1.amazonaws.com"),
            R("ap-south", "India", "Mumbai", "India", "Asia", "ec2.ap-south-1.amazonaws.com"),
            R("ap-southeast", "Singapore", "Singapore", "Singapore", "Asia", "ec2.ap-southeast-1.amazonaws.com"),
            R("ap-northeast", "Japan", "Tokyo", "Japan", "Asia", "ec2.ap-northeast-1.amazonaws.com"),
            R("ap-northeast2", "Korea", "Seoul", "South Korea", "Asia", "ec2.ap-northeast-2.amazonaws.com"),
            R("ap-east", "Hong Kong", "Hong Kong", "Hong Kong", "Asia", "ec2.ap-east-1.amazonaws.com"),
            R("au-southeast", "Australia", "Sydney", "Australia", "Oceania", "ec2.ap-southeast-2.amazonaws.com")
        };

        private static Relay R(string id, string name, string city, string country, string region, string host)
        {
            return new Relay
            {
                Id = id,
                Name = name,
                City = city,
                Country = country,
                Region = region,
                Host = host,
                ProbePort = 443,
                SocksPort = 1080,
                Provider = "ExitClone Edge"
            };
        }

        public static List<Relay> Load()
        {
            try
            {
                if (File.Exists(OverridePath))
                {
                    var json = File.ReadAllText(OverridePath);
                    var custom = JsonConvert.DeserializeObject<List<Relay>>(json);
                    if (custom != null && custom.Count > 0) return custom;
                }
            }
            catch (Exception)
            {
                // Fall through to the built-in fleet if the override is unreadable.
            }
            return BuiltIn.Select(Clone).ToList();
        }

        public static void Save(List<Relay> relays)
        {
            Directory.CreateDirectory(SettingsStore.DataDirectory);
            File.WriteAllText(OverridePath, JsonConvert.SerializeObject(relays, Formatting.Indented));
        }

        private static Relay Clone(Relay r) => new Relay
        {
            Id = r.Id,
            Name = r.Name,
            City = r.City,
            Country = r.Country,
            Region = r.Region,
            Host = r.Host,
            ProbePort = r.ProbePort,
            SocksPort = r.SocksPort,
            Provider = r.Provider
        };
    }
}
