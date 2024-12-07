using System;
using NetFinance.Models.AlphaVantage;
using NetFinance.Utilities;
using NUnit.Framework;

namespace NetFinance.Tests.Utilities;

[TestFixture]
[Category("UnitTests")]
public class HelperTests
{
	[TestCase(null, true)]
	[TestCase(new string[] { }, true)]
	[TestCase(new string[] { "" }, false)]
	[TestCase(new string[] { "Abc", "Abc" }, false)]
	public void IsNullOrEmpty_WithValidEntries_ReturnsResult(string[] array, bool expected)
	{
		var result = Helper.IsNullOrEmpty(array);

		// Assert
		Assert.That(result, Is.EqualTo(expected));
	}

	[TestCase(2024, 11, 1, 1730419200)]
	[TestCase(2011, 11, 27, 1322352000)]
	[TestCase(1972, 11, 27, 91670400)]
	public void ToUnixTimestamp_WithValidEntries_ReturnsResult(int year, int month, int day, long expected)
	{
		// Arrange
		var dateTime = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);

		// Act
		var result = Helper.ToUnixTimestamp(dateTime);

		// Assert
		Assert.That(result, Is.EqualTo(expected));
	}

	[TestCase(1733443200, 2024, 12, 6)]
	[TestCase(1233446400, 2009, 2, 1)]
	public void UnixTimeSecondsToDateTime_WithValidEntries_ReturnsResult(long unixTimeSeconds, int expectedYear, int expectedMonth, int expectedDay)
	{
		// Arrange
		var expected = new DateTime(expectedYear, expectedMonth, expectedDay, 0, 0, 0, DateTimeKind.Utc);

		// Act
		var result = Helper.UnixTimeSecondsToDateTime(unixTimeSeconds);

		// Assert
		Assert.That(result, Is.EqualTo(expected));
	}

	[TestCase(1733443200000, 2024, 12, 6)]
	[TestCase(1233446400000, 2009, 2, 1)]
	public void UnixTimeMillisecondsToDateTime_WithValidEntries_ReturnsResult(long unixTimeMilliseconds, int expectedYear, int expectedMonth, int expectedDay)
	{
		var expected = new DateTime(expectedYear, expectedMonth, expectedDay, 0, 0, 0, DateTimeKind.Utc);

		var result = Helper.UnixTimeMillisecondsToDateTime(unixTimeMilliseconds);

		Assert.That(result, Is.EqualTo(expected));
	}

	[TestCase("100", 100)]
	[TestCase("-100", -100)]
	[TestCase("1,200", 1200)]
	[TestCase("1.1k", 1100)]
	[TestCase("-1.1k", -1100)]
	[TestCase("1.1Mio", 1100000)]
	[TestCase("1.1Mrd", 1100000000)]
	[TestCase("1.1Bio", 1100000000000)]
	[TestCase("1.1Trl", 1100000000000000)]
	[TestCase("---", null)]
	[TestCase(null, null)]
	public void ParseLong_WithValidEntries_ReturnsResult(string numberString, long? expected)
	{
		var result = Helper.ParseLong(numberString);

		Assert.That(result, Is.EqualTo(expected));
	}

	[TestCase("-10", -10)]
	[TestCase("-10.1", -10.1)]
	[TestCase("1,234.12", 1234.12)]
	[TestCase("1.1k", 1100)]
	[TestCase("---", null)]
	[TestCase(null, null)]
	public void ParseDecimal_WithValidEntries_ReturnsResult(string numberString, decimal? expected)
	{
		var result = Helper.ParseDecimal(numberString);

		// Assert
		Assert.That(result, Is.EqualTo(expected));
	}

	[TestCase("Nov 1, 2024", 2024, 11, 1)]
	[TestCase("Nov 01, 2024", 2024, 11, 1)]
	[TestCase("November 1, 2024", 2024, 11, 1)]
	[TestCase("November 01, 2024", 2024, 11, 1)]
	[TestCase("2024-11-1", 2024, 11, 1)]
	[TestCase("2024-11-01", 2024, 11, 1)]
	[TestCase("2024/11/1", 2024, 11, 1)]
	[TestCase("2024/11/01", 2024, 11, 1)]
	public void ParseDate_WithValidEntries_ReturnsResult(string dateString, int expectedYear, int expectedMonth, int expectedDay)
	{
		// Arrange
		var expected = new DateTime(expectedYear, expectedMonth, expectedDay);

		// Act
		var result = Helper.ParseDate(dateString);

		// Assert
		Assert.That(result, Is.EqualTo(expected));
	}

	[TestCase(EInterval.Interval_1Min, "1min")]
	[TestCase(EInterval.Interval_5Min, "5min")]
	[TestCase(EInterval.Interval_15Min, "15min")]
	[TestCase(EInterval.Interval_30Min, "30min")]
	[TestCase(EInterval.Interval_60Min, "60min")]
	public void Description_AllIntervals_ReturnsText(EInterval intervall, string expected)
	{
		// Arrange
		// Act
		var result = intervall.Description();

		// Assert
		Assert.That(result, Is.EqualTo(expected));
	}
}
