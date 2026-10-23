
export interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  stockQuantity: number;
  imageUrl: string;
  isActive: boolean;
  createdOn: string;
  categoryId: number;
  category?: {
    id: number;
    name: string;
    description: string;
    isActive: boolean;
  };
}

export interface ProductResponse {
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
  data: Product[];
}
