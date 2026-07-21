import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { Make, VehicleModel, VehicleType } from './vehicle';

/**
 * Talks to the Car Lookup API. The URL is relative: in development the Angular dev server
 * proxies /api to the backend, and in Docker nginx does the same, so no environment-specific
 * base URL is needed.
 */
@Injectable({ providedIn: 'root' })
export class VehicleCatalog {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/vehicles';

  /** Makes matching a search term, ranked and capped by the server. */
  searchMakes(query: string, limit = 25): Observable<Make[]> {
    const params = new HttpParams().set('query', query).set('limit', limit);

    return this.http
      .get<Make[]>(`${this.baseUrl}/makes`, { params })
      .pipe(catchError(this.toUserFacingError));
  }

  /** Vehicle types produced by a make. */
  vehicleTypes(makeId: number): Observable<VehicleType[]> {
    return this.http
      .get<VehicleType[]>(`${this.baseUrl}/makes/${makeId}/vehicle-types`)
      .pipe(catchError(this.toUserFacingError));
  }

  /** Models for a make and model year, optionally narrowed to one vehicle type. */
  models(makeId: number, year: number, vehicleType?: string): Observable<VehicleModel[]> {
    let params = new HttpParams().set('year', year);

    if (vehicleType) {
      params = params.set('vehicleType', vehicleType);
    }

    return this.http
      .get<VehicleModel[]>(`${this.baseUrl}/makes/${makeId}/models`, { params })
      .pipe(catchError(this.toUserFacingError));
  }

  /** Selectable model years, newest first. */
  years(): Observable<number[]> {
    return this.http.get<number[]>(`${this.baseUrl}/years`).pipe(catchError(this.toUserFacingError));
  }

  /**
   * The API answers failures with an RFC 7807 problem document; surface its detail rather than
   * a raw status code.
   */
  private toUserFacingError(error: HttpErrorResponse): Observable<never> {
    const detail =
      error.error?.detail ??
      (error.status === 0
        ? 'Could not reach the Car Lookup service. Check that it is running.'
        : 'Something went wrong while loading vehicle data.');

    return throwError(() => new Error(detail));
  }
}
