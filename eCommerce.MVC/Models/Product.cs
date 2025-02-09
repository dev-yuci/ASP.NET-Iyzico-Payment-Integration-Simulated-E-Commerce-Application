namespace eCommerce.MVC.Models;

public sealed class Product
{
    public int Id { get; set; } 
    public string ImageUrl { get; set; } = string.Empty; //Doldurulması kesin yapılarda empty kullanılır.
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; } //Default da sıfır gelir zaten
}
