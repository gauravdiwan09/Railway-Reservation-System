import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { AdminDashboardActions, AdminDashboardViewModel } from '../dashboard-view.models';
import { AdminOverviewComponent } from './overview/admin-overview.component';
import { TrainManagementComponent } from './train-management/train-management.component';
import { ScheduleManagementComponent } from './schedule-management/schedule-management.component';
import { FareManagementComponent } from './fare-management/fare-management.component';
import { SeatManagementComponent } from './seat-management/seat-management.component';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [AdminOverviewComponent, FareManagementComponent, ScheduleManagementComponent, SeatManagementComponent, TrainManagementComponent],
  templateUrl: './admin-dashboard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminDashboardComponent {
  readonly vm = input.required<AdminDashboardViewModel>();
  readonly actions = input.required<AdminDashboardActions>();
}
