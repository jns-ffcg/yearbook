using System.Collections.Generic;
using System.Threading.Tasks;

namespace YearbookApp
{
    public interface IBookService
    {
        public Task<IEnumerable<BookItem>> GetAll();
    }
}