namespace elemechWisetrack.Models
{
    public class CategoryModel
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public string CategorySlug { get; set; } // new field
        public string? CategoryImage { get; set; }
        public bool CategoryStatus { get; set; }
    }

}
