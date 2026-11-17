import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminDashboardActions, AdminDashboardViewModel } from '../../dashboard-view.models';

@Component({
  selector: 'app-train-management',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './train-management.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TrainManagementComponent {
  readonly vm = input.required<AdminDashboardViewModel>();
  readonly actions = input.required<AdminDashboardActions>();
}