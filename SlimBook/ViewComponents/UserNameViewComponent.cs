﻿using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SlimBook.DataAccess.Repository.IRepository;

namespace SlimBook.ViewComponents
{
	public class UserNameViewComponent : ViewComponent
	{
		private readonly IUnitOfWork _unitOfWork;

		public UserNameViewComponent(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<IViewComponentResult> InvokeAsync()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
			var user = _unitOfWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);

			return View(user);
		}
	}
}