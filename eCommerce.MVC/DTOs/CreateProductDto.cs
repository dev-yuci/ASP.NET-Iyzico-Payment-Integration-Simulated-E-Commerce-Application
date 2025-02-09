namespace eCommerce.MVC.DTOs;

public sealed class CreateProductDto
{
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public decimal Price { get; set; }
	public IFormFile? File { get; set; }
}
