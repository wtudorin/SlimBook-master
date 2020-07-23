using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimBook.Models;

namespace SlimBook.DataAccess.Repository.IRepository
{
	public interface ICategoryRepository : IRepositoryAsync<Category>
	{
		void Update(Category category);
	}
}