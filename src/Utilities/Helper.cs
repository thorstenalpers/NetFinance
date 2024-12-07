using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using NetFinance.Models.AlphaVantage;

namespace NetFinance.Utilities;

public static class Helper
{

	public static readonly List<string> FirefoxVersions = [ "133.0",
										 "132.0", "132.0.1", "132.0.2",
										 "131.0", "131.0.1", "131.0.2",
										 "130.0", "130.0.1",
										 "129.0", "129.0.1", "129.0.2",
										 "128.0", "128.0.2", "128.0.3","128.1.0", "128.2.0", "128.3.0", "128.3.1"];

	public static readonly List<string> OperatingSystems = [

		"Windows NT 10.0; Win64; x64",
			"Windows NT 6.1; WOW64",
			"Macintosh; Intel Mac OS X 10_15_7",
			"Macintosh; Intel Mac OS X 10_14_6",
			"Linux x86_64",
			"Linux i686"
	];

	public static bool IsNullOrEmpty<T>(this IEnumerable<T> list)
	{
		return list?.Any() != true;
	}
	public static long? ToUnixTimestamp(DateTime? dateTime)
	{
		if (dateTime == null)
		{
			return null;
		}

		var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		return (long)(dateTime.Value - unixEpoch).TotalSeconds;
	}

	public static DateTime? UnixTimeSecondsToDateTime(long? unixTimeSeconds)
	{
		if (unixTimeSeconds == null)
		{
			return null;
		}

		var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		return epoch.AddSeconds(unixTimeSeconds.Value).ToUniversalTime();
	}
	public static DateTime? UnixTimeMillisecondsToDateTime(long? unixTimeMilliseconds)
	{
		if (unixTimeMilliseconds == null) return null;
		var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		return epoch.AddMilliseconds(unixTimeMilliseconds.Value).ToUniversalTime();
	}

	public static long? ParseLong(string? numberString)
	{
		var cleanedNumber = numberString?.Replace(",", "");
		if (string.IsNullOrWhiteSpace(cleanedNumber) || cleanedNumber.All(e => e == '-')) return null;

		if (long.TryParse(cleanedNumber, out long result))
		{
			return result;
		}
		result = (long)ParseWithMultiplier(cleanedNumber);
		return result;
	}

	public static decimal? ParseDecimal(string? numberString)
	{
		var cleanedNumber = numberString?.Replace(",", "");
		if (string.IsNullOrWhiteSpace(cleanedNumber) || cleanedNumber.All(e => e == '-')) return null;
		if (decimal.TryParse(cleanedNumber, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result))
		{
			return result;
		}

		result = ParseWithMultiplier(cleanedNumber);
		return result;
	}

	private static decimal ParseWithMultiplier(string? cleanedNumber)
	{
		// otherwise has numer format such as 100.00Mio?
		var match = new Regex("([0-9.,-]+)([A-Za-z]+)").Match(cleanedNumber);
		if (match.Groups.Count <= 2 || string.IsNullOrEmpty(match.Groups[2].Value))
		{
			throw new FormatException($"Unknown format of {cleanedNumber}");
		}
		if (!decimal.TryParse(match.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
		{
			throw new FormatException($"Unknown format of {cleanedNumber}");
		}
		var mulStr = match.Groups[2].Value;
		if (mulStr is "Trl." or "Trl" or "T")
		{
			return result * 1000000000000000;
		}
		else if (mulStr is "Bio." or "Bio" or "B")
		{
			return result * 1000000000000;
		}
		else if (mulStr is "Mrd." or "Mrd")
		{
			return result * 1000000000;
		}
		else if (mulStr is "Mio." or "Mio" or "M")
		{
			return result * 1000000;
		}
		else if (mulStr is "k" or "K")
		{
			return result * 1000;
		}
		else
		{
			throw new FormatException($"Unknown multiplikator={mulStr} of {cleanedNumber}");
		}
	}

	public static DateTime? ParseDate(string? dateString)
	{
		if (string.IsNullOrWhiteSpace(dateString)) return null;

		// List of possible formats
		var formats = new[]
		{
			"MMM d, yyyy",    // "Nov 1, 2024"
			"MMM dd, yyyy",   // "Nov 01, 2024"
            "MMMM d, yyyy",  // "November 1, 2024"
            "MMMM dd, yyyy",  // "November 01, 2024"
            "yyyy-MM-d",     // "2024-11-1"
            "yyyy-MM-dd",     // "2024-11-01"
            "yyyy/MM/d",     // "2024/11/1"
            "yyyy/MM/dd",     // "2024/11/01"
        };

		// Try parsing the date using each format
		foreach (var format in formats)
		{
			if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
			{
				return result;
			}
		}
		throw new FormatException($"Invalid date format {dateString}");
	}

	public static string Description(this EInterval value)
	{
		var field = value.GetType().GetField(value.ToString());
		var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
		return attribute == null ? value.ToString() : attribute.Description;
	}

	public static string CreateRandomUserAgent(Random? random = null)
	{
		random ??= new Random();

		var firefoxUserAgents = new List<string>();

		for (int i = 0; i < 1000; i++)
		{
			string firefoxVersion = FirefoxVersions[random.Next(FirefoxVersions.Count)];
			string operatingSystem = OperatingSystems[random.Next(OperatingSystems.Count)];
			string userAgent = $"Mozilla/5.0 ({operatingSystem}; rv:{firefoxVersion}) Gecko/20100101 Firefox/{firefoxVersion}";

			firefoxUserAgents.Add(userAgent);
		}
		var index = random.Next(firefoxUserAgents.Count);
		return firefoxUserAgents[index];
	}

	public static void AddCookiesToRequest(this HttpRequestMessage requestMessage, CookieCollection cookieCollection)
	{
		if (cookieCollection != null && requestMessage != null)
		{
			var cookieHeader = ConvertCookiesToHeader(cookieCollection);
			requestMessage.Headers.Add("Cookie", cookieHeader);
		}
	}

	public static bool AreAllFieldsNull<T>(T obj)
	{
		if (obj == null)
		{
			throw new ArgumentNullException(nameof(obj), "Object cannot be null.");
		}
		var fields = obj.GetType().GetFields(
			System.Reflection.BindingFlags.Instance |
			System.Reflection.BindingFlags.Public |
			System.Reflection.BindingFlags.NonPublic).ToList();
		return fields.All(field => field.GetValue(obj) == null);
	}

	private static string ConvertCookiesToHeader(CookieCollection cookieCollection)
	{
		List<string> cookieStrings = new();
		foreach (Cookie cookie in cookieCollection)
		{
			cookieStrings.Add($"{cookie.Name}={cookie.Value}");
		}
		return string.Join(";", cookieStrings);
	}
}