import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminDashboardActions, AdminDashboardViewModel } from '../../dashboard-view.models';

@Component({
  selector: 'app-seat-management',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './seat-management.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SeatManagementComponent {
  readonly vm = input.required<AdminDashboardViewModel>();
  readonly actions = input.required<AdminDashboardActions>();
}