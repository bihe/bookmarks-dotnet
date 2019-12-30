export enum ItemType {
  Node = "Node",
  Folder = "Folder"
}

export class BookmarkModel {
  public id: string;
  public path: string;
  public displayName: string;
  public url: string;
  public sortOrder: number;
  public type: ItemType;
  public created: Date;
  public modified: Date;
  public childCount: number;
}
