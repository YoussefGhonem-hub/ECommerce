namespace ECommerce.Application.Lookups.Queries.GetCountries;

public class CountryLookupDto
{
    public Guid Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
}