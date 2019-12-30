import { NgModule } from '@angular/core';
import { MatDialogModule, MatFormFieldModule, MatInputModule, MatOptionModule, MatProgressSpinnerModule, MatRadioModule, MatSelectModule, MatSnackBarModule, MatTooltipModule } from '@angular/material';
import { MatButtonModule } from '@angular/material/button';
import { AppComponent } from './components/app/app.component';
import { FooterComponent } from './components/footer/footer.component';
import { HeaderComponent } from './components/header/header.component';
import { CreateBookmarksDialog } from './components/home/create.dialog';
import { HomeComponent } from './components/home/home.component';
import { EllipsisPipe } from './shared/pipes/ellipsis';
import { ApiAppInfoService } from './shared/service/api.appinfo.service';
import { ApiBookmarksService } from './shared/service/api.bookmarks.service';
import { ApplicationState } from './shared/service/application.state';


@NgModule({
  imports: [ MatProgressSpinnerModule, MatTooltipModule, MatSnackBarModule, MatButtonModule, MatDialogModule, MatInputModule, MatFormFieldModule, MatRadioModule, MatOptionModule, MatSelectModule ],
  exports: [ MatProgressSpinnerModule, MatTooltipModule, MatSnackBarModule, MatButtonModule, MatDialogModule, MatInputModule, MatFormFieldModule, MatRadioModule, MatOptionModule, MatSelectModule ],
})
export class AppMaterialModule { }

export const sharedConfig: NgModule = {
    bootstrap: [ AppComponent ],
    declarations: [
        AppComponent,
        HomeComponent,
        FooterComponent,
        HeaderComponent,
        EllipsisPipe,
        CreateBookmarksDialog
    ],
    imports: [
        AppMaterialModule
    ],
    providers: [ ApplicationState, ApiAppInfoService, ApiBookmarksService ],
    entryComponents: [ CreateBookmarksDialog ]
};
