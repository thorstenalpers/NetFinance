using System;
using System.Net;
using NetFinance.Exceptions;

namespace NetFinance.Utilities;

internal class YahooCookie
{
	public Cookie? Cookie { get; private set; }

	public bool IsValid()
	{
		return !string.IsNullOrEmpty(Cookie?.Value) &&
			   (Cookie.Expires == DateTime.MinValue || Cookie.Expires > DateTime.Now);
	}

	public void Parse(string? cookieString)
	{
		if (string.IsNullOrWhiteSpace(cookieString))
			throw new ArgumentException("Cookie string cannot be null or empty.", nameof(cookieString));

		var attributes = cookieString.Split(';');
		var cookie = new Cookie();

		var firstAttribute = attributes[0].Trim();
		var nameValueParts = firstAttribute.Split('=', 2);
		if (nameValueParts.Length != 2)
			throw new FormatException("Invalid cookie format: missing name-value pair.");

		cookie.Name = nameValueParts[0].Trim();
		cookie.Value = nameValueParts[1].Trim();

		for (int i = 1; i < attributes.Length; i++)
		{
			var attribute = attributes[i].Trim();
			if (attribute.Contains('='))
			{
				var parts = attribute.Split('=', 2);
				var key = parts[0].Trim();
				var value = parts[1].Trim();

				switch (key.ToLowerInvariant())
				{
					case "domain":
						cookie.Domain = value;
						break;
					case "path":
						cookie.Path = value;
						break;
					case "expires":
						if (DateTime.TryParse(value, out var expires))
							cookie.Expires = expires;
						break;
					case "max-age":
						if (int.TryParse(value, out var maxAge))
							cookie.Expires = DateTime.UtcNow.AddSeconds(maxAge);
						break;
					case "samesite":
						break;
					default:
						break;
				}
			}
			else
			{
				switch (attribute.ToLowerInvariant())
				{
					case "secure":
						cookie.Secure = true;
						break;
					case "httponly":
						cookie.HttpOnly = true;
						break;
					default:
						break;
				}
			}
		}
		Cookie = cookie;
	}

	public override string ToString()
	{
		return Cookie?.ToString() ?? throw new NetFinanceException("Cookie is null");
	}
}