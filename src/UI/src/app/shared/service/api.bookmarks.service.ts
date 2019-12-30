import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { catchError, timeout } from 'rxjs/operators';
import { BookmarkModel } from '../models/bookmarks.model';
import { ListResult, Result } from '../models/result.model';
import { BaseDataService } from './api.base.service';


@Injectable()
export class ApiBookmarksService extends BaseDataService {
  private readonly BASE_URL: string = '/api/v1/bookmarks';

  constructor (private http: HttpClient) {
    super();
  }

  getBookmarksForPath(path: string): Observable<ListResult<BookmarkModel[]>> {
    const url = `${this.BASE_URL}/find?path=${path}`;
    return this.http.get<ListResult<BookmarkModel[]>>(url, this.RequestOptions)
      .pipe(
        timeout(this.RequestTimeOutDefault),
        catchError(this.handleError)
      );
  }

  fetchBookmarkById(id: string): Observable<BookmarkModel> {
    const url = `${this.BASE_URL}/${id}`;
    return this.http.get<BookmarkModel>(url, this.RequestOptions)
      .pipe(
        timeout(this.RequestTimeOutDefault),
        catchError(this.handleError)
      );
  }

  createBookmark(model: BookmarkModel): Observable<Result<string>> {
    return this.http.post<Result<string>>(this.BASE_URL, model, this.RequestOptions)
      .pipe(
        timeout(this.RequestTimeOutDefault),
        catchError(this.handleError)
      );
  }

  deleteBookmarkById(id: string): Observable<Result<string>> {
    const url = `${this.BASE_URL}/${id}`;
    return this.http.delete<Result<string>>(url, this.RequestOptions)
      .pipe(
        timeout(this.RequestTimeOutDefault),
        catchError(this.handleError)
      );
  }

  updateBookmark(model: BookmarkModel): Observable<Result<string>> {
    return this.http.put<Result<string>>(this.BASE_URL, model, this.RequestOptions)
      .pipe(
        timeout(this.RequestTimeOutDefault),
        catchError(this.handleError)
      );
  }
}