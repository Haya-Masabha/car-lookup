import { Component, ChangeDetectionStrategy, output, signal, inject } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, of, switchMap, catchError, tap } from 'rxjs';
import { Make } from '../vehicle';
import { VehicleCatalog } from '../vehicle-catalog';

/**
 * Type-ahead for picking a make. vPIC publishes over 12,000 makes, far too many for a dropdown,
 * so the term is sent to the API and only a short ranked list comes back.
 */
@Component({
  selector: 'app-make-picker',
  templateUrl: './make-picker.html',
  styleUrl: './make-picker.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MakePicker {
  private readonly catalog = inject(VehicleCatalog);

  /** Emits the chosen make, or null when the selection is cleared by further typing. */
  readonly makeSelected = output<Make | null>();

  protected readonly query = signal('');
  protected readonly open = signal(false);
  protected readonly searching = signal(false);
  /** Index of the keyboard-highlighted suggestion, or -1 when none is highlighted. */
  protected readonly activeIndex = signal(-1);

  protected readonly suggestions = toSignal(
    toObservable(this.query).pipe(
      debounceTime(200),
      distinctUntilChanged(),
      tap(() => this.activeIndex.set(-1)),
      switchMap(term => {
        const trimmed = term.trim();

        if (!trimmed) {
          return of<Make[]>([]);
        }

        this.searching.set(true);

        // A failed suggestion lookup just shows no suggestions; the error surfaces on search.
        return this.catalog.searchMakes(trimmed).pipe(
          catchError(() => of<Make[]>([])),
          tap(() => this.searching.set(false)),
        );
      }),
    ),
    { initialValue: [] as Make[] },
  );

  protected onInput(value: string): void {
    this.query.set(value);
    this.open.set(true);

    // Typing invalidates any previous pick: the id has to come from the list, not the text box.
    this.makeSelected.emit(null);
  }

  protected select(make: Make): void {
    this.query.set(make.makeName);
    this.open.set(false);
    this.activeIndex.set(-1);
    this.makeSelected.emit(make);
  }

  protected onKeydown(event: KeyboardEvent): void {
    const items = this.suggestions();

    if (!this.open() || items.length === 0) {
      return;
    }

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.activeIndex.update(i => (i + 1) % items.length);
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.activeIndex.update(i => (i - 1 + items.length) % items.length);
        break;
      case 'Enter': {
        const active = this.activeIndex();
        if (active >= 0) {
          event.preventDefault();
          this.select(items[active]);
        }
        break;
      }
      case 'Escape':
        this.open.set(false);
        break;
    }
  }

  /** Delayed so a click on a suggestion is not lost to the blur that precedes it. */
  protected onBlur(): void {
    setTimeout(() => this.open.set(false), 150);
  }

  reset(): void {
    this.query.set('');
    this.open.set(false);
    this.activeIndex.set(-1);
    this.makeSelected.emit(null);
  }
}
