import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { AdminDashboardActions, AdminDashboardViewModel } from '../../dashboard-view.models';

@Component({
  selector: 'app-admin-overview',
  standalone: true,
  templateUrl: './admin-overview.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminOverviewComponent {
  readonly vm = input.required<AdminDashboardViewModel>();
  readonly actions = input.required<AdminDashboardActions>();
}
