/** A vehicle manufacturer. */
export interface Make {
  makeId: number;
  makeName: string;
}

/** A category of vehicle a make produces, e.g. "Passenger Car". */
export interface VehicleType {
  vehicleTypeId: number;
  vehicleTypeName: string;
}

/** A model produced by a make in a given year. */
export interface VehicleModel {
  modelId: number;
  modelName: string;
  vehicleTypeName: string | null;
}
