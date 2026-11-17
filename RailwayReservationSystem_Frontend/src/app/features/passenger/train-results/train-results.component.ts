import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { PassengerDashboardActions, PassengerDashboardViewModel } from '../../dashboard-view.models';
import { TrainSearchResult } from '../../../core/train-search.service';

@Component({
  selector: 'app-train-results',
  standalone: true,
  templateUrl: './train-results.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TrainResultsComponent {
  readonly vm = input.required<PassengerDashboardViewModel>();
  readonly actions = input.required<PassengerDashboardActions>();

  protected duration(train: TrainSearchResult): string {
    const minutesSinceMidnight = (time: string): number => {
      const [hours, minutes] = time.split(':').map(Number);
      return hours * 60 + minutes;
    };
    let minutes = minutesSinceMidnight(train.arrivalTime) - minutesSinceMidnight(train.departureTime);
    if (train.arrivalNextDay || minutes < 0) minutes += 24 * 60;

    return `${Math.floor(minutes / 60)}h ${minutes % 60}m`;
  }
}
