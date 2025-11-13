namespace ECommerce.Shared.Dtos;
public class BaseFilterDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Sort { get; set; }
    public bool Descending { get; set; } = true;
}
