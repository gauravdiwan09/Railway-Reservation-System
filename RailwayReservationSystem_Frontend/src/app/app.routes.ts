import { Routes } from '@angular/router';
import { PageRouteOutletComponent } from './features/shell/page-route-outlet.component';

export const routes: Routes = [
	{ path: '', pathMatch: 'full', redirectTo: 'home' },
	{ path: 'home', component: PageRouteOutletComponent, data: { page: 'home' } },
	{ path: 'bookings', component: PageRouteOutletComponent, data: { page: 'bookings' } },
	{ path: 'admin', pathMatch: 'full', redirectTo: 'admin/overview' },
	{ path: 'admin/:section', component: PageRouteOutletComponent, data: { page: 'admin' } },
	{ path: '**', redirectTo: 'home' }
];
