export enum ItemType {
  Node = "node",
  Folder = "folder"
}

export class BookmarkModel {
  public Id: string;
  public Path: string;
  public DisplayName: string;
  public Url: string;
  public SortOrder: number;
  public Type: ItemType;
  public Created: Date;
  public Modified: Date;
  public ChildCound: number;
}
