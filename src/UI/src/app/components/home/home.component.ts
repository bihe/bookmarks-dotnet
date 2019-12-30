import { Component, OnInit } from '@angular/core';
import { MatDialog, MatSnackBar } from '@angular/material';
import { Router } from '@angular/router';
import { BookmarkModel } from 'src/app/shared/models/bookmarks.model';
import { ProblemDetail } from 'src/app/shared/models/error.problemdetail';
import { ApplicationState } from 'src/app/shared/service/application.state';
import { MessageUtils } from 'src/app/shared/utils/message.utils';
import { ApiBookmarksService } from '../../shared/service/api.bookmarks.service';
import { ConfirmDialogComponent, ConfirmDialogModel } from '../confirm-dialog/confirm-dialog.component';
import { CreateBookmarksDialog } from './create.dialog';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {

  bookmarks: BookmarkModel[] = [];
  currentPath: string = '';
  pathElemets: string[] = [];
  absolutePaths: string[] = [];
  isAdmin: boolean = false;
  search: string = '';
  searchMode: boolean = false;

  constructor(private bookmarksService: ApiBookmarksService,
    private snackBar: MatSnackBar,
    private router: Router,
    public dialog: MatDialog,
    private state: ApplicationState
  ) {}

  ngOnInit() {
    this.currentPath = '/';
    this.getBookmarksForPath(this.currentPath); // initial load

    this.state.isAdmin().subscribe(
      data => {
       this.isAdmin = data;
      }
    );
  }

  getBookmarksForPath(path: string) {
    this.state.setProgress(true);
    this.bookmarksService.getBookmarksForPath(path)
      .subscribe(
        data => {
          this.state.setProgress(false);
          if (data.count > 0) {
            this.bookmarks = data.value;
          } else {
            this.bookmarks = [];
          }
          this.currentPath = path;
          this.pathElemets = this.splitPathElements(path);
          console.log('currentPath: ' + this.currentPath);
        },
        error => {
          this.state.setProgress(false);
          console.log('Error: ' + error);
          new MessageUtils().showError(this.snackBar, error);
        }
      );
  }

  gotoPath(path: string) {
    if (path.startsWith('//')) {
      path = path.replace('//', '/'); // fix for the root path!
    }
    console.log('goto: ' + path);
    this.searchMode = false;
    this.getBookmarksForPath(path);
  }

  editBookmark(id: string) {
    console.log('edit bookmark: ' + id);
    this.bookmarksService.fetchBookmarkById(id).subscribe(
      data => {
        this.state.setProgress(false);
        console.log(data);

        const dialogRef = this.dialog.open(CreateBookmarksDialog, {
          width: '50vw',
          data: {
            absolutePaths: this.absolutePaths,
            currentPath: this.currentPath,
            existingBookmark: data
          }
        });

        dialogRef.afterClosed().subscribe(data => {
          console.log('dialog was closed');
          if (data.result) {
            let bookmark:BookmarkModel = data.model;
            console.log(bookmark);

            // update the UI immediately!
            let oldDisplayName = '';
            let existing = this.bookmarks.find(x => x.id === bookmark.id);
            if (existing) {
              oldDisplayName = existing.displayName;
              existing.displayName = bookmark.displayName;
            }

            this.bookmarksService.updateBookmark(bookmark).subscribe(
              data => {
                this.state.setProgress(false);
                console.log(data);
                if (data.success) {
                  new MessageUtils().showSuccess(this.snackBar, data.message);
                  this.getBookmarksForPath(this.currentPath);
                }
              },
              error => {
                if (oldDisplayName && existing) {
                  existing.displayName = oldDisplayName;
                }
                const errorDetail: ProblemDetail = error;
                this.state.setProgress(false);
                console.log(errorDetail);
                new MessageUtils().showError(this.snackBar, errorDetail.detail);
              }
            );
          }
        });

      },
      error => {
        const errorDetail: ProblemDetail = error;
        this.state.setProgress(false);
        console.log(errorDetail);
        new MessageUtils().showError(this.snackBar, errorDetail.detail);
      }
    );
  }

  addBookmark() {
    console.log('add bookmark!');
    const dialogRef = this.dialog.open(CreateBookmarksDialog, {
      width: '50vw',
      data: {
        absolutePaths: this.absolutePaths,
        currentPath: this.currentPath
      }
    });

    dialogRef.afterClosed().subscribe(data => {
      console.log('dialog was closed');
      if (data.result) {
        let bookmark:BookmarkModel = data.model;
        console.log(bookmark);

        this.bookmarksService.createBookmark(bookmark).subscribe(
          data => {
            this.state.setProgress(false);
            console.log(data);
            if (data.success) {
              new MessageUtils().showSuccess(this.snackBar, data.message);
              this.getBookmarksForPath(this.currentPath);
            }
          },
          error => {
            const errorDetail: ProblemDetail = error;
            this.state.setProgress(false);
            console.log(errorDetail);
            new MessageUtils().showError(this.snackBar, errorDetail.detail);
          }
        );
      }
    });
  }

  deleteBookmark(id: string) {
    const dialogData = new ConfirmDialogModel('Confirm delete!', 'Really delete bookmark?');

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      maxWidth: "400px",
      data: dialogData
    });

    dialogRef.afterClosed().subscribe(dialogResult => {
      console.log(dialogResult);
      if (dialogResult === true) {
        console.log('Will delete bookmark by id ' + id);

        this.bookmarksService.deleteBookmarkById(id).subscribe(
          data => {
            this.state.setProgress(false);
            console.log(data);
            if (data.success) {
              new MessageUtils().showSuccess(this.snackBar, data.message);
              this.getBookmarksForPath(this.currentPath);
            }
          },
          error => {
            const errorDetail: ProblemDetail = error;
            this.state.setProgress(false);
            console.log(errorDetail);
            new MessageUtils().showError(this.snackBar, errorDetail.detail);
          }
        );
      }
    });
  }

  searchBookmarks() {
    console.log('search for ' + this.search);
    this.state.setProgress(true);
    this.bookmarksService.getBookmarksByName(this.search)
      .subscribe(
        data => {
          this.state.setProgress(false);
          if (data.count > 0) {
            this.bookmarks = data.value;
            this.searchMode = true;
            this.currentPath = '/';
            this.pathElemets = this.splitPathElements(this.currentPath);
            this.search = '';
          } else {
            this.bookmarks = [];
          }
        },
        error => {
          this.state.setProgress(false);
          console.log('Error: ' + error);
          new MessageUtils().showError(this.snackBar, error);
        }
      );
  }

  private splitPathElements(path: string) : string[] {
    let parts = path.split('/');
    if (parts && parts.length > 0) {
      if (parts[0] === '') {
        parts[0] = '/';
      }
    } else {
      parts = [];
      parts[0] = '/';
    }

    if (parts[parts.length -1] === '') {
      parts.pop();
    }

    // also create a list of the path-elements which create the absolute path
    // for each element
    this.absolutePaths = [];
    let absPath = '';
    parts.forEach(e => {
      if (absPath !== '' && !absPath.endsWith('/')) {
        absPath += '/';
      }
      absPath += e;
      this.absolutePaths.push(absPath);
    });

    return parts;
  }
}
