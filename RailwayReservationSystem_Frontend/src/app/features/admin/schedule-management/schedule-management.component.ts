import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminDashboardActions, AdminDashboardViewModel } from '../../dashboard-view.models';

@Component({
  selector: 'app-schedule-management',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './schedule-management.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ScheduleManagementComponent {
  readonly vm = input.required<AdminDashboardViewModel>();
  readonly actions = input.required<AdminDashboardActions>();
}