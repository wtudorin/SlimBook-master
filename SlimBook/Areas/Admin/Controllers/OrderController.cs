﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SlimBook.DataAccess.Repository.IRepository;
using SlimBook.Models;
using SlimBook.Models.ViewModels;
using SlimBook.Utility;
using Stripe;

namespace SlimBook.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize]
	public class OrderController : Controller
	{
		public readonly IUnitOfWork _unitOfWork;

		[BindProperty]
		public OrderDetailsVM OrderVM { get; set; }

		public OrderController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Details(int id)
		{
			OrderVM = new OrderDetailsVM()
			{
				OrderHeader = _unitOfWork.OrderHeaderRepository.GetFirstOrDefault(u => u.Id == id, includeProperties: "ApplicationUser"),
				OrderDetails = _unitOfWork.OrderDetailsRepository.GetAll(u => u.OrderId == id, includeProperties: "Product"),
				IsInternalUser = User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee)
			};

			return View(OrderVM);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[ActionName("Details")]
		public IActionResult DetailsPOST(string stripeToken)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeaderRepository.GetFirstOrDefault(
				o => o.Id == OrderVM.OrderHeader.Id, includeProperties: SD.IncludibleObjects.ApplicationUser);
			if (stripeToken != null)
			{
				var options = new ChargeCreateOptions()
				{
					Amount = Convert.ToInt32(orderHeader.OrderTotal * 100),
					Currency = "usd",
					Description = "Order ID : " + orderHeader.Id,
					Source = stripeToken
				};
				var service = new ChargeService();
				Charge charge = service.Create(options);
				if (charge.Id == null)
				{
					orderHeader.PaymentStatus = SD.PaymentStatusRejected;
				}
				else
				{
					orderHeader.TransactionId = charge.Id;
				}

				if (charge.Status.ToLower() == "succeeded")
				{
					orderHeader.PaymentStatus = SD.PaymentStatusApproved;
					orderHeader.PaymentDate = DateTime.Now;
					orderHeader.PaymentDueDate = DateTime.MinValue;
				}
				_unitOfWork.Save();
			}

			return RedirectToAction("Details", "Order", new { id = orderHeader.Id });
		}

		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		public IActionResult StartProcessing(int id)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeaderRepository.GetFirstOrDefault(u => u.Id == id);
			orderHeader.OrderStatus = SD.StatusInProcess;
			_unitOfWork.Save();
			return RedirectToAction("Index");
		}

		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		public IActionResult ShipOrder(int id)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeaderRepository.GetFirstOrDefault(u => u.Id == id);
			orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
			orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
			orderHeader.OrderStatus = SD.StatusShipped;
			orderHeader.ShippingDate = DateTime.Now;
			_unitOfWork.Save();
			return RedirectToAction("Index");
		}

		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		public IActionResult CancelOrder(int id)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeaderRepository.GetFirstOrDefault(u => u.Id == id);
			if (orderHeader.PaymentStatus == SD.PaymentStatusApproved && orderHeader.TransactionId != null)
			{
				var options = new RefundCreateOptions()
				{
					Amount = Convert.ToInt32(orderHeader.OrderTotal * 100),
					Reason = RefundReasons.RequestedByCustomer,
					Charge = orderHeader.TransactionId
				};
				var service = new RefundService();
				Refund refund = service.Create(options);

				orderHeader.OrderStatus = SD.StatusRefunded;
				orderHeader.PaymentStatus = SD.StatusRefunded;
			}
			else
			{
				orderHeader.OrderStatus = SD.StatusCancelled;
				orderHeader.PaymentStatus = SD.StatusCancelled;
			}

			_unitOfWork.Save();
			return RedirectToAction("Index");
		}

		#region API Calls

		[HttpGet]
		public IActionResult GetOrderList(string status)
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

			IEnumerable<OrderHeader> orderHeaderList;
			orderHeaderList = _unitOfWork.OrderHeaderRepository.GetAll(includeProperties: "ApplicationUser");

			if (!User.IsInRole(SD.Role_Admin) && !User.IsInRole(SD.Role_Employee))
			{
				orderHeaderList = orderHeaderList.Where(o => o.ApplicationUserId == claim.Value);
			}

			switch (status)
			{
				case "pending":
					orderHeaderList = orderHeaderList.Where(o => o.PaymentStatus == SD.PaymentStatusDelayedPayment);
					break;

				case "inprocess":
					orderHeaderList = orderHeaderList.Where(o => o.OrderStatus == SD.StatusApproved ||
																	o.OrderStatus == SD.StatusInProcess ||
																	o.OrderStatus == SD.StatusPending);
					break;

				case "completed":
					orderHeaderList = orderHeaderList.Where(o => o.OrderStatus == SD.StatusShipped);
					break;

				case "rejected":
					orderHeaderList = orderHeaderList.Where(o => o.OrderStatus == SD.StatusCancelled ||
																	o.OrderStatus == SD.StatusRefunded ||
																	o.PaymentStatus == SD.PaymentStatusRejected);
					break;

				default:
					break;
			}

			return Json(new { data = orderHeaderList });
		}

		#endregion API Calls
	}
}