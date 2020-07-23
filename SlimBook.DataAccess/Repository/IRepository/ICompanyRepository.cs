using SlimBook.Models;

namespace SlimBook.DataAccess.Repository.IRepository
{
    public interface ICompanyRepository : IRepository<Company>
    {
        void Update(Company Company);
    }
}