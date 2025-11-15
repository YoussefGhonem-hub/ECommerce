namespace ECommerce.Application.UserAddresses.Queries;

public class MyAddressDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }

    public string? FullName { get; set; }

    public Guid CountryId { get; set; }
    public string CountryNameEn { get; set; } = string.Empty;
    public string CountryNameAr { get; set; } = string.Empty;

    public Guid CityId { get; set; }
    public string CityNameEn { get; set; } = string.Empty;
    public string CityNameAr { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
    public string? HouseNo { get; set; }

    public bool IsDefault { get; set; }
}