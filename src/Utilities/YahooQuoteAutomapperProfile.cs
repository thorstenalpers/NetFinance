using System.Diagnostics.CodeAnalysis;
using NetFinance.Models.Yahoo;
using NetFinance.Models.Yahoo.Dtos;

namespace NetFinance.Utilities;

[ExcludeFromCodeCoverage]
internal class YahooQuoteAutomapperProfile : AutoMapper.Profile
{
	public YahooQuoteAutomapperProfile()
	{
		CreateMap<QuoteResponse, Quote>()
			.ForMember(dest => dest.FirstTradeDate, opt => opt.MapFrom(src => Helper.UnixTimeMillisecondsToDateTime(src.FirstTradeDateMilliseconds)))
			.ForMember(dest => dest.RegularMarketTime, opt => opt.MapFrom(src => Helper.UnixTimeSecondsToDateTime(src.RegularMarketTime)))
			.ForMember(dest => dest.PostMarketTime, opt => opt.MapFrom(src => Helper.UnixTimeSecondsToDateTime(src.PostMarketTime)))
			.ForMember(dest => dest.DividendDate, opt => opt.MapFrom(src => Helper.UnixTimeSecondsToDateTime(src.DividendDate)))
			.ForMember(dest => dest.EarningsDate, opt => opt.MapFrom(src => Helper.UnixTimeSecondsToDateTime(src.EarningsTimestamp)))
			.ForMember(dest => dest.EarningsDateStart, opt => opt.MapFrom(src => Helper.UnixTimeSecondsToDateTime(src.EarningsTimestampStart)))
			.ForMember(dest => dest.EarningsDateEnd, opt => opt.MapFrom(src => Helper.UnixTimeSecondsToDateTime(src.EarningsTimestampEnd)))
			.ForMember(dest => dest.EarningsCallDateStart, opt => opt.MapFrom(src => Helper.UnixTimeSecondsToDateTime(src.EarningsCallTimestampStart)))
			.ForMember(dest => dest.EarningsCallDateEnd, opt => opt.MapFrom(src => Helper.UnixTimeSecondsToDateTime(src.EarningsCallTimestampEnd)));
	}
}
