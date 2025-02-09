using Bogus;
using eCommerce.MVC.Context;
using eCommerce.MVC.DTOs;
using eCommerce.MVC.Models;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using System;

namespace eCommerce.MVC.Controllers;
public class HomeController : Controller
{
    //private static List<Product> products = new(); //Listenin özelliklerini Product Model'inden alıyoruz.
    //private static List<ShoppingCart> shoppingCarts = new();
    
	private readonly ApplicationDbContext _context = new();

    public IActionResult Index()
    {
		var products = _context.Products.ToList(); //Db bağlantısı

        return View(products);
    }

    public IActionResult Contact()
    {
        return View();
    }

    public IActionResult ShoppingCart(int index)
    {
		var shoppingCarts = _context.ShoppingCarts.ToList(); //Db bağlantısı

        decimal total = 0;
        foreach(var item in shoppingCarts)
        {
            total += item.Price;
        }

        ViewBag.Total = total; //Kısa vadede veri taşımak için kullanılıyor.

        //@TempData["Total"] = total; Uzun vadede veri taşımak için kullanılır

		return View(shoppingCarts);
	}

    [HttpGet]
    public IActionResult AddShoppingCart(int id)
    {
        Product product = _context.Products.First(x => x.Id == id);

        ShoppingCart shoppingCart = new()
        {
            Name = product.Name,
            ImageUrl = product.ImageUrl,
            Price = product.Price
        };

        _context.Add(shoppingCart); //ShoppingCart Model'ine ekleme sağlıyor.
		_context.SaveChanges();

        return RedirectToAction("Index", "Home"); //Yönlendirme sağlıyor
    }

	[HttpGet]
	public IActionResult RemoveFromCart(int id)
	{
		var shoppingCart = _context.ShoppingCarts.FirstOrDefault(x => x.Id == id);
		if (shoppingCart != null)
		{
			_context.ShoppingCarts.Remove(shoppingCart);
			_context.SaveChanges();
		}

		return RedirectToAction("ShoppingCart");
	}

