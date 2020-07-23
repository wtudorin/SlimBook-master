using System.Linq;
using SlimBook.DataAccess.Data;
using SlimBook.DataAccess.Repository.IRepository;
using SlimBook.Models;

namespace SlimBook.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private readonly ApplicationDbContext _db = null;

        public OrderHeaderRepository(ApplicationDbContext db) : base(db)
        {
            this._db = db;
        }

        public void Update(OrderHeader orderHeader)
        {
            _db.Update(orderHeader);
        }
    }
}