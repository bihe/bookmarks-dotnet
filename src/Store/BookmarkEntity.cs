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
        public string Id { get; set; }
        public string Path { get; set; }
        public string DisplayName { get; set; }
        public string Url { get; set; }
        public int SortOrder { get; set; }
        public ItemType Type { get; set; }
        public string UserName { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }
        public int ChildCount { get; set; }

        public override string ToString()
        {
            return $"Bookmark: '{Path}, {DisplayName}' (Id: {Id}, Type: {Type.ToString()})";
        }
    }
}
