export interface Designation {
  id: number;
  designationCode: string;
  designationName: string;
  description: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface DesignationRequest {
  designationCode: string;
  designationName: string;
  description: string | null;
  isActive: boolean;
}