namespace eCommerce.MVC.DTOs;

public sealed class PayDto
{
	public string Owner {  get; set; } = string.Empty;
	public string CardNumber { get; set; } = string.Empty;
	public string ExpiryDate { get; set; } = string.Empty;
	public string CVC {  get; set; } = string.Empty;
}
