using System.Linq;
using SlimBook.DataAccess.Data;
using SlimBook.DataAccess.Repository.IRepository;
using SlimBook.Models;

namespace SlimBook.DataAccess.Repository
{
	public class CategoryRepository : RepositoryAsync<Category>, ICategoryRepository
	{
		private readonly ApplicationDbContext _db = null;

		public CategoryRepository(ApplicationDbContext db) : base(db)
		{
			this._db = db;
		}

		public void Update(Category category)
		{
			var objFromDB = _db.Categories.FirstOrDefault(c => c.Id == category.Id);
			if (objFromDB != null)
			{
				objFromDB.Name = category.Name;
			}
		}
	}
}