using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Models.Products
{
    public class ImgJumpsellerModel
    {

        public ImagePost image { get; set; }
    }

    public class ImagePost
    {
        public string url { get; set; }
        public int position { get; set; }
        public int id { get; set; }
    }


}
