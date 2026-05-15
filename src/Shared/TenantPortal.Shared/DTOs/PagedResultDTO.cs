
namespace TenantPortal.Shared.DTOs
{
    public class PagedResultDTO<T>
    {
        public List<T> Items { get; set; }
        public int Records { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int CalculatedPages { get; private set; }
        public PagedResultDTO(List<T> items, int records, int pageNumber, int pageSize) 
        {
            this.Items = items;
            this.Records = records;
            this.PageNumber = pageNumber;
            this.PageSize = pageSize;
            this.CalculatedPages = (int)Math.Ceiling(records / (double)pageSize);
        }
    }
}