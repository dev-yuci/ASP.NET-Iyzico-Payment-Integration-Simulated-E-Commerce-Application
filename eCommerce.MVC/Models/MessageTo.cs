namespace eCommerce.MVC.Models;

public sealed class MessageTo
{
	public int Id { get; set; }
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Message { get; set; } = string.Empty;
}
