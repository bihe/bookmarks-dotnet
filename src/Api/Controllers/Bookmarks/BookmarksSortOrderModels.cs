using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Api.Controllers.Bookmarks
{
    public class BookmarksSortOrderModel
    {
        public List<string> Ids { get; set; } = new List<string>();
        public List<int> SortOrder { get; set; } = new List<int>();

        public override string ToString()
        {
            return $"Ids: '{string.Join(",", Ids)}', SortOrder: {string.Join(",", SortOrder)}";
        }
    }
}
