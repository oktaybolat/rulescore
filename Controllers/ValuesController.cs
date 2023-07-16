using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RulesCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RulesController : ControllerBase
    {
        [HttpPost("applyrules")]
        public IActionResult ApplyRules([FromBody] JsonDocument json)
        {
            if (json != null)
            {
                var jsoninArray = json.RootElement.GetProperty("JSONin").EnumerateArray().ToList();
                var jsonruleArray = json.RootElement.GetProperty("JSONrule").EnumerateArray().ToList();

                var result = new List<JsonElement>();

                foreach (var rule in jsonruleArray)
                {
                    if (rule.TryGetProperty("name", out var nameProp) &&
                        rule.TryGetProperty("rule", out var kuralProp) &&
                        rule.TryGetProperty("tvalue", out var tdegerProp) &&
                        rule.TryGetProperty("fvalue", out var fdegerProp))
                    {
                        var name = nameProp.GetString();
                        var kural = kuralProp.GetString();
                        var tdeger = tdegerProp.GetString();
                        var fdeger = fdegerProp.GetString();

                        Console.WriteLine($"sending Values for kural, tdeger, fdeger: {kural} {tdeger} {fdeger}");
                        var resultValue = EvaluateRule(kural, jsoninArray, tdeger, fdeger);
                        Console.WriteLine($"Calculated Value: {resultValue}");

                        var resultObj = new Dictionary<string, JsonElement>
                        {
                            { "name", JsonDocument.Parse($"\"{name}\"").RootElement },
                            { "value", resultValue }
                        };

                        result.Add(JsonDocument.Parse(JsonSerializer.Serialize(resultObj)).RootElement);
                    }
                }

                return Ok(JsonSerializer.Serialize(result));
            }

            return BadRequest();
        }

        public JsonElement EvaluateRule(string kural, List<JsonElement> gelenJson, string tdeger, string fdeger)
        {
            Console.WriteLine($"Kural: {kural}");
            Console.WriteLine($"Tdeger: {tdeger}");
            Console.WriteLine($"Fdeger: {fdeger}");

            if (string.IsNullOrEmpty(kural))
            {
                return GetParsedValue(tdeger, gelenJson);
            }
            else if (kural.Contains("!="))
            {
                Console.WriteLine("!=");
                var parts = kural.Split("!=");
                var left = parts[0].Trim();
                var right = parts[1].Trim();

                Console.WriteLine($"Left: {left}");
                Console.WriteLine($"Right: {right}");

                var leftValue = GetParsedValue(left, gelenJson).GetString();
                var rightValue = GetParsedValue(right, gelenJson).GetString();

                Console.WriteLine($"LeftValue: {leftValue}");
                Console.WriteLine($"RightValue: {rightValue}");

                if (leftValue != rightValue)
                {
                    return GetParsedValue(tdeger, gelenJson);
                }
                else
                {
                    return GetParsedValue(fdeger, gelenJson);
                }
            }
            else if (kural.Contains("=="))
            {
                Console.WriteLine("==");
                var parts = kural.Split("==");
                var left = parts[0].Trim();
                var right = parts[1].Trim();

                Console.WriteLine($"Left: {left}");
                Console.WriteLine($"Right: {right}");

                var leftValue = GetParsedValue(left, gelenJson).GetString();
                var rightValue = GetParsedValue(right, gelenJson).GetString();

                Console.WriteLine($"LeftValue: {leftValue}");
                Console.WriteLine($"RightValue: {rightValue}");

                if (leftValue == rightValue)
                {
                    return GetParsedValue(tdeger, gelenJson);
                }
                else
                {
                    return GetParsedValue(fdeger, gelenJson);
                }
            }
            else if (kural.Contains("<"))
            {
                Console.WriteLine("<");
                var parts = kural.Split("<");
                var left = parts[0].Trim();
                var right = parts[1].Trim();

                Console.WriteLine($"Left: {left}");
                Console.WriteLine($"Right: {right}");

                var leftValue = GetParsedValue(left, gelenJson).GetString();
                var rightValue = GetParsedValue(right, gelenJson).GetString();

                Console.WriteLine($"LeftValue: {leftValue}");
                Console.WriteLine($"RightValue: {rightValue}");

                if (Convert.ToInt32(leftValue) < Convert.ToInt32(rightValue))
                {
                    return GetParsedValue(tdeger, gelenJson);
                }
                else
                {
                    return GetParsedValue(fdeger, gelenJson);
                }
            }
            else if (kural.Contains(">"))
            {
                Console.WriteLine(">");
                var parts = kural.Split(">");
                var left = parts[0].Trim();
                var right = parts[1].Trim();

                Console.WriteLine($"Left: {left}");
                Console.WriteLine($"Right: {right}");

                var leftValue = GetParsedValue(left, gelenJson).GetString();
                var rightValue = GetParsedValue(right, gelenJson).GetString();

                Console.WriteLine($"LeftValue: {leftValue}");
                Console.WriteLine($"RightValue: {rightValue}");

                if (Convert.ToInt32(leftValue) > Convert.ToInt32(rightValue))
                {
                    return GetParsedValue(tdeger, gelenJson);
                }
                else
                {
                    return GetParsedValue(fdeger, gelenJson);
                }
            }

            return JsonDocument.Parse("\"\"").RootElement;
        }

        public JsonElement GetParsedValue(string value, List<JsonElement> gelenJson)
        {
            Console.WriteLine($"Value: {value}");

            var parsedValues = ParseString(value);

            foreach (var i in parsedValues)
            {
                Console.WriteLine($"Parsed Value: {i}");
            }

            // Parse edilmiş değerleri birleştirerek işleyelim.
            var resultValue = string.Join("", parsedValues.Select((v, index) =>
            {
                if (v.StartsWith("[DATE(") && v.EndsWith(")]"))
                {
                    var format = v.TrimStart("[DATE(".ToCharArray()).TrimEnd(")]".ToCharArray());
                    if (string.IsNullOrEmpty(format))
                    {
                        format = "dd/MM/yyyy";
                    }

                    var date = DateTime.Now.ToString(format);
                    return date;
                }
                else if (v.StartsWith("[NOW(") && v.EndsWith(")]"))
                {
                    var format = v.TrimStart("[NOW(".ToCharArray()).TrimEnd(")]".ToCharArray());
                    if (string.IsNullOrEmpty(format))
                    {
                        format = "dd/MM/yyyy hh:mm:ss";
                    }

                    var now = DateTime.Now.ToString(format);
                    return now;
                }
                else if (v.StartsWith("[") && v.EndsWith("]"))
                {
                    var alan = v.Trim('[', ']');
                    var alanValueElement = gelenJson.FirstOrDefault(j =>
                    {
                        JsonElement nameProp;
                        return j.TryGetProperty("name", out nameProp) && nameProp.GetString() == alan;
                    });

                    var alanValue = "";
                    if (alanValueElement.ToString() != "")
                    {
                        alanValue = alanValueElement.GetProperty("value").GetString();
                    }

                    return alanValue;
                }
                else
                {
                    return v;
                }
            }));

            return JsonDocument.Parse($"\"{resultValue}\"").RootElement;
        }

 

        public static List<string> ParseString(string input)
        {
            List<string> result = new List<string>();
            int currentIndex = 0;

            // Bu düzenli ifade ile [] içindekileri yakalayabiliriz.
            var regex = new Regex(@"\[(.*?)\]");

            foreach (Match match in regex.Matches(input))
            {
                // [] içindekilerin öncesi (text kısmı) ekleniyor.
                if (currentIndex < match.Index)
                {
                    result.Add(input.Substring(currentIndex, match.Index - currentIndex));
                }

                // [] içindeki kısmı ekliyoruz.
                result.Add(match.Value);

                // Başlangıç indeksi güncelleniyor.
                currentIndex = match.Index + match.Length;
            }

            // Son kalan text kısmı da ekleniyor.
            if (currentIndex < input.Length)
            {
                result.Add(input.Substring(currentIndex));
            }

            return result;
        }

    }
}
