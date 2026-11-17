import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { PassengerDashboardActions, PassengerDashboardViewModel } from '../../dashboard-view.models';

type BookingFilter = 'all' | 'upcoming' | 'past' | 'cancelled';

@Component({
  selector: 'app-booking-history',
  standalone: true,
  imports: [CurrencyPipe],
  templateUrl: './booking-history.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BookingHistoryComponent {
  readonly vm = input.required<PassengerDashboardViewModel>();
  readonly actions = input.required<PassengerDashboardActions>();

  protected filter: BookingFilter = 'all';

  protected get filteredBookings() {
    const bookings = this.vm().bookings;
    if (this.filter === 'cancelled') return bookings.filter((booking) => booking.status.toLowerCase() === 'cancelled');
    if (this.filter === 'upcoming') return bookings.filter((booking) => booking.status.toLowerCase() !== 'cancelled' && booking.journeyDate >= this.vm().today);
    if (this.filter === 'past') return bookings.filter((booking) => booking.status.toLowerCase() !== 'cancelled' && booking.journeyDate < this.vm().today);
    return bookings;
  }

  protected bookingCount(filter: BookingFilter): number {
    const bookings = this.vm().bookings;
    if (filter === 'cancelled') return bookings.filter((booking) => booking.status.toLowerCase() === 'cancelled').length;
    if (filter === 'upcoming') return bookings.filter((booking) => booking.status.toLowerCase() !== 'cancelled' && booking.journeyDate >= this.vm().today).length;
    if (filter === 'past') return bookings.filter((booking) => booking.status.toLowerCase() !== 'cancelled' && booking.journeyDate < this.vm().today).length;
    return bookings.length;
  }
}
