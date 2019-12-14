using System;

namespace Store
{
    public enum ItemType
    {
        Node,
        Folder
    }

    public class BookmarkEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public int SortOrder { get; set; } = 0;
        public ItemType Type { get; set; } = ItemType.Node;
        public string UserName { get; set; } = string.Empty;
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime? Modified { get; set; }
        public int ChildCount { get; set; } = 0;

        public override string ToString()
        {
            return $"Bookmark: '{Path}, {DisplayName}' (Id: {Id}, Type: {Type.ToString()})";
        }
    }
}
