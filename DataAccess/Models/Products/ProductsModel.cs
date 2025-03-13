using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Models.Products
{
    public class ProductsModel
    {


        public Product? product { get; set; }
    }

    public class Product
    {
        public int id { get; set; }
        [Required]
        public string name { get; set; } // required
        public string page_title { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public int? days_to_expire { get; set; }
        [Required]
        public float price { get; set; } // required
        public float discount { get; set; }
        public float weight { get; set; }
        public int stock { get; set; }
        public bool stock_unlimited { get; set; }
        public int stock_threshold { get; set; }
        public bool stock_notification { get; set; }
        public float? cost_per_item { get; set; }
        public float? compare_at_price { get; set; }
        public string sku { get; set; }
        public string brand { get; set; }
        public string barcode { get; set; }
        public string google_product_category { get; set; }
        public bool featured { get; set; }
        public bool reviews_enabled { get; set; }
        public string status { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public string package_format { get; set; }
        public float length { get; set; }
        public float width { get; set; }
        public float height { get; set; }
        public float diameter { get; set; }
        public string permalink { get; set; }
        public List<Category>? categories { get; set; }
        public List<Image>? images { get; set; }
        public List<Variant>? variants { get; set; }
    }

    public class Category
    {
        public int id { get; set; }
        public string name { get; set; }
        public int parent_id { get; set; }
        public string permalink { get; set; }
    }

    public class Image
    {
        public int id { get; set; }
        public int position { get; set; }
        public string url { get; set; }
    }

    public class Variant
    {
        public int id { get; set; }
        public float price { get; set; }
        public string sku { get; set; }
        public string barcode { get; set; }
        public int stock { get; set; }
        public bool stock_unlimited { get; set; }
        public int stock_threshold { get; set; }
        public bool stock_notification { get; set; }
        public float? cost_per_item { get; set; }
        public float? compare_at_price { get; set; }
        public Option[] options { get; set; }
        public Image1 image { get; set; }
    }

    public class Image1
    {
        public int id { get; set; }
        public int position { get; set; }
        public string url { get; set; }
    }

    public class Option
    {
        public string name { get; set; }
        public string option_type { get; set; }
        public string value { get; set; }
        public string custom { get; set; }
        public int product_option_position { get; set; }
        public int product_value_position { get; set; }
    }

}

