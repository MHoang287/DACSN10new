using DACSN10.Models;
using System.ComponentModel.DataAnnotations;

namespace DACSN10.Areas.Admin.Models
{
    public class CategoryAdminViewModel
    {
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        [Display(Name = "Tên Danh Mục")]
        public string Name { get; set; }

        [Display(Name = "Mô Tả")]
        public string Description { get; set; }

        [Display(Name = "Số Lượng Khóa Học")]
        public int CourseCount { get; set; }
    }

    public class CategorySearchViewModel
    {
        public string Keyword { get; set; }
        public List<CategoryAdminViewModel> SearchResults { get; set; } = new List<CategoryAdminViewModel>();
    }
}