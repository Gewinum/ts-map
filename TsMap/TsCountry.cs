﻿using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using TsMap.HashFiles;

namespace TsMap
{
    public class TsCountry
    {
        private TsMapper _mapper;

        [JsonIgnore]
        public ulong Token { get; }

        public int CountryId { get; }
        public string Name { get; }
        [JsonIgnore]
        public string LocalizationToken { get; }
        public string CountryCode { get; }
        public float X { get; }
        public float Y { get; }
        [JsonIgnore]
        public Dictionary<string, string> LocalizedNames { get; }

        public TsCountry(TsMapper mapper, string path)
        {
            _mapper = mapper;
            var file = _mapper.Rfs.GetFileEntry(path);

            if (file == null) return;
            LocalizedNames = new Dictionary<string, string>();
            var fileContent = file.Entry.Read();

            var lines = Encoding.UTF8.GetString(fileContent).Split('\n');

            foreach (var line in lines)
            {
                var (validLine, key, value) = SiiHelper.ParseLine(line);
                if (!validLine) continue;

                if (key == "country_data")
                {
                    Token = ScsHash.StringToToken(SiiHelper.Trim(value.Split('.')[2]));
                }
                else if (key == "country_id")
                {
                    CountryId = int.Parse(value);
                }
                else if (key == "name")
                {
                    Name = value.Split('"')[1];
                }
                else if (key == "name_localized")
                {
                    LocalizationToken = value.Split('"')[1];
                    LocalizationToken = LocalizationToken.Replace("@", "");
                }
                else if (key == "country_code")
                {
                    CountryCode = value.Split('"')[1];
                }
                else if (key == "pos")
                {
                    var vector = value.Split('(')[1].Split(')')[0];
                    var values = vector.Split(',');
                    X = float.Parse(values[0], CultureInfo.InvariantCulture);
                    Y = float.Parse(values[2], CultureInfo.InvariantCulture);
                }
            }
        }

        public void AddLocalizedName(string locale, string name)
        {
            if (!LocalizedNames.ContainsKey(locale)) LocalizedNames.Add(locale, name);
        }

        public string GetLocalizedName(string locale)
        {
            return (LocalizedNames.ContainsKey(locale)) ? LocalizedNames[locale] : Name;
        }
    }
}
