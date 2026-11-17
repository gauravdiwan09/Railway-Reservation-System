import { ChangeDetectionStrategy, Component, input, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PassengerDashboardActions, PassengerDashboardViewModel } from '../../dashboard-view.models';

@Component({
  selector: 'app-journey-search',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './journey-search.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class JourneySearchComponent implements OnInit {
  readonly vm = input.required<PassengerDashboardViewModel>();
  readonly actions = input.required<PassengerDashboardActions>();

  protected searchDraft = { source: '', destination: '', journeyDate: '' };
  protected pnrQuery = '';

  ngOnInit(): void {
    this.searchDraft = { ...this.vm().journeyForm };
  }

  protected search(): void {
    this.actions().searchTrains(this.searchDraft.source, this.searchDraft.destination, this.searchDraft.journeyDate);
  }

  protected swapStations(): void {
    [this.searchDraft.source, this.searchDraft.destination] = [this.searchDraft.destination, this.searchDraft.source];
  }

  protected checkPnr(): void {
    this.actions().findBookingByPnr(this.pnrQuery);
  }
}
