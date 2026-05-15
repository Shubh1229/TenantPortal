namespace TenantPortal.Shared.DTOs
{
    /// <summary>
    /// Generic wrapper for paginated query results.
    /// </summary>
    /// <typeparam name="T">The type of items in the page.</typeparam>
    public class PagedResultDTO<T>
    {
        /// <summary>The items on the current page.</summary>
        public List<T> Items { get; set; }

        /// <summary>Total number of records across all pages.</summary>
        public int Records { get; set; }

        /// <summary>The current page number (1-based).</summary>
        public int PageNumber { get; set; }

        /// <summary>Number of items per page.</summary>
        public int PageSize { get; set; }

        /// <summary>Total number of pages, calculated from <see cref="Records"/> and <see cref="PageSize"/>.</summary>
        public int CalculatedPages { get; private set; }

        /// <summary>
        /// Initialises a paged result and calculates the total page count.
        /// </summary>
        public PagedResultDTO(List<T> items, int records, int pageNumber, int pageSize)
        {
            Items = items;
            Records = records;
            PageNumber = pageNumber;
            PageSize = pageSize;
            CalculatedPages = (int)Math.Ceiling(records / (double)pageSize);
        }
    }
}
