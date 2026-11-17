import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PassengerDashboardActions, PassengerDashboardViewModel } from '../../dashboard-view.models';

@Component({
  selector: 'app-reservation-form',
  standalone: true,
  imports: [CurrencyPipe, FormsModule],
  templateUrl: './reservation-form.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReservationFormComponent {
  readonly vm = input.required<PassengerDashboardViewModel>();
  readonly actions = input.required<PassengerDashboardActions>();
}