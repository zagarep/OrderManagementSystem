
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CreateOrderItem {
  productId: number;
  quantity: number;
}

export interface CreateOrderRequest {
  customerName: string;
  customerEmail: string;
  deliveryAddress: string;
  items: CreateOrderItem[];
}

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private readonly apiUrl = 'https://localhost:7119/api/Orders';

  constructor(private http: HttpClient) { }

  createOrder(order: CreateOrderRequest): Observable<unknown> {
    return this.http.post<unknown>(this.apiUrl, order);
  }
}
