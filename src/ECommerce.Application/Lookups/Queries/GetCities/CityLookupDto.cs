namespace ECommerce.Application.Lookups.Queries.GetCities;

public class CityLookupDto
{
    public Guid Id { get; set; }
    public Guid CountryId { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
}