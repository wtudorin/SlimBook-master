using System.Linq;
using SlimBook.DataAccess.Data;
using SlimBook.DataAccess.Repository.IRepository;
using SlimBook.Models;

namespace SlimBook.DataAccess.Repository
{
    public class CompanyRepository : Repository<Company>, ICompanyRepository
    {
        private readonly ApplicationDbContext _db = null;

        public CompanyRepository(ApplicationDbContext db) : base(db)
        {
            this._db = db;
        }

        public void Update(Company company)
        {
            _db.Update(company);
        }
    }
}