	[HttpPost]
    public IActionResult Pay(PayDto payDto)
    {
		/* IyzıPay Ödeme Entegrasyonu */

		/*
		 * Kredi kartı bilgileri
		 * Toplam tutar
		 * Sepetteki ürün bilgisi
		 * Açık adres
		 * Fatura adresi
		 */

		var shoppingCarts = _context.ShoppingCarts.ToList();

		decimal total = 0;
		foreach (var item in shoppingCarts)
		{
			total += item.Price;
		}

		/* API Key ve Secret Key'i İyzico sandbox'a girip kayıt olduktan sonra firma ayarları kısmından elde edebiliyoruz. */
		Options options = new Options();
		options.ApiKey = "sandbox-AEpYKksX5pXbIP9uugMqNB7MmPrdmIfo";
		options.SecretKey = "sandbox-rcGBFiHBgiKXjN3ndDtuL6XPxYEFpGUE";
		options.BaseUrl = "https://sandbox-api.iyzipay.com";

		/*  */
		CreatePaymentRequest request = new CreatePaymentRequest();
		request.Locale = Locale.TR.ToString(); //Lokasyon
		request.ConversationId = Guid.NewGuid().ToString(); //Unic bir Id olmalı
		request.Price = total.ToString().Replace(",", "."); //Ödeme tutarı
		request.PaidPrice = total.ToString().Replace(",", "."); //Komisyon miktarı
		request.Currency = Currency.TRY.ToString(); //Para formatı
		request.Installment = 1; //Taksit seçeneği
		request.BasketId = Guid.NewGuid().ToString(); //Sipariş numarası / Sepet numarası (Unic olmalı)
		request.PaymentChannel = PaymentChannel.WEB.ToString(); //Ödeme kanalları
		request.PaymentGroup = PaymentGroup.PRODUCT.ToString();

		/* Kart Bilgileri */
		PaymentCard paymentCard = new PaymentCard();
		paymentCard.CardHolderName = payDto.Owner; //Kart sahibinin ismi
		paymentCard.CardNumber = payDto.CardNumber; //Kart numarası
		paymentCard.ExpireMonth = payDto.ExpiryDate.Split("/")[0]; //Son kullanma tarihleri
		paymentCard.ExpireYear = payDto.ExpiryDate.Split("/")[1];
		paymentCard.Cvc = payDto.CVC; //CVC numarası
		paymentCard.RegisterCard = 0; //Kartın sisteme kayıt edilmesini sağlıyor. (1 ise kaydet, değilse kaydetme)
		request.PaymentCard = paymentCard;

		/* Alıcı Bilgileri */
		Buyer buyer = new Buyer();
		buyer.Id = "BY789";
		buyer.Name = "John";
		buyer.Surname = "Doe";
		buyer.GsmNumber = "+905350000000";
		buyer.Email = "email@email.com";
		buyer.IdentityNumber = "74300864791";
		buyer.LastLoginDate = "2015-10-05 12:43:35"; //(Zorunlu değil)
		buyer.RegistrationDate = "2013-04-21 15:12:09";
		buyer.RegistrationAddress = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
		buyer.Ip = "85.34.78.112"; //(Zorunlu değil)
		buyer.City = "Istanbul";
		buyer.Country = "Turkey";
		buyer.ZipCode = "34732";
		request.Buyer = buyer;

		/* Teslim Adresi */
		Address shippingAddress = new Address();
		shippingAddress.ContactName = "Jane Doe";
		shippingAddress.City = "Istanbul";
		shippingAddress.Country = "Turkey";
		shippingAddress.Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
		shippingAddress.ZipCode = "34742";
		request.ShippingAddress = shippingAddress;

		/* Fatura Adresi */
		Address billingAddress = new Address();
		billingAddress.ContactName = "Jane Doe";
		billingAddress.City = "Istanbul";
		billingAddress.Country = "Turkey";
		billingAddress.Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
		billingAddress.ZipCode = "34742";
		request.BillingAddress = billingAddress;

		/* Ürün Bilgileri */

		/*
		 * Ürün bilgilerindeki 3 adet 'Price' ın toplamı yukarıda verilen ödeme tutarının toplamı kadar olmalı.
		 * Aksi takdirde hata vermekte.
		 */

		List<BasketItem> basketItems = new List<BasketItem>();

		foreach(var card in shoppingCarts)
		{
			BasketItem basketItem = new BasketItem();
			basketItem.Id = Guid.NewGuid().ToString();
			basketItem.Name = card.Name;
			basketItem.ItemType = BasketItemType.PHYSICAL.ToString();
			basketItem.Price = card.Price.ToString().Replace(",",".");
			basketItem.Category1 = "TS Ürün";
			basketItems.Add(basketItem);
		}

		request.BasketItems = basketItems;

		/* En son gönderilecek sınıf */
		/* ERROR MESSAGE 50001 */
		Payment payment = Payment.Create(request, options);

		
		if(payment.Status == "success")
		{
			_context.RemoveRange(shoppingCarts);
			_context.SaveChanges();
			ViewBag.Total = 0;
			return RedirectToAction("ShoppingCart");
		}

		/* Ödeme yönteminde hata oluşursa sistem tarafından gönderilen hata mesajını yakaladığımız yer */
		TempData["Error"] = payment.ErrorMessage;

		return RedirectToAction("ShoppingCart");
		
    }

	public IActionResult Products()
	{
		var products = _context.Products.ToList();

		return View(products);
	}

	[HttpPost]
	public IActionResult CreateProduct(CreateProductDto request)
	{
		/* Dosya formatını ayıklama ve ismini değiştirme */
		string format = request.File!.FileName.Substring(request.File!.FileName.LastIndexOf("."));
		string fileName = Guid.NewGuid().ToString() + format;
		/* Dosya oluşturma işlemi */
		using (var stream = System.IO.File.Create("wwwroot/images/" + request.File!.FileName))
		{
			request.File.CopyTo(stream);
		}

		Product product = new()
		{
			Name = request.Name,
			Description = request.Description,
			ImageUrl = request.File.FileName,
			Price = request.Price
		};

		_context.Add(product);
		_context.SaveChanges();

		return RedirectToAction("Products");
	}

	[HttpGet]
	public IActionResult Delete(int id)
	{
		Product? product = _context.Products.FirstOrDefault(p => p.Id == id);
		if (product == null)
		{
			return NotFound(); //Ürün bulunamadı.
		}

		var imagePath = Path.Combine("wwwroot/images", product.ImageUrl);
		if (System.IO.File.Exists(imagePath))
		{
			System.IO.File.Delete(imagePath);
		}

		_context.Products.Remove(product);
		_context.SaveChanges();

		return RedirectToAction("Products");
	}

	public IActionResult MessageTo(MessageTo messageTo)
	{
		if (ModelState.IsValid)
		{
			_context.MessageTos.Add(messageTo);
			_context.SaveChanges();

			// Mesaj başarıyla kaydedildikten sonra yönlendirme veya bir mesaj gösterin
			return RedirectToAction("Index"); // Başarılı bir mesaj göstermek için başka bir action'a yönlendirebilirsiniz
		}

		return View(messageTo);
	}
}