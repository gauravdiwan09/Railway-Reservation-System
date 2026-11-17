import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminDashboardActions, AdminDashboardViewModel } from '../../dashboard-view.models';

@Component({
  selector: 'app-fare-management',
  standalone: true,
  imports: [CurrencyPipe, FormsModule],
  templateUrl: './fare-management.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FareManagementComponent {
  readonly vm = input.required<AdminDashboardViewModel>();
  readonly actions = input.required<AdminDashboardActions>();
}