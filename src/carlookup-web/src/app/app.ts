import { Component, ChangeDetectionStrategy, inject, signal, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, forkJoin, of } from 'rxjs';
import { MakePicker } from './make-picker/make-picker';
import { Make, VehicleModel, VehicleType } from './vehicle';
import { VehicleCatalog } from './vehicle-catalog';

@Component({
  selector: 'app-root',
  imports: [MakePicker],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  private readonly catalog = inject(VehicleCatalog);
  private readonly makePicker = viewChild.required(MakePicker);

  /** Falls back to a locally generated range if the API cannot be reached on first paint. */
  protected readonly years = toSignal(
    this.catalog.years().pipe(catchError(() => of(App.fallbackYears()))),
    { initialValue: [] as number[] },
  );

  protected readonly selectedMake = signal<Make | null>(null);
  protected readonly year = signal(new Date().getFullYear());
  protected readonly vehicleType = signal('');

  protected readonly availableTypes = signal<VehicleType[]>([]);
  protected readonly types = signal<VehicleType[]>([]);
  protected readonly models = signal<VehicleModel[]>([]);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly searched = signal<{ make: string; year: number; type: string } | null>(null);

  protected onMakeSelected(make: Make | null): void {
    this.selectedMake.set(make);
    this.vehicleType.set('');
    this.availableTypes.set([]);
    this.error.set(null);

    if (!make) {
      return;
    }

    // Load the type filter as soon as a make is picked, so the choice is available before search.
    this.catalog
      .vehicleTypes(make.makeId)
      .pipe(catchError(() => of([] as VehicleType[])))
      .subscribe(types => this.availableTypes.set(types));
  }

  protected search(): void {
    const make = this.selectedMake();

    if (!make) {
      this.error.set('Pick a make from the list first.');
      return;
    }

    const year = this.year();
    const type = this.vehicleType();

    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      types: this.catalog.vehicleTypes(make.makeId),
      models: this.catalog.models(make.makeId, year, type || undefined),
    }).subscribe({
      next: ({ types, models }) => {
        this.types.set(types);
        this.models.set(models);
        this.searched.set({ make: make.makeName, year, type });
        this.loading.set(false);
      },
      error: (failure: Error) => {
        this.searched.set(null);
        this.loading.set(false);
        this.error.set(failure.message);
      },
    });
  }

  protected reset(): void {
    this.makePicker().reset();
    this.selectedMake.set(null);
    this.vehicleType.set('');
    this.availableTypes.set([]);
    this.types.set([]);
    this.models.set([]);
    this.searched.set(null);
    this.error.set(null);
  }

  private static fallbackYears(): number[] {
    const latest = new Date().getFullYear() + 1;

    return Array.from({ length: latest - 1995 + 1 }, (_, i) => latest - i);
  }
}
