namespace NetFinance.Models.Yahoo;

public record Profile
{
	public string? Adress { get; set; }
	public string? Phone { get; set; }
	public string? Website { get; set; }
	public string? Sector { get; set; }
	public string? Industry { get; set; }
	public long? CntEmployees { get; set; }
	public string? CorporateGovernance { get; set; }
	public string? Description { get; set; }
}
