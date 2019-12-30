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
    if (this.data.existingBookmark) {
      this.bookmark = this.data.existingBookmark;
      this.type = this.bookmark.type.toString();
    } else {
      this.bookmark = new BookmarkModel();
      this.bookmark.id = '';
      this.type = ItemType.Node.toString();
    }
    this.selectedPath = this.data.currentPath;
  }

  onSave(): void {
    let itemType = ItemType.Node;
    if (this.type === 'Folder') {
      itemType = ItemType.Folder;
    }
    this.bookmark.type = itemType;
    this.bookmark.path = this.selectedPath;
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
