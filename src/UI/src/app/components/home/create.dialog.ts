import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { BookmarkModel, ItemType } from 'src/app/shared/models/bookmarks.model';
import { CreateBookmarkModel } from './create.model';

@Component({
  selector: 'create.dialog',
  templateUrl: 'create.dialog.html',
  styleUrls: ['create.dialog.css'],
})
export class CreateBookmarksDialog implements OnInit {

  bookmark: BookmarkModel
  type: string
  selectedPath: string

  constructor(public dialogRef: MatDialogRef<CreateBookmarksDialog>,
    @Inject(MAT_DIALOG_DATA) public data: CreateBookmarkModel)
  {}

  ngOnInit(): void {
    this.bookmark = new BookmarkModel();
    this.type = ItemType.Node.toString();
    this.selectedPath = this.data.currentPath;
  }

  onSave(): void {
    let itemType = ItemType.Node;
    if (this.type === 'folder') {
      itemType = ItemType.Folder;
    }
    this.bookmark.Type = itemType;
    this.bookmark.Path = this.selectedPath;
    this.dialogRef.close({
      result: true,
      model: this.bookmark
    });
  }

  onNoClick(): void {
    this.dialogRef.close({
      result: false
    });
  }

}
