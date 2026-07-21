import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { VehicleCatalog } from './vehicle-catalog';

describe('VehicleCatalog', () => {
  let catalog: VehicleCatalog;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    catalog = TestBed.inject(VehicleCatalog);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('sends the search term and limit to the makes endpoint', () => {
    catalog.searchMakes('hon').subscribe();

    const request = httpMock.expectOne(r => r.url === '/api/vehicles/makes');

    expect(request.request.params.get('query')).toBe('hon');
    expect(request.request.params.get('limit')).toBe('25');
    request.flush([]);
  });

  it('omits the vehicle type parameter when no type is selected', () => {
    catalog.models(474, 2015).subscribe();

    const request = httpMock.expectOne(r => r.url === '/api/vehicles/makes/474/models');

    expect(request.request.params.get('year')).toBe('2015');
    expect(request.request.params.has('vehicleType')).toBe(false);
    request.flush([]);
  });

  it('includes the vehicle type parameter when one is selected', () => {
    catalog.models(474, 2015, 'Truck').subscribe();

    const request = httpMock.expectOne(r => r.url === '/api/vehicles/makes/474/models');

    expect(request.request.params.get('vehicleType')).toBe('Truck');
    request.flush([]);
  });

  it('surfaces the detail from a problem document', async () => {
    const failure = new Promise<Error>(resolve =>
      catalog.searchMakes('hon').subscribe({ error: resolve }),
    );

    httpMock.expectOne(r => r.url === '/api/vehicles/makes').flush(
      { status: 503, title: 'Vehicle data service unavailable', detail: 'vPIC is unreachable.' },
      { status: 503, statusText: 'Service Unavailable' },
    );

    expect((await failure).message).toBe('vPIC is unreachable.');
  });

  it('explains a network failure in plain language', async () => {
    const failure = new Promise<Error>(resolve =>
      catalog.years().subscribe({ error: resolve }),
    );

    httpMock
      .expectOne('/api/vehicles/years')
      .error(new ProgressEvent('network error'), { status: 0 });

    expect((await failure).message).toContain('Could not reach the Car Lookup service');
  });
});
