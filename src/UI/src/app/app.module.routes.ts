import { NgModule } from '@angular/core';
import { RouterModule, Routes, UrlSegment } from '@angular/router';
import { HomeComponent } from './components/home/home.component';

const routes: Routes = [
  { path: '', redirectTo: 'start', pathMatch: 'full' },
  {
    // custom matcher
    // match for all URLs starting with 'start' and collect the sub-path in
    // the variable path
    // e.g. /start => path: /
    // e.g. /start/Folder1/Folders2 => path: /Folder1/Folder2
    // e.g. /start/a/b/c/d/e/f/g => path: /a/b/c/d/e/f/g
    matcher: (url) => {
      if (url[0].path === 'start') {
        let path = '/';
        if (url.length > 1) {
          url.forEach((e, i) => {
            if (e.path !== 'start') {
              if (!path.endsWith('/')) {
                path += '/';
              }
              path += e.path;
            }
          });
        }
        return {
          consumed: url,
          posParams: {
            path: new UrlSegment(path, {})
          }
        };
      }
      return null;
    },
    component: HomeComponent
  },
  { path: 'start', component: HomeComponent },
  { path: '**', redirectTo: 'start', }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
  })
export class AppRoutingModule {}
