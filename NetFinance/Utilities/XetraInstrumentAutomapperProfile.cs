using System.Diagnostics.CodeAnalysis;
using NetFinance.Models.Xetra;
using NetFinance.Models.Xetra.Dto;

namespace NetFinance.Utilities;

[ExcludeFromCodeCoverage]
internal class XetraInstrumentAutomapperProfile : AutoMapper.Profile
{
	public XetraInstrumentAutomapperProfile()
	{
		CreateMap<InstrumentItem, Instrument>()
			.ForMember(dest => dest.InstrumentName, opt => opt.MapFrom(src => src.Instrument))
			.ForMember(dest => dest.Symbol, opt => opt.MapFrom(src => src.Mnemonic + ".DE"));
	}
}