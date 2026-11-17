import { TestBed } from '@angular/core/testing';
import { BookingHistoryComponent } from './booking-history/booking-history.component';
import { JourneySearchComponent } from './journey-search/journey-search.component';
import { TrainResultsComponent } from './train-results/train-results.component';
import { PassengerDashboardActions, PassengerDashboardViewModel } from '../dashboard-view.models';
import { TrainSearchResult } from '../../core/train-search.service';

const journeys: TrainSearchResult[] = [
  { trainId: 1, trainNumber: '100', trainName: 'Zulu Express', source: 'A', destination: 'B', totalSeats: 20, scheduleId: 11, departureTime: '09:00:00', arrivalTime: '12:00:00', arrivalNextDay: false },
  { trainId: 2, trainNumber: '200', trainName: 'Alpha Express', source: 'A', destination: 'B', totalSeats: 20, scheduleId: 22, departureTime: '07:30:00', arrivalTime: '11:00:00', arrivalNextDay: false },
  { trainId: 3, trainNumber: '300', trainName: 'Central Express', source: 'A', destination: 'B', totalSeats: 20, scheduleId: 33, departureTime: '10:00:00', arrivalTime: '02:00:00', arrivalNextDay: true }
];

function dashboardViewModel(overrides: Partial<PassengerDashboardViewModel> = {}): PassengerDashboardViewModel {
  return {
    authenticated: true,
    today: '2026-10-01',
    journeyForm: { source: 'A', destination: 'B', journeyDate: '2026-10-01' },
    results: [],
    loadingClassAvailability: false,
    seatClasses: () => [],
    bookings: [],
    loadingBookings: false,
    searching: false,
    errorMessage: '',
    pnrBooking: null,
    pnrMessage: '',
    pnrError: false,
    checkingPnr: false,
    selectedJourney: null,
    checkingJourney: false,
    scheduleOperating: null,
    bookingDetails: null,
    journeyMessage: '',
    journeyFares: [],
    selectedClassType: '',
    availableJourneySeats: [],
    selectedJourneyFare: null,
    bookingTotal: 0,
    journeyFarePerPassenger: 0,
    passengerDrafts: [],
    submittingBooking: false,
    paymentOpen: false,
    paymentError: '',
    ...overrides
  };
}

const noActions = {} as PassengerDashboardActions;

describe('Passenger feature components', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [JourneySearchComponent, TrainResultsComponent, BookingHistoryComponent] });
  });

  it('swaps the departure and arrival fields without changing the journey date', async () => {
    const fixture = TestBed.createComponent(JourneySearchComponent);
    fixture.componentRef.setInput('vm', dashboardViewModel());
    fixture.componentRef.setInput('actions', noActions);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const source = compiled.querySelector<HTMLInputElement>('[name="source"]')!;
    const destination = compiled.querySelector<HTMLInputElement>('[name="destination"]')!;
    compiled.querySelector<HTMLButtonElement>('.route-swap')!.click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(source.value).toBe('B');
    expect(destination.value).toBe('A');
    expect(compiled.querySelector<HTMLInputElement>('[name="journeyDate"]')?.value).toBe('2026-10-01');
  });

  it('shows class availability and journey duration without reshaping backend order', () => {
    const fixture = TestBed.createComponent(TrainResultsComponent);
    fixture.componentRef.setInput('vm', dashboardViewModel({
      results: journeys,
      seatClasses: (scheduleId) => scheduleId === 11
        ? [{ classType: 'SL', availableSeats: 0 }, { classType: 'AC3', availableSeats: 4 }]
        : scheduleId === 22
          ? [{ classType: 'SL', availableSeats: 2 }]
          : [{ classType: 'AC3', availableSeats: 0 }]
    }));
    fixture.componentRef.setInput('actions', {
      formatTime: (time: string) => time.slice(0, 5),
      prepareBooking: () => undefined
    } as unknown as PassengerDashboardActions);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.class-seat-count.no-seats')?.textContent).toContain('0 left');
    const centralRow = [...compiled.querySelectorAll<HTMLElement>('.train-result-row')]
      .find((row) => row.textContent?.includes('Central Express'));
    expect(centralRow?.querySelector('.trip-duration')?.textContent).toContain('16h 0m');
    expect(compiled.querySelector('.train-result-row')?.textContent).toContain('Zulu Express');
    expect(compiled.querySelector('[name="sortResults"]')).toBeNull();
    expect(compiled.querySelector('[name="filterClass"]')).toBeNull();
    expect(compiled.querySelector('[name="availableOnly"]')).toBeNull();
  });

  it('filters booking history into upcoming, past, and cancelled records', () => {
    const fixture = TestBed.createComponent(BookingHistoryComponent);
    fixture.componentRef.setInput('vm', dashboardViewModel({
      bookings: [
        { bookingId: 1, pnr: 'UPCOMING', userId: 5, trainId: 1, scheduleId: 1, classType: 'SL', journeyDate: '2026-10-03', status: 'Confirmed', numberOfTickets: 1, totalFare: 100, passengers: [] },
        { bookingId: 2, pnr: 'PAST', userId: 5, trainId: 1, scheduleId: 1, classType: 'SL', journeyDate: '2026-09-29', status: 'Confirmed', numberOfTickets: 1, totalFare: 100, passengers: [] },
        { bookingId: 3, pnr: 'CANCELLED', userId: 5, trainId: 1, scheduleId: 1, classType: 'SL', journeyDate: '2026-10-03', status: 'Cancelled', numberOfTickets: 1, totalFare: 100, passengers: [] }
      ]
    }));
    fixture.componentRef.setInput('actions', noActions);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const filters = compiled.querySelectorAll<HTMLButtonElement>('.booking-filters button');

    filters[1].click();
    fixture.detectChanges();
    expect(compiled.querySelectorAll('.booking-row').length).toBe(1);
    expect(compiled.querySelector('.booking-row')?.textContent).toContain('UPCOMING');

    filters[2].click();
    fixture.detectChanges();
    expect(compiled.querySelector('.booking-row')?.textContent).toContain('PAST');

    filters[3].click();
    fixture.detectChanges();
    expect(compiled.querySelector('.booking-row')?.textContent).toContain('CANCELLED');
  });
});
