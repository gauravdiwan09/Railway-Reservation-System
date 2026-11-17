import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { routes } from './app.routes';
import { RazorpayCheckoutService } from './core/razorpay-checkout.service';

const adminToken = `header.${btoa(JSON.stringify({ nameid: '7', role: 'Admin' }))}.signature`;
const passengerToken = `header.${btoa(JSON.stringify({ nameid: '12', role: 'Passenger' }))}.signature`;

describe('App', () => {
  beforeEach(async () => {
    sessionStorage.removeItem('railway-access-token');
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter(routes),
        {
          provide: RazorpayCheckoutService,
          useValue: {
            openCheckout: jasmine.createSpy('openCheckout').and.resolveTo({
              razorpay_payment_id: 'pay_123',
              razorpay_order_id: 'order_123',
              razorpay_signature: 'verified_signature'
            })
          }
        }
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the train search page', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Every journey');
  });

  it('should render the admin dashboard for an admin token', () => {
    sessionStorage.setItem('railway-access-token', adminToken);
    const fixture = TestBed.createComponent(App);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Administrator dashboard');
    http.expectOne('/api/users/7').flush({ userId: 7, fullName: 'Railway Admin', email: 'admin@example.com', phone: '', roleName: 'Admin', createdAt: '2026-09-30' });
    http.expectOne('/api/trains').flush([]);
    fixture.detectChanges();
    expect(compiled.querySelector('.account-name')?.textContent).toContain('Railway Admin');
  });

  it('opens booking history separately and returns to journey search from the navbar', async () => {
    sessionStorage.setItem('railway-access-token', passengerToken);
    const fixture = TestBed.createComponent(App);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/users/12').flush({ userId: 12, fullName: 'Casey Passenger', email: 'casey@example.com', phone: '', roleName: 'Passenger', createdAt: '2026-09-30' });
    http.expectOne('/api/Bookings/user/12').flush([]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.passenger-hero')).not.toBeNull();
    expect(compiled.querySelector('.booking-panel')).toBeNull();
    compiled.querySelector<HTMLAnchorElement>('a[routerLink="/bookings"]')!.click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.querySelector('.booking-panel')).not.toBeNull();
    expect(compiled.querySelector('.passenger-hero')).toBeNull();
    expect(compiled.querySelector('.journey-panel')).toBeNull();

    compiled.querySelector<HTMLAnchorElement>('.nav-inner a[routerLink="/home"]')!.click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.querySelector('.passenger-hero')).not.toBeNull();
    expect(compiled.querySelector('.booking-panel')).toBeNull();

    compiled.querySelector<HTMLAnchorElement>('a[routerLink="/bookings"]')!.click();
    await fixture.whenStable();
    fixture.detectChanges();
    compiled.querySelector<HTMLAnchorElement>('a[fragment="journey"]')!.click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.querySelector('.passenger-hero')).not.toBeNull();
    expect(compiled.querySelector('.journey-panel#journey')).not.toBeNull();
    expect(compiled.querySelector('.booking-panel')).toBeNull();
  });

  it('opens a centered auth dialog only from the navigation and switches to registration', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.auth-modal')).toBeNull();

    compiled.querySelector<HTMLButtonElement>('.nav-auth-button')!.click();
    fixture.detectChanges();
    expect(compiled.querySelector('[role="dialog"]')).not.toBeNull();
    expect(compiled.querySelector('input[name="email"]')).not.toBeNull();

    compiled.querySelector<HTMLButtonElement>('.modal-switch')!.click();
    fixture.detectChanges();
    expect(compiled.querySelector('input[name="fullName"]')).not.toBeNull();

    compiled.querySelector<HTMLButtonElement>('.modal-close')!.click();
    fixture.detectChanges();
    expect(compiled.querySelector('.auth-modal')).toBeNull();
  });

  it('shows backend-aligned registration errors before sending a request', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('.nav-auth-button')!.click();
    fixture.detectChanges();
    compiled.querySelector<HTMLButtonElement>('.modal-switch')!.click();
    fixture.detectChanges();

    const app = fixture.componentInstance as unknown as {
      fullName: string;
      email: string;
      password: string;
      validateRegistration(): boolean;
    };
    app.fullName = 'Casey Passenger';
    app.email = 'casey@example.com';
    app.password = '12345';
    expect(app.validateRegistration()).toBeFalse();
    fixture.detectChanges();
    expect(compiled.querySelector('.modal-error')?.textContent).toContain('at least 6 characters');

    app.password = '123456';
    expect(app.validateRegistration()).toBeTrue();
    app.email = 'not-an-email';
    expect(app.validateRegistration()).toBeFalse();
    fixture.detectChanges();
    expect(compiled.querySelector('.modal-error')?.textContent).toContain('valid email address');
  });

  it('checks a PNR from the signed-out landing page without opening auth', () => {
    const fixture = TestBed.createComponent(App);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    const app = fixture.componentInstance as unknown as { pnrQuery: string; findBookingByPnr(): void };
    app.pnrQuery = 'PNR123';
    app.findBookingByPnr();
    const request = http.expectOne('/api/Bookings/pnr/PNR123');
    expect(request.request.method).toBe('GET');
    request.flush({ pnr: 'PNR123', journeyDate: '2026-10-02', status: 'Confirmed' });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.public-status-result')?.textContent).toContain('Confirmed');
    expect(compiled.querySelector('.auth-modal')).toBeNull();

    app.pnrQuery = 'NOTFOUND';
    app.findBookingByPnr();
    http.expectOne('/api/Bookings/pnr/NOTFOUND').flush({
      type: 'about:blank',
      title: 'Booking not found',
      status: 404,
      detail: 'No reservation exists for this PNR.'
    }, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();
    expect(compiled.querySelector('.pnr-feedback')?.textContent).toContain('No reservation exists for this PNR.');
    expect(compiled.querySelector('.pnr-feedback')?.classList.contains('pnr-error')).toBeTrue();
  });

  it('checks the schedule, fares, and seats before posting a booking', async () => {
    sessionStorage.setItem('railway-access-token', passengerToken);
    const fixture = TestBed.createComponent(App);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/users/12').flush({ userId: 12, fullName: 'Casey Passenger', email: 'casey@example.com', phone: '', roleName: 'Passenger', createdAt: '2026-09-30' });
    http.expectOne('/api/Bookings/user/12').flush([]);
    expect((fixture.nativeElement as HTMLElement).querySelector('.status-lookup')).toBeNull();

    const app = fixture.componentInstance as unknown as {
      source: string;
      destination: string;
      journeyDate: string;
      selectedClassType: { set(value: string): void };
      passengerDrafts: { set(value: Array<{ name: string; age: number; gender: string }>): void };
      journeySeats: { set(value: Array<{ seatId: number; trainId: number; scheduleId: number; seatNumber: string; classType: string; isAvailable: boolean }>): void };
      searchTrains(): void;
      prepareBooking(train: {
        trainId: number;
        trainNumber: string;
        trainName: string;
        source: string;
        destination: string;
        totalSeats: number;
        startDistanceKm?: number;
        endDistanceKm?: number;
        totalDistanceKm?: number;
        scheduleId: number;
        departureTime: string;
        arrivalTime: string;
        arrivalNextDay: boolean;
      }): void;
      submitBooking(): void;
      continueToRazorpay(): void;
    };
    app.source = 'Mangaluru';
    app.destination = 'Bengaluru';
    app.searchTrains();

    const search = http.expectOne((request) => request.url === '/api/trains/search');
    expect(search.request.params.get('source')).toBe('Mangaluru');
    expect(search.request.params.get('destination')).toBe('Bengaluru');
    const journeyDate = app.journeyDate;
    search.flush([{
      trainId: 41,
      trainNumber: '7001',
      trainName: 'Coastal Express',
      source: 'Mangaluru',
      destination: 'Bengaluru',
      totalSeats: 200,
      startDistanceKm: 0,
      endDistanceKm: 100,
      totalDistanceKm: 100,
      scheduleId: 81,
      departureTime: '08:00:00',
      arrivalTime: '12:00:00',
      arrivalNextDay: false
    }]);
    http.expectOne((request) => request.url === '/api/trains/41/seats' && request.params.get('scheduleId') === '81').flush([
      { seatId: 1, trainId: 41, scheduleId: 81, seatNumber: 'A1', classType: 'AC', isAvailable: true },
      { seatId: 2, trainId: 41, scheduleId: 81, seatNumber: 'S1', classType: 'SL', isAvailable: false }
    ]);
    http.expectOne('/api/trains/41/fare').flush([
      { fareId: 1, trainId: 41, classType: 'AC', amount: 900 },
      { fareId: 2, trainId: 41, classType: 'SL', amount: 500 }
    ]);
    fixture.detectChanges();
    const searchResults = fixture.nativeElement as HTMLElement;
    expect(searchResults.querySelector('.class-availability')?.textContent).toContain('SL');
    expect(searchResults.querySelector('.class-seat-count.no-seats')?.textContent).toContain('0 left');
    expect(searchResults.querySelector('.row-primary')).toBeNull();
    expect(searchResults.querySelector('.train-result-row')).not.toBeNull();
    expect(searchResults.querySelector('.results-panel')).not.toBeNull();
    expect(searchResults.querySelector('.booking-panel')).toBeNull();

    app.prepareBooking({
      trainId: 41,
      trainNumber: '7001',
      trainName: 'Coastal Express',
      source: 'Mangaluru',
      destination: 'Bengaluru',
      totalSeats: 200,
      startDistanceKm: 0,
      endDistanceKm: 100,
      totalDistanceKm: 100,
      scheduleId: 81,
      departureTime: '08:00:00',
      arrivalTime: '12:00:00',
      arrivalNextDay: false
    });
    const availability = http.expectOne((request) => request.url.endsWith('/availability'));
    expect(availability.request.params.get('journeyDate')).toBe(journeyDate);
    availability.flush(true);
    http.expectOne('/api/trains/41/fare').flush([
      { fareId: 1, trainId: 41, classType: 'AC', amount: 900 },
      { fareId: 2, trainId: 41, classType: 'SL', amount: 500 }
    ]);
    http.expectOne((request) => request.url === '/api/trains/41/seats' && request.params.get('scheduleId') === '81').flush([
      { seatId: 1, trainId: 41, scheduleId: 81, seatNumber: 'A1', classType: 'AC', isAvailable: true },
      { seatId: 2, trainId: 41, scheduleId: 81, seatNumber: 'S1', classType: 'SL', isAvailable: true }
    ]);
    fixture.detectChanges();

    app.selectedClassType.set('SL');
    app.passengerDrafts.set([{ name: 'Casey Passenger', age: 18, gender: 'Female' }]);
    app.journeySeats.set([{ seatId: 2, trainId: 41, scheduleId: 81, seatNumber: 'S1', classType: 'SL', isAvailable: false }]);
    fixture.detectChanges();
    const bookingReview = (fixture.nativeElement as HTMLElement).querySelector('.booking-review');
    expect(bookingReview?.textContent).toContain('1 passenger(s)');
    expect(bookingReview?.querySelector('.review-total')?.textContent).toContain('500');
    expect((fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.reservation-form button[type="submit"]')?.disabled).toBeFalse();
    app.submitBooking();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('.reservation-form .error-message')?.textContent).toContain('Only 0 seats are available');

    app.journeySeats.set([{ seatId: 2, trainId: 41, scheduleId: 81, seatNumber: 'S1', classType: 'SL', isAvailable: true }]);
    app.submitBooking();
    app.continueToRazorpay();

    const payment = http.expectOne({ method: 'POST', url: '/api/payments/razorpay/orders' });
    expect(payment.request.body).toEqual({
      trainId: 41,
      scheduleId: 81,
      journeyDate,
      classType: 'SL',
      passengerCount: 1,
      source: 'Mangaluru',
      destination: 'Bengaluru'
    });
    payment.flush({ paymentId: 'internal-payment-1', orderId: 'order_123', keyId: 'rzp_test_public', amount: 50000, currency: 'INR' });
    await fixture.whenStable();

    const verification = http.expectOne({ method: 'POST', url: '/api/payments/razorpay/verify' });
    expect(verification.request.body).toEqual({
      paymentId: 'internal-payment-1',
      orderId: 'order_123',
      razorpayPaymentId: 'pay_123',
      razorpaySignature: 'verified_signature'
    });
    verification.flush({ paymentId: 'internal-payment-1', status: 'Captured', amount: 500 });

    const booking = http.expectOne({ method: 'POST', url: '/api/Bookings' });
    expect(booking.request.body).toEqual({
      trainId: 41,
      scheduleId: 81,
      journeyDate,
      source: 'Mangaluru',
      destination: 'Bengaluru',
      startDistanceKm: 0,
      endDistanceKm: 100,
      classType: 'SL',
      paymentId: 'internal-payment-1',
      passengers: [{ name: 'Casey Passenger', age: 18, gender: 'Female' }]
    });
    booking.flush({ bookingId: 15, pnr: 'PNR123', userId: 12, journeyDate, classType: 'SL', status: 'Confirmed', totalFare: 500, passengers: [] });
    http.expectOne('/api/Bookings/user/12').flush([]);
    http.expectOne((request) => request.url === '/api/trains/41/seats' && request.params.get('scheduleId') === '81').flush([]);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.booking-confirmation')?.textContent).toContain('PNR123');
  });
});
