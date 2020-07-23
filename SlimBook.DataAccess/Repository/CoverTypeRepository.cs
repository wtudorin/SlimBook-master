using System.Linq;
using SlimBook.DataAccess.Data;
using SlimBook.DataAccess.Repository.IRepository;
using SlimBook.Models;

namespace SlimBook.DataAccess.Repository
{
    public class CoverTypeRepository : Repository<CoverType>, ICoverTypeRepository
    {
        private readonly ApplicationDbContext _db = null;

        public CoverTypeRepository(ApplicationDbContext db) : base(db)
        {
            this._db = db;
        }

        public void Update(CoverType CoverType)
        {
            var objFromDB = _db.CoverTypes.FirstOrDefault(c => c.Id == CoverType.Id);
            if (objFromDB != null)
            {
                objFromDB.Name = CoverType.Name;
            }
        }
    }
}