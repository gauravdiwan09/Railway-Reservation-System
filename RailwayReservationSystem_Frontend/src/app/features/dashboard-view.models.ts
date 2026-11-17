import { FareRecord, ScheduleRecord, SeatRecord, TrainRecord } from '../core/admin-api.service';
import { PassengerBooking, PublicBookingStatus } from '../core/booking.service';
import { TrainSearchResult } from '../core/train-search.service';

export interface SeatClassCount {
  classType: string;
  availableSeats: number;
}

export type AdminViewName = 'overview' | 'trains' | 'schedules' | 'fares' | 'seats';

export interface RouteStationDraft {
  stationName: string;
  distanceFromOriginKm: number;
}

export interface AdminDashboardViewModel {
  today: string;
  trains: TrainRecord[];
  schedules: ScheduleRecord[];
  fares: FareRecord[];
  seats: SeatRecord[];
  scheduleCount: number;
  savingAdmin: boolean;
  adminView: AdminViewName;
  selectedDays: string[];
  trainEditId: number | null;
  scheduleEditId: number | null;
  fareEditId: number | null;
  seatEditId: number | null;
  adminMessage: { text: string; tone: 'success' | 'error' } | null;
  weekDays: string[];
  selectedTrainId: string;
  selectedScheduleId: string;
  inventoryDate: string;
  trainDraft: { trainNumber: string; trainName: string; source: string; destination: string; totalSeats: number; totalDistanceKm: number; intermediateStations: RouteStationDraft[] };
  scheduleDraft: { departureTime: string; arrivalTime: string; arrivalNextDay: boolean };
  fareDraft: { classType: string; amount: number };
  seatDraft: { seatNumber: string; classType: string; startDistanceKm: number | null; endDistanceKm: number | null };
  seatRouteStations: RouteStationDraft[];
}

export interface AdminDashboardActions {
  selectAdminView(view: AdminViewName): void;
  editTrain(trainId: number): void;
  deleteTrain(train: TrainRecord): void;
  saveTrain(): void;
  cancelTrainEdit(): void;
  addTrainStop(): void;
  removeTrainStop(index: number): void;
  onTrainSelected(trainId: string): void;
  toggleDay(day: string, event: Event): void;
  saveSchedule(): void;
  cancelScheduleEdit(): void;
  editSchedule(scheduleId: number): void;
  deleteSchedule(scheduleId: number): void;
  saveFare(): void;
  cancelFareEdit(): void;
  editFare(fareId: number): void;
  deleteFare(fareId: number): void;
  onScheduleSelected(scheduleId: string): void;
  setInventoryDate(date: string): void;
  loadSeatInventory(): void;
  saveSeat(): void;
  cancelSeatEdit(): void;
  editSeat(seatId: number): void;
  deleteSeat(seatId: number): void;
}

export interface PassengerDashboardViewModel {
  authenticated: boolean;
  today: string;
  journeyForm: { source: string; destination: string; journeyDate: string };
  results: TrainSearchResult[];
  loadingClassAvailability: boolean;
  seatClasses(scheduleId: number): SeatClassCount[];
  bookings: PassengerBooking[];
  loadingBookings: boolean;
  searching: boolean;
  errorMessage: string;
  pnrBooking: PublicBookingStatus | null;
  pnrMessage: string;
  pnrError: boolean;
  checkingPnr: boolean;
  selectedJourney: TrainSearchResult | null;
  checkingJourney: boolean;
  scheduleOperating: boolean | null;
  bookingDetails: PassengerBooking | null;
  journeyMessage: string;
  journeyFares: FareRecord[];
  selectedClassType: string;
  availableJourneySeats: SeatRecord[];
  selectedJourneyFare: FareRecord | null;
  bookingTotal: number;
  journeyFarePerPassenger: number;
  passengerDrafts: Array<{ name: string; age: number; gender: string }>;
  submittingBooking: boolean;
  paymentOpen: boolean;
  paymentError: string;
}

export interface PassengerDashboardActions {
  searchTrains(source: string, destination: string, journeyDate: string): void;
  findBookingByPnr(pnr: string): void;
  formatTime(time: string): string;
  prepareBooking(train: TrainSearchResult): void;
  closeBooking(): void;
  setSelectedClassType(classType: string): void;
  addPassenger(): void;
  removePassenger(index: number): void;
  submitBooking(): void;
  continueToRazorpay(): void;
  closePayment(): void;
  viewBooking(bookingId: number): void;
  downloadTicket(booking: PassengerBooking): void;
  cancelBooking(booking: PassengerBooking): void;
}
