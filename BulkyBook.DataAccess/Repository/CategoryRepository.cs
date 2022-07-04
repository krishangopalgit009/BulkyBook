using BulkyBook.Data;
using BulkyBook.DataAccess.IRepository;
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        private readonly ApplicationDBContext _dBConext;

        public CategoryRepository(ApplicationDBContext DBConext) : base(DBConext)
        {
            _dBConext = DBConext;
        }

        public void Update(Category category)
        {
            _dBConext.Update(category);
        }
    }
}
