import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { App } from './app';

describe('App', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  function createComponent() {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    // The year list is requested on first render.
    httpMock.expectOne('/api/vehicles/years').flush([2026, 2025, 2015]);

    return fixture;
  }

  it('renders the lookup form', () => {
    const page = createComponent().nativeElement as HTMLElement;

    expect(page.querySelector('h1')?.textContent).toContain('Find vehicle types and models');
    expect(page.querySelector('#year-select')).toBeTruthy();
  });

  it('keeps the search button disabled until a make is picked', () => {
    const fixture = createComponent();
    const button = (fixture.nativeElement as HTMLElement).querySelector('button[type="submit"]');

    expect(button?.hasAttribute('disabled')).toBe(true);
  });

  it('loads the vehicle type filter as soon as a make is picked', () => {
    const fixture = createComponent();

    fixture.componentInstance['onMakeSelected']({ makeId: 474, makeName: 'Honda' });

    httpMock
      .expectOne('/api/vehicles/makes/474/vehicle-types')
      .flush([{ vehicleTypeId: 3, vehicleTypeName: 'Truck' }]);

    expect(fixture.componentInstance['availableTypes']()).toHaveLength(1);
  });

  it('shows the returned models after a search', async () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;

    component['onMakeSelected']({ makeId: 474, makeName: 'Honda' });
    httpMock.expectOne('/api/vehicles/makes/474/vehicle-types').flush([]);

    component['year'].set(2015);
    component['search']();

    httpMock
      .expectOne('/api/vehicles/makes/474/vehicle-types')
      .flush([{ vehicleTypeId: 3, vehicleTypeName: 'Truck' }]);
    httpMock
      .expectOne(r => r.url === '/api/vehicles/makes/474/models')
      .flush([{ modelId: 1866, modelName: 'Ridgeline', vehicleTypeName: 'Truck' }]);

    fixture.detectChanges();
    await fixture.whenStable();

    const page = fixture.nativeElement as HTMLElement;
    expect(page.textContent).toContain('Ridgeline');
    expect(page.querySelector('.chip')?.textContent).toContain('Truck');
  });

  it('shows the error message when the API fails', async () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;

    component['onMakeSelected']({ makeId: 474, makeName: 'Honda' });
    httpMock.expectOne('/api/vehicles/makes/474/vehicle-types').flush([]);

    component['search']();

    // forkJoin cancels the sibling models request as soon as this one fails, so it is never
    // flushed here.
    httpMock
      .expectOne('/api/vehicles/makes/474/vehicle-types')
      .flush(
        { status: 503, detail: 'vPIC is unreachable.' },
        { status: 503, statusText: 'Service Unavailable' },
      );

    fixture.detectChanges();
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).querySelector('.notice.error')?.textContent)
      .toContain('vPIC is unreachable.');
  });
});
