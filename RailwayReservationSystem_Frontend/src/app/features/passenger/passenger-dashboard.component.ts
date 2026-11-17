import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { PassengerDashboardActions, PassengerDashboardViewModel } from '../dashboard-view.models';
import { JourneySearchComponent } from './journey-search/journey-search.component';
import { ReservationFormComponent } from './reservation-form/reservation-form.component';
import { TrainResultsComponent } from './train-results/train-results.component';

@Component({
  selector: 'app-passenger-dashboard',
  standalone: true,
  imports: [JourneySearchComponent, ReservationFormComponent, TrainResultsComponent],
  templateUrl: './passenger-shell.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PassengerDashboardComponent {
  readonly vm = input.required<PassengerDashboardViewModel>();
  readonly actions = input.required<PassengerDashboardActions>();

}
