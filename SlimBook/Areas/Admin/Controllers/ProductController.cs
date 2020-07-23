using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using SlimBook.DataAccess.Repository.IRepository;
using SlimBook.Models;
using SlimBook.Models.ViewModels;
using SlimBook.Utility;

namespace SlimBook.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = SD.Role_Admin)]
	public class ProductController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IWebHostEnvironment _hostEnvironment;

		public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment hostEnvironment)
		{
			_unitOfWork = unitOfWork;
			_hostEnvironment = hostEnvironment;
		}

		public IActionResult Index()
		{
			return View();
		}

		[HttpGet]
		public async Task<IActionResult> Upsert(int? id)
		{
			IEnumerable<Category> catList = await _unitOfWork.Category.GetAllAsync();

			ProductVM productVM = new ProductVM()
			{
				Product = new Product(),
				CategoryList = catList.Select(c => new SelectListItem
				{
					Text = c.Name,
					Value = c.Id.ToString()
				}),
				CoverTypeList = _unitOfWork.CoverType.GetAll().Select(c => new SelectListItem
				{
					Text = c.Name,
					Value = c.Id.ToString()
				})
			};

			if (id == null)
			{
				return View(productVM);
			}

			productVM.Product = _unitOfWork.Product.Get(id.GetValueOrDefault());
			if (productVM.Product == null)
			{
				return NotFound();
			}
			return View(productVM);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Upsert(ProductVM productVM)
		{
			if (ModelState.IsValid)
			{
				string webRootPath = _hostEnvironment.WebRootPath;
				var files = HttpContext.Request.Form.Files;
				if (files.Count > 0)
				{
					string filename = Guid.NewGuid().ToString();
					var uploads = Path.Combine(webRootPath, @"images\products");
					var extension = Path.GetExtension(files[0].FileName);

					if (productVM.Product.ImageUrl != null)
					{
						var imagePath = Path.Combine(webRootPath, productVM.Product.ImageUrl.TrimStart('\\'));
						if (System.IO.File.Exists(imagePath))
						{
							System.IO.File.Delete(imagePath);
						}
					}
					using (var fileStream = new FileStream(Path.Combine(uploads, filename + extension), FileMode.Create))
					{
						files[0].CopyTo(fileStream);
					}
					productVM.Product.ImageUrl = @"\images\products\" + filename + extension;
				}
				else
				{
					if (productVM.Product.Id != 0)
					{
						Product objFromDb = _unitOfWork.Product.Get(productVM.Product.Id);
						productVM.Product.ImageUrl = objFromDb.ImageUrl;
					}
				}

				if (productVM.Product.Id == 0)
				{
					_unitOfWork.Product.Add(productVM.Product);
				}
				else
				{
					_unitOfWork.Product.Update(productVM.Product);
				}
				_unitOfWork.Save();
				return RedirectToAction(nameof(Index));
			}
			else
			{
				IEnumerable<Category> catList = await _unitOfWork.Category.GetAllAsync();
				productVM.CategoryList = catList.Select(c => new SelectListItem
				{
					Text = c.Name,
					Value = c.Id.ToString()
				});
				productVM.CoverTypeList = _unitOfWork.CoverType.GetAll().Select(c => new SelectListItem
				{
					Text = c.Name,
					Value = c.Id.ToString()
				});
				if (productVM.Product.Id != 0)
				{
					productVM.Product = _unitOfWork.Product.Get(productVM.Product.Id);
				}
			}
			return View(productVM);
		}

		#region API Calls

		[HttpGet]
		public IActionResult GetAll()
		{
			var allObj = _unitOfWork.Product.GetAll(includeProperties: "Category,CoverType");
			return Json(new { data = allObj });
		}

		[HttpDelete]
		public IActionResult Delete(int id)
		{
			Product objFromDb = _unitOfWork.Product.Get(id);

			if (objFromDb == null)
			{
				return Json(new { success = false, message = "Error while deleting" });
			}
			string webRootPath = _hostEnvironment.WebRootPath;
			if (objFromDb.ImageUrl != null)
			{
				var imagePath = Path.Combine(webRootPath, objFromDb.ImageUrl.TrimStart('\\'));
				if (System.IO.File.Exists(imagePath))
				{
					System.IO.File.Delete(imagePath);
				}
			}
			_unitOfWork.Product.Remove(objFromDb);
			_unitOfWork.Save();
			return Json(new { success = true, message = "Delete Successful" });
		}

		#endregion API Calls
	}
}