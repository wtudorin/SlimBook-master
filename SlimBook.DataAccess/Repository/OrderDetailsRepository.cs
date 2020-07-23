using System.Linq;
using SlimBook.DataAccess.Data;
using SlimBook.DataAccess.Repository.IRepository;
using SlimBook.Models;

namespace SlimBook.DataAccess.Repository
{
    public class OrderDetailsRepository : Repository<OrderDetails>, IOrderDetailsRepository
    {
        private readonly ApplicationDbContext _db = null;

        public OrderDetailsRepository(ApplicationDbContext db) : base(db)
        {
            this._db = db;
        }

        public void Update(OrderDetails orderDetails)
        {
            _db.Update(orderDetails);
        }
    }
}