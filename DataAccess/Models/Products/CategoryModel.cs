using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Models.Products
{
    public class CategoryModel
    {

        public int? id { get; set; }
        public string name { get; set; }
        public object description { get; set; }
        public List<object> images { get; set; }
        public List<ProductsCategryModel> products { get; set; }
        public int? parent_id { get; set; }
        public string permalink { get; set; }


        public class CategoryResponse
        {
            public CategoryModel Category { get; set; }
        }        
        public class ProductsCategryModel
        {
            public int? id { get; set; }
        }




    }
}
