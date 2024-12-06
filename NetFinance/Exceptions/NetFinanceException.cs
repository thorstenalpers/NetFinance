using System;
using System.Diagnostics.CodeAnalysis;

namespace NetFinance.Exceptions;

[ExcludeFromCodeCoverage]
public class NetFinanceException : Exception
{
	public NetFinanceException()
	{
	}

	public NetFinanceException(string message)
		: base(message)
	{
	}

	public NetFinanceException(string message, Exception inner)
		: base(message, inner)
	{
	}
}
