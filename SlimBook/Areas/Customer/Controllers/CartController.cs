using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SlimBook.DataAccess.Repository.IRepository;
using SlimBook.Models;
using SlimBook.Models.ViewModels;
using SlimBook.Utility;
using Stripe;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace SlimBook.Areas.Customer.Controllers
{
	[Area("Customer")]
	public class CartController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IEmailSender _emailSender;
		private readonly UserManager<IdentityUser> _userManager;
		private readonly ILogger<CartController> _logger;

		private TwilioSettings _twilioOptions { get; set; }

		[BindProperty]
		public ShoppingCartVM shoppingCartVM { get; set; }

		public CartController(IUnitOfWork unitOfWork,
			IEmailSender emailSender,
			UserManager<IdentityUser> userManager,
			IOptions<TwilioSettings> twilioOptions,
			ILogger<CartController> logger
			)
		{
			_unitOfWork = unitOfWork;
			_emailSender = emailSender;
			_userManager = userManager;
			_twilioOptions = twilioOptions.Value;
			_logger = logger;
		}

		[HttpPost]
		[ActionName("Index")]
		public async Task<IActionResult> IndexPOST()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
			var user = _unitOfWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);

			if (user == null)
			{
				ModelState.AddModelError(string.Empty, "Verification email is empty!");
			}

			var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
			code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
			var callbackUrl = Url.Page(
				"/Account/ConfirmEmail",
				pageHandler: null,
				values: new { area = "Identity", userId = user.Id, code = code },
				protocol: Request.Scheme);

			await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
				$"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
			ModelState.AddModelError(string.Empty, "Verification email sent. Please check your email.");
			return RedirectToAction("Index");
		}

		public IActionResult Index()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

			shoppingCartVM = new ShoppingCartVM()
			{
				OrderHeader = new Models.OrderHeader(),
				ListCart = _unitOfWork.ShoppingCartRepository.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product")
			};

			shoppingCartVM.OrderHeader.OrderTotal = 0;
			shoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser
				.GetFirstOrDefault(u => u.Id == claim.Value,
				includeProperties: "Company");

			foreach (var list in shoppingCartVM.ListCart)
			{
				list.Price = SD.GetPriceBasedOnQuantity(list.Count,
					list.Product.Price,
					list.Product.Price50,
					list.Product.Price100);
				shoppingCartVM.OrderHeader.OrderTotal += (list.Price * list.Count);
				list.Product.Description = SD.ConvertToRawHtml(list.Product.Description);
				if (list.Product.Description.Length > 100)
				{
					list.Product.Description = list.Product.Description.Substring(0, 99) + "...";
				}
			}

			return View(shoppingCartVM);
		}

		public IActionResult Plus(int cartID)
		{
			var cart = _unitOfWork.ShoppingCartRepository.GetFirstOrDefault(c => c.Id == cartID, includeProperties: "Product");
			cart.Count++;
			cart.Price = SD.GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));
		}

		public IActionResult Minus(int cartID)
		{
			var cart = _unitOfWork.ShoppingCartRepository.GetFirstOrDefault(c => c.Id == cartID, includeProperties: "Product");
			if (cart.Count == 1)
			{
				var cnt = _unitOfWork.ShoppingCartRepository.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
				_unitOfWork.ShoppingCartRepository.Remove(cart);
				_unitOfWork.Save();
				HttpContext.Session.SetInt32(SD.SessionShoppingCart, cnt - 1);
			}
			else
			{
				cart.Count--;
				cart.Price = SD.GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
				_unitOfWork.Save();
			}
			string redirectUrl = Url.Action("Index");
			return Json(new { redirectUrl });
			//            return RedirectToAction(nameof(Index));
		}

		public IActionResult Remove(int cartID)
		{
			var cart = _unitOfWork.ShoppingCartRepository.GetFirstOrDefault(c => c.Id == cartID, includeProperties: "Product");
			var cnt = _unitOfWork.ShoppingCartRepository.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
			_unitOfWork.ShoppingCartRepository.Remove(cart);
			_unitOfWork.Save();
			HttpContext.Session.SetInt32(SD.SessionShoppingCart, cnt - 1);
			return RedirectToAction(nameof(Index));
		}

		[HttpDelete]
		public IActionResult Delete(int cartId)
		{
			var cart = _unitOfWork.ShoppingCartRepository.GetFirstOrDefault(c => c.Id == cartId, includeProperties: "Product");
			if (cart == null)
			{
				return Json(new { success = false, message = "Error while deleting" });
			}
			var cnt = _unitOfWork.ShoppingCartRepository.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
			_unitOfWork.ShoppingCartRepository.Remove(cart);
			_unitOfWork.Save();
			HttpContext.Session.SetInt32(SD.SessionShoppingCart, cnt - 1);
			return Json(new { success = true, message = "Delete Successful" });
		}

		public IActionResult Summary()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

			shoppingCartVM = new ShoppingCartVM()
			{
				OrderHeader = new Models.OrderHeader(),
				ListCart = _unitOfWork.ShoppingCartRepository.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product")
			};
			shoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork
														.ApplicationUser
														.GetFirstOrDefault(c => c.Id == claim.Value,
														includeProperties: "Company");

			foreach (var list in shoppingCartVM.ListCart)
			{
				list.Price = SD.GetPriceBasedOnQuantity(list.Count,
					list.Product.Price,
					list.Product.Price50,
					list.Product.Price100);
				shoppingCartVM.OrderHeader.OrderTotal += (list.Price * list.Count);
			}
			shoppingCartVM.OrderHeader.Name = shoppingCartVM.OrderHeader.ApplicationUser.Name;
			shoppingCartVM.OrderHeader.PhoneNumber = shoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
			shoppingCartVM.OrderHeader.City = shoppingCartVM.OrderHeader.ApplicationUser.City;
			shoppingCartVM.OrderHeader.PostalCode = shoppingCartVM.OrderHeader.ApplicationUser.PostalCode;
			shoppingCartVM.OrderHeader.StreetAddress = shoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
			shoppingCartVM.OrderHeader.State = shoppingCartVM.OrderHeader.ApplicationUser.State;
			return View(shoppingCartVM);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[ActionName("Summary")]
		public IActionResult SummaryPOST(string stripeToken)
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
			shoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork
														.ApplicationUser
														.GetFirstOrDefault(c => c.Id == claim.Value,
														includeProperties: "Company");
			shoppingCartVM.ListCart = _unitOfWork.ShoppingCartRepository.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product");
			shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
			shoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
			shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
			shoppingCartVM.OrderHeader.ApplicationUserId = claim.Value;
			shoppingCartVM.OrderHeader.OrderDate = DateTime.Now;

			_unitOfWork.OrderHeaderRepository.Add(shoppingCartVM.OrderHeader);
			_unitOfWork.Save();

			foreach (var item in shoppingCartVM.ListCart)

			{
				OrderDetails ordersDetail = new OrderDetails()
				{
					ProductId = item.ProductId,
					OrderId = shoppingCartVM.OrderHeader.Id,
					Price = SD.GetPriceBasedOnQuantity(item.Count, item.Product.Price, item.Product.Price50, item.Product.Price100),
					Count = item.Count
				};
				shoppingCartVM.OrderHeader.OrderTotal += ordersDetail.Count * ordersDetail.Price;
				_unitOfWork.OrderDetailsRepository.Add(ordersDetail);
			}
			_unitOfWork.ShoppingCartRepository.RemoveRange(shoppingCartVM.ListCart);
			_unitOfWork.Save();
			HttpContext.Session.SetInt32(SD.SessionShoppingCart, 0);

			if (stripeToken == null)
			{
				// Authorizsed user - order will be created with delayed payment
				shoppingCartVM.OrderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
				shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
				shoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
			}
			else
			{
				//Process the payment
				var options = new ChargeCreateOptions()
				{
					Amount = Convert.ToInt32(shoppingCartVM.OrderHeader.OrderTotal * 100),
					Currency = "usd",
					Description = "Order ID : " + shoppingCartVM.OrderHeader.Id,
					Source = stripeToken
				};
				var service = new ChargeService();
				Charge charge = service.Create(options);
				if (charge.BalanceTransactionId == null)
				{
					shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
				}
				else
				{
					shoppingCartVM.OrderHeader.TransactionId = charge.BalanceTransactionId;
				}

				if (charge.Status.ToLower() == "succeeded")
				{
					shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;
					shoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
					shoppingCartVM.OrderHeader.PaymentDate = DateTime.Now;
				}
			}
			_unitOfWork.Save();

			return RedirectToAction("OrderConfirmation", "Cart", new { id = shoppingCartVM.OrderHeader.Id });
		}

		public IActionResult OrderConfirmation(int id)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeaderRepository.GetFirstOrDefault(o => o.Id == id, includeProperties: SD.IncludibleObjects.ApplicationUser);
			TwilioClient.Init(_twilioOptions.AccountSid, _twilioOptions.AuthToken);
			try
			{
				//var message = MessageResource.Create(
				//	body: $"Dear {orderHeader.Name}, a new order has been placed on Slim Book.{Environment.NewLine}Your Order Id is: {id}{Environment.NewLine}The items will be shipped to:{Environment.NewLine}{orderHeader.StreetAddress}, {orderHeader.City} {orderHeader.State} {orderHeader.PostalCode}",
				//	from: new Twilio.Types.PhoneNumber(_twilioOptions.PhoneNumber),
				//	to: new Twilio.Types.PhoneNumber(orderHeader.PhoneNumber)
				//	);
				var message = MessageResource.Create(
					body: $"Dear {orderHeader.Name}, a new order has been placed on Slim Book.{Environment.NewLine}Your Order Id is: {id}{Environment.NewLine}The items will be shipped to:{Environment.NewLine}{orderHeader.StreetAddress}, {orderHeader.City} {orderHeader.State} {orderHeader.PostalCode}",
					//					body: $"Your 1 code is 2",
					from: new Twilio.Types.PhoneNumber("whatsapp:+14155238886"),
					//					from: new Twilio.Types.PhoneNumber(SD.WhatsAppPrefix + _twilioOptions.PhoneNumber),
					to: new Twilio.Types.PhoneNumber(SD.WhatsAppPrefix + orderHeader.PhoneNumber)
					);
				_logger.Log(LogLevel.Information, $"WhatsApp message sent. Result: {JsonConvert.SerializeObject(message)}");
			}
			catch (Exception ex)
			{
				_logger.Log(LogLevel.Error, ex.Message);
			}
			return View(id);
		}
	}
}