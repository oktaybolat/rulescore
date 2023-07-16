using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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
            else if (kural.Contains("!"))
            {
                Console.WriteLine("!");
                var parts = kural.Split("!=");
                var left = parts[0].Trim();
                var right = parts[1].Trim('!');
                right = parts[1].Trim('=');

                Console.WriteLine($"Left: {left}");
                Console.WriteLine($"Right: {right}");

                var leftValue = GetParsedValue(left, gelenJson).GetString();
                var rightValue=right;
                if (right.Contains("[")) {rightValue = GetParsedValue(right, gelenJson).GetString();}

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
                var parts = kural.Split('=');
                var left = parts[0].Trim();
                var right = parts[1].Trim('=');

                Console.WriteLine($"Left: {left}");
                Console.WriteLine($"Right: {right}");

                var leftValue = GetParsedValue(left, gelenJson).GetString();
                var rightValue=right;
                if (right.Contains("[")) {rightValue = GetParsedValue(right, gelenJson).GetString();}

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
                var parts = kural.Split('<');
                var left = parts[0].Trim();
                var right = parts[1].Trim('<');
                right = right.Trim('=');

                Console.WriteLine($"Left: {left}");
                Console.WriteLine($"Right: {right}");

                var leftValue = GetParsedValue(left, gelenJson).GetString();
                var rightValue=right;
                if (right.Contains("[")) {rightValue = GetParsedValue(right, gelenJson).GetString();}

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
                var parts = kural.Split('>');
                var left = parts[0].Trim();
                var right = parts[1].Trim();

                Console.WriteLine($"Left: {left}");
                Console.WriteLine($"Right: {right}");

                var leftValue = GetParsedValue(left, gelenJson).GetString();
                var rightValue=right;
                if (right.Contains("[")) {rightValue = GetParsedValue(right, gelenJson).GetString();}
                
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
            Console.WriteLine($"Jsonin: { gelenJson.ToString()}");
            
            if (value.StartsWith("[DATE()]"))
            {
                var format = value.TrimStart("[DATE(".ToCharArray()).TrimEnd(")]".ToCharArray());
                Console.WriteLine($"Date Format: {format}");
                if (format=="") format="dd/MM/yyyy";
                var date = DateTime.Now.ToString(format);
                return JsonDocument.Parse($"\"{date}\"").RootElement;
            }
            else if (value.StartsWith("[NOW()]"))
            {
                var format = value.TrimStart("[NOW(".ToCharArray()).TrimEnd(")]".ToCharArray());
                Console.WriteLine($"Time Format: {format}");
                if (format=="") format="dd/MM/yyyy hh:mm:ss";
                var now = DateTime.Now.ToString(format);
                return JsonDocument.Parse($"\"{now}\"").RootElement;
            }
            
            else if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                return JsonDocument.Parse(value).RootElement;
            }
            else if (value.Contains("+"))
            {
                var degerler = value.Trim('[', ']').Split('+').Select(d => d.Trim()).ToList();
                var birlesikDeger = string.Join("", degerler.Select(d => GetParsedValue(d, gelenJson).GetString()));
                return JsonDocument.Parse($"\"{birlesikDeger}\"").RootElement;
            }
            else if (value.StartsWith("[") && value.EndsWith("]"))
            {
                var alan = value.Trim('[', ']');
                var alanValueElement = gelenJson.FirstOrDefault(j =>
                {
                    JsonElement nameProp;
                    return j.TryGetProperty("name", out nameProp) && nameProp.GetString() == alan;
                });
                
                Console.WriteLine($"alanvalue: { alanValueElement.ToString()}");
                var alanValue = "";
                if (alanValueElement.ToString()!="")
                { alanValue=alanValueElement.GetProperty("value").GetString();}

                return JsonDocument.Parse($"\"{alanValue}\"").RootElement;

            }



            return JsonDocument.Parse("\"\"").RootElement;
        }
    }
}
