using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SlimBook.DataAccess.Repository.IRepository;
using SlimBook.Models;
using SlimBook.Models.ViewModels;
using SlimBook.Utility;

namespace SlimBook.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = SD.Role_Admin)]
	public class CategoryController : Controller
	{
		public readonly IUnitOfWork _unitOfWork;

		public CategoryController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<IActionResult> Index(int categoryPage = 1)
		{
			int pageSize = 2;
			CategoryVM categoryVM = new CategoryVM()
			{
				Categories = await _unitOfWork.Category.GetAllAsync()
			};
			int count = categoryVM.Categories.ToList().Count;

			while ((categoryPage * pageSize - 1) > count && (categoryPage > 1))
			{
				categoryPage--;
			}

			categoryVM.Categories = categoryVM.Categories
				.OrderBy(c => c.Name)
				.Skip((categoryPage - 1) * pageSize).Take(pageSize).ToList();

			categoryVM.PagingInfo = new PagingInfo
			{
				CurrentPage = categoryPage,
				ItemsPerPage = pageSize,
				TotalItem = count,
				UrlParam = "/Admin/Category/Index?categoryPage=:"
			};

			return View(categoryVM);
		}

		[HttpGet]
		public async Task<IActionResult> Upsert(int? id)
		{
			Category category = new Category();
			if (id == null)
			{
				return View(category);
			}
			category = await _unitOfWork.Category.GetAsync(id.GetValueOrDefault());
			if (category == null)
			{
				return NotFound();
			}
			return View(category);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Upsert(Category category)
		{
			if (ModelState.IsValid)
			{
				if (category.Id == 0)
				{
					await _unitOfWork.Category.AddAsync(category);
				}
				else
				{
					_unitOfWork.Category.Update(category);
				}
				_unitOfWork.Save();
				return RedirectToAction(nameof(Index));
			}
			return View(category);
		}

		#region API Calls

		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			var allObj = await _unitOfWork.Category.GetAllAsync();
			return Json(new { data = allObj });
		}

		[HttpDelete]
		public async Task<IActionResult> Delete(int id)
		{
			Category objFromDb = await _unitOfWork.Category.GetAsync(id);

			if (objFromDb == null)
			{
				TempData["Error"] = "Error deleting Category";
				return Json(new { success = false, message = "Error while deleting" });
			}
			await _unitOfWork.Category.RemoveAsync(objFromDb);
			_unitOfWork.Save();
			TempData["Success"] = "Category successfully deleted";
			return Json(new { success = true, message = "Delete Successful" });
		}

		#endregion API Calls
	}
}