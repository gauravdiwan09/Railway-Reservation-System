import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal, ViewEncapsulation } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter, forkJoin, Observable } from 'rxjs';
import { AdminDashboardComponent } from './features/admin/admin-dashboard.component';
import { PassengerDashboardComponent } from './features/passenger/passenger-dashboard.component';
import { BookingHistoryComponent } from './features/passenger/booking-history/booking-history.component';
import {
  AdminDashboardActions,
  AdminDashboardViewModel,
  PassengerDashboardActions,
  PassengerDashboardViewModel
} from './features/dashboard-view.models';
import { AuthService } from './core/auth.service';
import { AdminApiService, FareRecord, ScheduleRecord, SeatRecord, TrainRecord } from './core/admin-api.service';
import { BookingService, PassengerBooking, PublicBookingStatus } from './core/booking.service';
import { RazorpayCheckoutService } from './core/razorpay-checkout.service';
import { TrainSearchResult, TrainSearchService } from './core/train-search.service';

interface SeatClassCount {
  classType: string;
  availableSeats: number;
}

@Component({
  selector: 'app-root',
  imports: [AdminDashboardComponent, BookingHistoryComponent, FormsModule, PassengerDashboardComponent, RouterLink, RouterOutlet],
  templateUrl: './features/shell/app-shell.html',
  styleUrl: './dashboard.scss',
  encapsulation: ViewEncapsulation.None
})
export class App implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly trainSearch = inject(TrainSearchService);
  private readonly adminApi = inject(AdminApiService);
  private readonly bookingApi = inject(BookingService);
  private readonly razorpayCheckout = inject(RazorpayCheckoutService);
  private readonly router = inject(Router);

  protected readonly authenticated = computed(() => this.auth.token() !== null);
  protected readonly isAdmin = computed(() => this.auth.claims()?.role?.toLowerCase() === 'admin');
  protected readonly loginOpen = signal(false);
  protected readonly passengerPage = signal<'home' | 'bookings'>('home');
  protected readonly profile = this.auth.profile;
  protected readonly results = signal<TrainSearchResult[]>([]);
  protected readonly classAvailability = signal<Record<number, SeatClassCount[]>>({});
  protected readonly loadingClassAvailability = signal(false);
  protected readonly bookings = signal<PassengerBooking[]>([]);
  protected readonly trains = signal<TrainRecord[]>([]);
  protected readonly schedules = signal<ScheduleRecord[]>([]);
  protected readonly fares = signal<FareRecord[]>([]);
  protected readonly seats = signal<SeatRecord[]>([]);
  protected readonly scheduleCount = computed(() => this.schedules().length);
  protected readonly loadingBookings = signal(false);
  protected readonly searching = signal(false);
  protected readonly signingIn = signal(false);
  protected readonly savingAdmin = signal(false);
  protected readonly adminView = signal<'overview' | 'trains' | 'schedules' | 'fares' | 'seats'>('overview');
  protected readonly selectedDays = signal<string[]>([]);
  protected readonly trainEditId = signal<number | null>(null);
  protected readonly scheduleEditId = signal<number | null>(null);
  protected readonly fareEditId = signal<number | null>(null);
  protected readonly seatEditId = signal<number | null>(null);
  protected readonly adminMessage = signal<{ text: string; tone: 'success' | 'error' } | null>(null);
  protected readonly errorMessage = signal('');
  protected readonly authMode = signal<'login' | 'register'>('login');
  protected readonly selectedJourney = signal<TrainSearchResult | null>(null);
  protected readonly selectedClassType = signal('');
  protected readonly journeyFares = signal<FareRecord[]>([]);
  protected readonly journeySeats = signal<SeatRecord[]>([]);
  protected readonly scheduleOperating = signal<boolean | null>(null);
  protected readonly checkingJourney = signal(false);
  protected readonly submittingBooking = signal(false);
  protected readonly paymentOpen = signal(false);
  protected readonly paymentError = signal('');
  private pendingPaymentId: string | null = null;
  protected readonly journeyMessage = signal('');
  protected readonly passengerDrafts = signal([{ name: '', age: 18, gender: 'Female' }]);
  protected readonly bookingDetails = signal<PassengerBooking | null>(null);
  protected readonly pnrBooking = signal<PublicBookingStatus | null>(null);
  protected readonly pnrMessage = signal('');
  protected readonly pnrError = signal(false);
  protected readonly checkingPnr = signal(false);
  protected readonly availableJourneySeats = computed(() => {
    const classType = this.selectedClassType();
    return this.journeySeats().filter((seat) => seat.classType === classType && seat.isAvailable);
  });
  protected readonly selectedJourneyFare = computed(() =>
    this.journeyFares().find((fare) => fare.classType === this.selectedClassType()) ?? null
  );
  protected readonly journeyFarePerPassenger = computed(() => {
    const fare = this.selectedJourneyFare()?.amount ?? 0;
    const journey = this.selectedJourney();
    if (!journey) return fare;
    const distance = Math.max((journey.endDistanceKm ?? 0) - (journey.startDistanceKm ?? 0), 0);
    const totalDistance = journey.totalDistanceKm ?? journey.endDistanceKm ?? distance;
    return totalDistance > 0 ? Math.round((fare * distance / totalDistance) * 100) / 100 : fare;
  });
  protected readonly bookingTotal = computed(() =>
    this.journeyFarePerPassenger() * this.passengerDrafts().length
  );
  protected readonly today = this.localDate();
  protected readonly weekDays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

  protected readonly adminDashboardViewModel = computed<AdminDashboardViewModel>(() => ({
    today: this.today,
    trains: this.trains(),
    schedules: this.schedules(),
    fares: this.fares(),
    seats: this.seats(),
    scheduleCount: this.scheduleCount(),
    savingAdmin: this.savingAdmin(),
    adminView: this.adminView(),
    selectedDays: this.selectedDays(),
    trainEditId: this.trainEditId(),
    scheduleEditId: this.scheduleEditId(),
    fareEditId: this.fareEditId(),
    seatEditId: this.seatEditId(),
    adminMessage: this.adminMessage(),
    weekDays: this.weekDays,
    selectedTrainId: this.selectedTrainId,
    selectedScheduleId: this.selectedScheduleId,
    inventoryDate: this.inventoryDate,
    trainDraft: this.trainDraft,
    scheduleDraft: this.scheduleDraft,
    fareDraft: this.fareDraft,
    seatDraft: this.seatDraft,
    seatRouteStations: this.selectedTrainRouteStations()
  }));

  protected readonly passengerDashboardViewModel = computed<PassengerDashboardViewModel>(() => ({
    authenticated: this.authenticated(),
    today: this.today,
    journeyForm: { source: this.source, destination: this.destination, journeyDate: this.journeyDate },
    results: this.results(),
    loadingClassAvailability: this.loadingClassAvailability(),
    seatClasses: (scheduleId) => this.seatClasses(scheduleId),
    bookings: this.bookings(),
    loadingBookings: this.loadingBookings(),
    searching: this.searching(),
    errorMessage: this.errorMessage(),
    pnrBooking: this.pnrBooking(),
    pnrMessage: this.pnrMessage(),
    pnrError: this.pnrError(),
    checkingPnr: this.checkingPnr(),
    selectedJourney: this.selectedJourney(),
    checkingJourney: this.checkingJourney(),
    scheduleOperating: this.scheduleOperating(),
    bookingDetails: this.bookingDetails(),
    journeyMessage: this.journeyMessage(),
    journeyFares: this.journeyFares(),
    selectedClassType: this.selectedClassType(),
    availableJourneySeats: this.availableJourneySeats(),
    selectedJourneyFare: this.selectedJourneyFare(),
    bookingTotal: this.bookingTotal(),
    journeyFarePerPassenger: this.journeyFarePerPassenger(),
    passengerDrafts: this.passengerDrafts(),
    submittingBooking: this.submittingBooking(),
    paymentOpen: this.paymentOpen(),
    paymentError: this.paymentError()
  }));

  protected readonly adminDashboardActions: AdminDashboardActions = {
    selectAdminView: (view) => this.selectAdminView(view),
    editTrain: (trainId) => this.editTrain(trainId),
    deleteTrain: (train) => this.deleteTrain(train),
    saveTrain: () => this.saveTrain(),
    cancelTrainEdit: () => this.cancelTrainEdit(),
    addTrainStop: () => this.addTrainStop(),
    removeTrainStop: (index) => this.removeTrainStop(index),
    onTrainSelected: (trainId) => this.onTrainSelected(trainId),
    toggleDay: (day, event) => this.toggleDay(day, event),
    saveSchedule: () => this.saveSchedule(),
    cancelScheduleEdit: () => this.cancelScheduleEdit(),
    editSchedule: (scheduleId) => this.editSchedule(scheduleId),
    deleteSchedule: (scheduleId) => this.deleteSchedule(scheduleId),
    saveFare: () => this.saveFare(),
    cancelFareEdit: () => this.cancelFareEdit(),
    editFare: (fareId) => this.editFare(fareId),
    deleteFare: (fareId) => this.deleteFare(fareId),
    onScheduleSelected: (scheduleId) => this.onScheduleSelected(scheduleId),
    loadSeatInventory: () => this.loadSeatInventory(),
    saveSeat: () => this.saveSeat(),
    cancelSeatEdit: () => this.cancelSeatEdit(),
    editSeat: (seatId) => this.editSeat(seatId),
    deleteSeat: (seatId) => this.deleteSeat(seatId),
    setInventoryDate: (date) => this.setInventoryDate(date)
  };

  protected readonly passengerDashboardActions: PassengerDashboardActions = {
    searchTrains: (source, destination, journeyDate) => this.searchTrains(source, destination, journeyDate),
    findBookingByPnr: (pnr) => this.findBookingByPnr(pnr),
    formatTime: (time) => this.formatTime(time),
    prepareBooking: (train) => this.prepareBooking(train),
    closeBooking: () => this.closeBooking(),
    setSelectedClassType: (classType) => this.selectedClassType.set(classType),
    addPassenger: () => this.addPassenger(),
    removePassenger: (index) => this.removePassenger(index),
    submitBooking: () => this.submitBooking(),
    continueToRazorpay: () => this.continueToRazorpay(),
    closePayment: () => this.closePayment(),
    viewBooking: (bookingId) => this.viewBooking(bookingId),
    downloadTicket: (booking) => this.downloadTicket(booking),
    cancelBooking: (booking) => this.cancelBooking(booking)
  };

  protected email = '';
  protected password = '';
  protected fullName = '';
  protected phone = '';
  protected source = '';
  protected destination = '';
  protected journeyDate = this.today;
  protected selectedTrainId = '';
  protected selectedScheduleId = '';
  protected inventoryDate = this.today;
  protected pnrQuery = '';
  protected trainDraft = { trainNumber: '', trainName: '', source: '', destination: '', totalSeats: 0, totalDistanceKm: 0, intermediateStations: [] as Array<{ stationName: string; distanceFromOriginKm: number }> };
  protected scheduleDraft = { departureTime: '', arrivalTime: '', arrivalNextDay: false };
  protected fareDraft = { classType: 'SL', amount: 0 };
  protected seatDraft = { seatNumber: '', classType: 'SL', startDistanceKm: null as number | null, endDistanceKm: null as number | null };

  ngOnInit(): void {
    this.syncPageFromUrl();
    this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe(() => this.syncPageFromUrl());
    this.loadDashboardData();
  }

  protected toggleLogin(): void {
    this.loginOpen.set(true);
    this.authMode.set('login');
    this.errorMessage.set('');
  }

  protected closeLogin(): void {
    this.loginOpen.set(false);
    this.errorMessage.set('');
  }

  protected showPassengerHome(event?: Event): void {
    event?.preventDefault();
    this.passengerPage.set('home');
    void this.router.navigateByUrl('/home');
  }

  protected showBookingHistory(event?: Event): void {
    event?.preventDefault();
    this.passengerPage.set('bookings');
    void this.router.navigateByUrl('/bookings');
  }

  protected showJourneySearch(event?: Event): void {
    event?.preventDefault();
    this.passengerPage.set('home');
    void this.router.navigateByUrl('/home');
    requestAnimationFrame(() => document.getElementById('journey')?.scrollIntoView({ behavior: 'smooth', block: 'start' }));
  }

  protected toggleAuthMode(): void {
    this.authMode.update((mode) => mode === 'login' ? 'register' : 'login');
    this.errorMessage.set('');
  }

  protected signIn(): void {
    if (!this.email.trim() || !this.password) return;

    this.signingIn.set(true);
    this.errorMessage.set('');
    this.auth.login({ email: this.email.trim(), password: this.password }).subscribe({
      next: () => {
        this.signingIn.set(false);
        this.loginOpen.set(false);
        this.loadDashboardData();
        if (this.isAdmin()) void this.router.navigateByUrl('/admin/overview');
      },
      error: (error: unknown) => {
        this.signingIn.set(false);
        this.errorMessage.set(this.describeError(error));
      }
    });
  }

  protected registerPassenger(): void {
    if (!this.validateRegistration()) return;

    this.signingIn.set(true);
    this.errorMessage.set('');
    this.auth.register({
      fullName: this.fullName.trim(),
      email: this.email.trim(),
      phone: this.phone.trim(),
      password: this.password
    }).subscribe({
      next: () => this.auth.login({ email: this.email.trim(), password: this.password }).subscribe({
        next: () => {
          this.signingIn.set(false);
          this.loginOpen.set(false);
          this.authMode.set('login');
          this.loadDashboardData();
          void this.router.navigateByUrl('/home');
        },
        error: (error: unknown) => {
          this.signingIn.set(false);
          this.authMode.set('login');
          this.errorMessage.set(`Account created. Sign in to continue. ${this.describeError(error)}`);
        }
      }),
      error: (error: unknown) => {
        this.signingIn.set(false);
        this.errorMessage.set(this.describeError(error));
      }
    });
  }

  protected validateRegistration(): boolean {
    if (!this.fullName.trim()) {
      this.errorMessage.set('Enter your full name.');
      return false;
    }
    if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(this.email.trim())) {
      this.errorMessage.set('Enter a valid email address, including a domain extension.');
      return false;
    }
    if (this.password.length < 6) {
      this.errorMessage.set('Password must be at least 6 characters long.');
      return false;
    }

    this.errorMessage.set('');
    return true;
  }

  protected signOut(): void {
    this.auth.logout();
    this.results.set([]);
    this.bookings.set([]);
    this.trains.set([]);
    this.schedules.set([]);
    this.fares.set([]);
    this.seats.set([]);
    this.selectedJourney.set(null);
    this.bookingDetails.set(null);
    this.loginOpen.set(false);
    this.authMode.set('login');
    this.passengerPage.set('home');
    void this.router.navigateByUrl('/home');
  }

  protected searchTrains(source = this.source, destination = this.destination, journeyDate = this.journeyDate): void {
    this.source = source;
    this.destination = destination;
    this.journeyDate = journeyDate;
    if (!this.authenticated()) {
      this.loginOpen.set(true);
      this.errorMessage.set('Sign in to search available trains.');
      return;
    }

    this.searching.set(true);
    this.errorMessage.set('');
    this.results.set([]);
    this.trainSearch.search(this.source.trim(), this.destination.trim(), this.journeyDate).subscribe({
      next: (results) => {
        this.results.set(results);
        this.searching.set(false);
        this.loadClassAvailability(results);
      },
      error: (error: unknown) => {
        this.searching.set(false);
        this.errorMessage.set(this.describeError(error));
      }
    });
  }

  protected formatTime(time: string): string {
    return time.slice(0, 5);
  }

  protected prepareBooking(train: TrainSearchResult): void {
    if (!this.authenticated()) {
      this.loginOpen.set(true);
      this.authMode.set('login');
      this.errorMessage.set('Sign in to reserve a journey.');
      return;
    }

    this.selectedJourney.set(train);
    this.pendingPaymentId = null;
    this.journeyMessage.set('');
    this.bookingDetails.set(null);
    this.journeyFares.set([]);
    this.journeySeats.set([]);
    this.scheduleOperating.set(null);
    this.passengerDrafts.set([{ name: '', age: 18, gender: 'Female' }]);
    this.checkingJourney.set(true);
    this.adminApi.checkScheduleAvailability(train.trainId, train.scheduleId, this.journeyDate).subscribe({
      next: (operating) => {
        this.scheduleOperating.set(operating);
        if (!operating) {
          this.checkingJourney.set(false);
          this.journeyMessage.set('This service does not run on the selected journey date.');
          return;
        }

        forkJoin({
          fares: this.adminApi.getFares(train.trainId),
          seats: this.adminApi.getSeats(train.trainId, train.scheduleId, this.journeyDate, train.startDistanceKm, train.endDistanceKm)
        }).subscribe({
          next: ({ fares, seats }) => {
            this.journeyFares.set(fares);
            this.journeySeats.set(seats);
            this.selectedClassType.set(fares[0]?.classType ?? '');
            this.checkingJourney.set(false);
            if (fares.length === 0) this.journeyMessage.set('No fares are configured for this train yet.');
          },
          error: (error: unknown) => {
            this.checkingJourney.set(false);
            this.journeyMessage.set(this.describeError(error));
          }
        });
      },
      error: (error: unknown) => {
        this.checkingJourney.set(false);
        this.journeyMessage.set(this.describeError(error));
      }
    });
  }

  protected closeBooking(): void {
    this.selectedJourney.set(null);
    this.journeyMessage.set('');
    this.closePayment();
  }

  protected addPassenger(): void {
    if (this.passengerDrafts().length >= 6) return;
    this.passengerDrafts.update((passengers) => [...passengers, { name: '', age: 18, gender: 'Female' }]);
  }

  protected removePassenger(index: number): void {
    if (this.passengerDrafts().length <= 1) return;
    this.passengerDrafts.update((passengers) => passengers.filter((_, passengerIndex) => passengerIndex !== index));
  }

  protected submitBooking(): void {
    const train = this.selectedJourney();
    const passengers = this.passengerDrafts();
    if (!train || !this.selectedClassType() || !this.scheduleOperating()) return;
    if (passengers.some((passenger) => !passenger.name.trim() || passenger.age < 1)) {
      this.journeyMessage.set('Enter a valid name and age for each passenger.');
      return;
    }
    if (this.availableJourneySeats().length < passengers.length) {
      this.journeyMessage.set(`Only ${this.availableJourneySeats().length} seats are available in this class.`);
      return;
    }

    this.submittingBooking.set(false);
    this.journeyMessage.set('');
    this.pendingPaymentId = null;
    this.paymentError.set('');
    this.paymentOpen.set(true);
  }

  protected continueToRazorpay(): void {
    const train = this.selectedJourney();
    const passengers = this.passengerDrafts();
    if (!train || !this.selectedClassType()) return;
    this.submittingBooking.set(true);
    this.paymentError.set('');

    if (this.pendingPaymentId) {
      this.createBookingAfterPayment(train, passengers, this.pendingPaymentId);
      return;
    }

    this.bookingApi.createRazorpayOrder({
      trainId: train.trainId,
      scheduleId: train.scheduleId,
      journeyDate: this.journeyDate,
      classType: this.selectedClassType(),
      passengerCount: passengers.length,
      source: train.source,
      destination: train.destination
    }).subscribe({
      next: (order) => {
        this.razorpayCheckout.openCheckout(order, {
          name: this.profile()?.fullName,
          email: this.auth.claims()?.email ?? undefined,
          contact: this.profile()?.phone
        }).then((result) => {
          if (!result) {
            this.submittingBooking.set(false);
            return;
          }

          this.bookingApi.verifyRazorpayPayment({
            paymentId: order.paymentId,
            orderId: result.razorpay_order_id,
            razorpayPaymentId: result.razorpay_payment_id,
            razorpaySignature: result.razorpay_signature
          }).subscribe({
            next: (verifiedPayment) => {
              this.pendingPaymentId = verifiedPayment.paymentId;
              this.createBookingAfterPayment(train, passengers, verifiedPayment.paymentId);
            },
            error: (error: unknown) => {
              this.submittingBooking.set(false);
              this.paymentError.set(this.describeError(error));
            }
          });
        }).catch((error: unknown) => {
          this.submittingBooking.set(false);
          this.paymentError.set(error instanceof Error ? error.message : 'Razorpay checkout could not be opened.');
        });
      },
      error: (error: unknown) => {
        this.submittingBooking.set(false);
        this.paymentError.set(this.describeError(error));
      }
    });
  }

  protected closePayment(): void {
    this.paymentOpen.set(false);
    this.paymentError.set('');
    this.submittingBooking.set(false);
  }

  private createBookingAfterPayment(
    train: TrainSearchResult,
    passengers: Array<{ name: string; age: number; gender: string }>,
    paymentId: string
  ): void {
    this.bookingApi.create({
      trainId: train.trainId,
      scheduleId: train.scheduleId,
      journeyDate: this.journeyDate,
      source: train.source,
      destination: train.destination,
      startDistanceKm: train.startDistanceKm ?? 0,
      endDistanceKm: train.endDistanceKm ?? train.totalDistanceKm ?? 0,
      classType: this.selectedClassType(),
      paymentId,
      passengers: passengers.map(({ name, age, gender }) => ({ name: name.trim(), age, gender }))
    }).subscribe({
      next: (booking) => {
        this.submittingBooking.set(false);
        this.paymentOpen.set(false);
        this.pendingPaymentId = null;
        this.bookingDetails.set(booking);
        this.journeyMessage.set(`Payment successful. Booking confirmed. Your PNR is ${booking.pnr}.`);
        this.refreshBookings();
        this.adminApi.getSeats(train.trainId, train.scheduleId, this.journeyDate, train.startDistanceKm, train.endDistanceKm).subscribe({
          next: (seats) => this.journeySeats.set(seats)
        });
      },
      error: (error: unknown) => {
        this.submittingBooking.set(false);
        this.paymentError.set(this.describeError(error));
      }
    });
  }

  protected viewBooking(bookingId: number): void {
    this.bookingApi.getById(bookingId).subscribe({
      next: (booking) => {
        if (booking.userId !== this.auth.claims()?.userId) {
          this.errorMessage.set('That booking is not available in your account.');
          return;
        }
        this.bookingDetails.set(booking);
      },
      error: (error: unknown) => this.errorMessage.set(this.describeError(error))
    });
  }

  protected findBookingByPnr(pnr = this.pnrQuery): void {
    this.pnrQuery = pnr;
    if (!this.pnrQuery.trim()) return;
    this.pnrBooking.set(null);
    this.pnrMessage.set('');
    this.pnrError.set(false);
    this.checkingPnr.set(true);
    this.bookingApi.getByPnr(this.pnrQuery.trim()).subscribe({
      next: (status) => {
        this.pnrBooking.set(status);
        this.checkingPnr.set(false);
        this.pnrMessage.set('Reservation status found.');
      },
      error: (error: unknown) => {
        this.checkingPnr.set(false);
        this.pnrError.set(true);
        this.pnrMessage.set(this.describeError(error));
      }
    });
  }

  protected cancelBooking(booking: PassengerBooking): void {
    if (booking.status.toLowerCase() === 'cancelled' || !globalThis.confirm(`Cancel booking ${booking.pnr}?`)) return;
    this.bookingApi.cancel(booking.bookingId).subscribe({
      next: (result) => {
        this.bookingDetails.set(null);
        this.errorMessage.set(result.refundStatus === 'RefundPending'
          ? 'Ticket cancelled and seats released. Razorpay refund is pending; check payment status in Razorpay Dashboard.'
          : '');
        this.refreshBookings();
      },
      error: (error: unknown) => this.errorMessage.set(this.describeError(error))
    });
  }

  protected downloadTicket(booking: PassengerBooking): void {
    this.bookingApi.downloadTicket(booking.bookingId).subscribe({
      next: (blob) => {
        const objectUrl = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = objectUrl;
        link.download = `RailwayTicket_${booking.pnr}.pdf`;
        link.click();
        URL.revokeObjectURL(objectUrl);
      },
      error: (error: unknown) => this.errorMessage.set(this.describeError(error))
    });
  }

  protected selectAdminView(view: 'overview' | 'trains' | 'schedules' | 'fares' | 'seats'): void {
    this.adminView.set(view);
    void this.router.navigateByUrl(`/admin/${view}`);
    this.adminMessage.set(null);
    if (this.selectedTrainId && view !== 'overview' && view !== 'trains') {
      this.onTrainSelected(this.selectedTrainId);
    }
  }

  protected onTrainSelected(trainId: string): void {
    this.selectedTrainId = trainId;
    this.selectedScheduleId = '';
    this.schedules.set([]);
    this.fares.set([]);
    this.seats.set([]);
    if (!trainId) return;

    this.adminApi.getSchedules(Number(trainId)).subscribe({
      next: (schedules) => this.schedules.set(schedules),
      error: (error: unknown) => this.showAdminError(error)
    });
    this.adminApi.getFares(Number(trainId)).subscribe({
      next: (fares) => this.fares.set(fares),
      error: (error: unknown) => this.showAdminError(error)
    });
  }

  protected onScheduleSelected(scheduleId: string): void {
    this.selectedScheduleId = scheduleId;
    this.seats.set([]);
    if (!this.selectedTrainId || !scheduleId) return;
    this.adminApi.getSeats(Number(this.selectedTrainId), Number(scheduleId), this.inventoryDate).subscribe({
      next: (seats) => this.seats.set(seats),
      error: (error: unknown) => this.showAdminError(error)
    });
  }

  protected loadSeatInventory(): void {
    this.onScheduleSelected(this.selectedScheduleId);
  }

  protected setInventoryDate(date: string): void {
    this.inventoryDate = date;
    this.loadSeatInventory();
  }

  protected toggleDay(day: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selectedDays.update((days) => checked
      ? [...days, day]
      : days.filter((selectedDay) => selectedDay !== day));
  }

  protected saveTrain(): void {
    const editId = this.trainEditId();
    const request = editId
      ? this.adminApi.updateTrain(editId, this.trainDraft)
      : this.adminApi.addTrain(this.trainDraft);
    this.runAdminSave(request, () => {
      this.trainDraft = { trainNumber: '', trainName: '', source: '', destination: '', totalSeats: 0, totalDistanceKm: 0, intermediateStations: [] };
      this.trainEditId.set(null);
      this.loadTrains();
    }, editId ? 'Train updated successfully.' : 'Train registered successfully.');
  }

  protected addTrainStop(): void {
    this.trainDraft.intermediateStations.push({ stationName: '', distanceFromOriginKm: 0 });
  }

  protected removeTrainStop(index: number): void {
    this.trainDraft.intermediateStations.splice(index, 1);
  }

  protected saveSchedule(): void {
    if (!this.selectedTrainId || this.selectedDays().length === 0) {
      this.adminMessage.set({ text: 'Select a train and at least one operating day.', tone: 'error' });
      return;
    }

    const request = {
      departureTime: `${this.scheduleDraft.departureTime}:00`,
      arrivalTime: `${this.scheduleDraft.arrivalTime}:00`,
      arrivalNextDay: this.scheduleDraft.arrivalNextDay,
      daysOfOperation: this.selectedDays().join(' ')
    };
    const editId = this.scheduleEditId();
    const operation = editId
      ? this.adminApi.updateSchedule(editId, request)
      : this.adminApi.addSchedule(Number(this.selectedTrainId), request);
    this.runAdminSave(operation, () => {
      this.scheduleDraft = { departureTime: '', arrivalTime: '', arrivalNextDay: false };
      this.selectedDays.set([]);
      this.scheduleEditId.set(null);
      this.onTrainSelected(this.selectedTrainId);
    }, editId ? 'Schedule updated successfully.' : 'Schedule added successfully.');
  }

  protected saveFare(): void {
    if (!this.selectedTrainId) {
      this.adminMessage.set({ text: 'Select a train before adding a fare.', tone: 'error' });
      return;
    }

    const editId = this.fareEditId();
    const operation = editId
      ? this.adminApi.updateFare(editId, this.fareDraft)
      : this.adminApi.addFare(Number(this.selectedTrainId), this.fareDraft);
    this.runAdminSave(operation, () => {
      this.fareDraft = { classType: 'SL', amount: 0 };
      this.fareEditId.set(null);
      this.onTrainSelected(this.selectedTrainId);
    }, editId ? 'Fare updated successfully.' : 'Fare added successfully.');
  }

  protected saveSeat(): void {
    if (!this.selectedTrainId || !this.selectedScheduleId) {
      this.adminMessage.set({ text: 'Select both a train and a schedule before adding a seat.', tone: 'error' });
      return;
    }

    const editId = this.seatEditId();
    const operation = editId
      ? this.adminApi.updateSeat(editId, this.seatDraft)
      : this.adminApi.addSeat(Number(this.selectedTrainId), Number(this.selectedScheduleId), this.seatDraft);
    this.runAdminSave(operation, () => {
      this.seatDraft = { seatNumber: '', classType: 'SL', startDistanceKm: null, endDistanceKm: null };
      this.seatEditId.set(null);
      this.onScheduleSelected(this.selectedScheduleId);
    }, editId ? 'Seat updated successfully.' : 'Seat added successfully.');
  }

  protected editTrain(trainId: number): void {
    this.adminApi.getTrain(trainId).subscribe({
      next: (train) => {
        this.trainDraft = { trainNumber: train.trainNumber, trainName: train.trainName, source: train.source, destination: train.destination, totalSeats: train.totalSeats, totalDistanceKm: train.totalDistanceKm ?? 0, intermediateStations: train.intermediateStations?.map((station) => ({ ...station })) ?? [] };
        this.trainEditId.set(trainId);
        this.selectAdminView('trains');
      },
      error: (error: unknown) => this.showAdminError(error)
    });
  }

  protected deleteTrain(train: TrainRecord): void {
    if (!globalThis.confirm(`Delete ${train.trainNumber} · ${train.trainName}?`)) return;
    this.adminApi.deleteTrain(train.trainId).subscribe({
      next: () => this.loadTrains(),
      error: (error: unknown) => this.showAdminError(error)
    });
  }

  protected editSchedule(scheduleId: number): void {
    this.adminApi.getSchedule(scheduleId).subscribe({
      next: (schedule) => {
        this.selectedTrainId = String(schedule.trainId);
        this.scheduleDraft = { departureTime: schedule.departureTime.slice(0, 5), arrivalTime: schedule.arrivalTime.slice(0, 5), arrivalNextDay: schedule.arrivalNextDay };
        this.selectedDays.set(schedule.daysOfOperation.split(' ').filter(Boolean));
        this.scheduleEditId.set(scheduleId);
        this.onTrainSelected(this.selectedTrainId);
      },
      error: (error: unknown) => this.showAdminError(error)
    });
  }

  protected deleteSchedule(scheduleId: number): void {
    if (!globalThis.confirm(`Delete schedule ${scheduleId}?`)) return;
    this.adminApi.deleteSchedule(scheduleId).subscribe({
      next: () => this.onTrainSelected(this.selectedTrainId),
      error: (error: unknown) => this.showAdminError(error)
    });
  }

  protected editFare(fareId: number): void {
    this.adminApi.getFare(fareId).subscribe({
      next: (fare) => {
        this.selectedTrainId = String(fare.trainId);
        this.fareDraft = { classType: fare.classType, amount: fare.amount };
        this.fareEditId.set(fareId);
        this.onTrainSelected(this.selectedTrainId);
      },
      error: (error: unknown) => this.showAdminError(error)
    });
  }

  protected deleteFare(fareId: number): void {
    if (!globalThis.confirm(`Delete fare ${fareId}?`)) return;
    this.adminApi.deleteFare(fareId).subscribe({
      next: () => this.onTrainSelected(this.selectedTrainId),
      error: (error: unknown) => this.showAdminError(error)
    });
  }

  protected editSeat(seatId: number): void {
    this.adminApi.getSeat(seatId).subscribe({
      next: (seat) => {
        this.selectedTrainId = String(seat.trainId);
        this.selectedScheduleId = String(seat.scheduleId);
        this.seatDraft = { seatNumber: seat.seatNumber, classType: seat.classType, startDistanceKm: seat.startDistanceKm ?? null, endDistanceKm: seat.endDistanceKm ?? null };
        this.seatEditId.set(seatId);
        this.adminApi.getSchedules(seat.trainId).subscribe({
          next: (schedules) => {
            this.schedules.set(schedules);
            this.onScheduleSelected(this.selectedScheduleId);
          },
          error: (error: unknown) => this.showAdminError(error)
        });
      },
      error: (error: unknown) => this.showAdminError(error)
    });
  }

  protected deleteSeat(seatId: number): void {
    if (!globalThis.confirm(`Delete seat ${seatId}?`)) return;
    this.adminApi.deleteSeat(seatId).subscribe({
      next: () => this.onScheduleSelected(this.selectedScheduleId),
      error: (error: unknown) => this.showAdminError(error)
    });
  }

  protected cancelTrainEdit(): void {
    this.trainEditId.set(null);
    this.trainDraft = { trainNumber: '', trainName: '', source: '', destination: '', totalSeats: 0, totalDistanceKm: 0, intermediateStations: [] };
  }

  protected cancelScheduleEdit(): void {
    this.scheduleEditId.set(null);
    this.scheduleDraft = { departureTime: '', arrivalTime: '', arrivalNextDay: false };
    this.selectedDays.set([]);
  }

  protected cancelFareEdit(): void {
    this.fareEditId.set(null);
    this.fareDraft = { classType: 'SL', amount: 0 };
  }

  protected cancelSeatEdit(): void {
    this.seatEditId.set(null);
    this.seatDraft = { seatNumber: '', classType: 'SL', startDistanceKm: null, endDistanceKm: null };
  }

  private loadDashboardData(): void {
    if (!this.authenticated()) return;
    this.loadUserProfile();
    if (this.isAdmin()) {
      this.loadTrains();
      return;
    }

    const userId = this.auth.claims()?.userId;
    if (userId === null || userId === undefined) return;

    this.refreshBookings();
  }

  private loadUserProfile(): void {
    const userId = this.auth.claims()?.userId;
    if (userId === null || userId === undefined) return;

    this.auth.getUserDetails(userId).subscribe({
      next: (profile) => this.auth.profile.set(profile),
      error: (error: unknown) => {
        if (this.isAdmin()) {
          this.showAdminError(error);
        } else {
          this.errorMessage.set(this.describeError(error));
        }
      }
    });
  }

  private loadClassAvailability(results: TrainSearchResult[]): void {
    this.classAvailability.set({});
    if (results.length === 0) return;

    this.loadingClassAvailability.set(true);
    forkJoin(results.map((train) => forkJoin({
      seats: this.adminApi.getSeats(train.trainId, train.scheduleId, this.journeyDate, train.startDistanceKm, train.endDistanceKm),
      fares: this.adminApi.getFares(train.trainId)
    }))).subscribe({
      next: (inventories) => {
        const availability: Record<number, SeatClassCount[]> = {};
        results.forEach((train, index) => {
          const { fares, seats } = inventories[index];
          const classTypes = new Set(fares.map((fare) => fare.classType));
          seats.forEach((seat) => classTypes.add(seat.classType));
          availability[train.scheduleId] = Array.from(classTypes, (classType) => ({
            classType,
            availableSeats: seats.filter((seat) => seat.classType === classType && seat.isAvailable).length
          }));
        });
        this.classAvailability.set(availability);
        this.loadingClassAvailability.set(false);
      },
      error: (error: unknown) => {
        this.loadingClassAvailability.set(false);
        this.errorMessage.set(this.describeError(error));
      }
    });
  }

  protected seatClasses(scheduleId: number): SeatClassCount[] {
    return this.classAvailability()[scheduleId] ?? [];
  }

  private refreshBookings(): void {
    const userId = this.auth.claims()?.userId;
    if (userId === null || userId === undefined || this.isAdmin()) return;
    this.loadingBookings.set(true);
    this.bookingApi.getForPassenger(userId).subscribe({
      next: (bookings) => {
        this.bookings.set(bookings);
        this.loadingBookings.set(false);
      },
      error: (error: unknown) => {
        this.loadingBookings.set(false);
        this.errorMessage.set(this.describeError(error));
      }
    });
  }

  private loadTrains(): void {
    this.adminApi.getTrains().subscribe({
      next: (trains) => this.trains.set(trains),
      error: (error: unknown) => this.showAdminError(error)
    });
  }

  private syncPageFromUrl(): void {
    const path = this.router.url.split('?')[0].split('/').filter(Boolean);
    if (path[0] === 'admin') {
      if (!this.isAdmin()) {
        void this.router.navigateByUrl('/home');
        return;
      }
      const section = path[1];
      if (section === 'overview' || section === 'trains' || section === 'schedules' || section === 'fares' || section === 'seats') {
        this.adminView.set(section);
      }
      return;
    }

    this.passengerPage.set(path[0] === 'bookings' ? 'bookings' : 'home');
    if (this.isAdmin() && path[0] !== 'admin') void this.router.navigateByUrl('/admin/overview');
  }

  private selectedTrainRouteStations(): Array<{ stationName: string; distanceFromOriginKm: number }> {
    const train = this.trains().find((item) => String(item.trainId) === this.selectedTrainId);
    if (!train) return [];
    return [
      { stationName: train.source, distanceFromOriginKm: 0 },
      ...(train.intermediateStations ?? []),
      { stationName: train.destination, distanceFromOriginKm: train.totalDistanceKm ?? 0 }
    ];
  }

  private runAdminSave(request: Observable<unknown>, onSuccess: () => void, message: string): void {
    this.savingAdmin.set(true);
    this.adminMessage.set(null);
    request.subscribe({
      next: () => {
        this.savingAdmin.set(false);
        this.adminMessage.set({ text: message, tone: 'success' });
        onSuccess();
      },
      error: (error: unknown) => {
        this.savingAdmin.set(false);
        this.showAdminError(error);
      }
    });
  }

  private showAdminError(error: unknown): void {
    this.adminMessage.set({ text: this.describeError(error), tone: 'error' });
  }

  private localDate(): string {
    const now = new Date();
    const offset = now.getTimezoneOffset() * 60_000;
    return new Date(now.getTime() - offset).toISOString().slice(0, 10);
  }

  private describeError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as {
        message?: string;
        detail?: string;
        title?: string;
        errors?: Record<string, string[]>;
      } | null;
      const validationMessage = body?.errors
        ? Object.values(body.errors).flat().find((message) => Boolean(message))
        : undefined;
      if (validationMessage || body?.detail || body?.message) {
        return validationMessage ?? body?.detail ?? body?.message ?? '';
      }
      if (body?.title && body.title !== 'Internal Server Error') return body.title;

      return ({
        0: 'Unable to reach the railway services. Check that the gateway is running.',
        400: 'Please check the information and try again.',
        401: 'Your session has expired or the credentials are incorrect. Please sign in again.',
        403: 'Your account does not have permission to perform this action.',
        404: 'The requested railway record could not be found.',
        409: 'This request conflicts with an existing booking or record.',
        500: 'The railway service encountered an error. Please try again shortly.'
      } as Record<number, string>)[error.status] ?? 'The request could not be completed. Please try again.';
    }

    return 'Something went wrong. Please try again.';
  }
}
