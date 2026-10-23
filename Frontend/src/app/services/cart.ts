
import { Injectable } from '@angular/core';
import { Product } from '../models/product';

export interface CartItem {
  product: Product;
  quantity: number;
}

@Injectable({
  providedIn: 'root'
})
export class Cart {
  private items: CartItem[] = [];

  getItems(): CartItem[] {
    return [...this.items];
  }

  addToCart(product: Product, quantity = 1): void {
    if (quantity < 1 || product.stockQuantity < 1) {
      return;
    }

    const existing = this.items.find(
      item => item.product.id === product.id
    );

    const newQuantity = (existing?.quantity ?? 0) + quantity;

    if (newQuantity > product.stockQuantity) {
      return;
    }

    if (existing) {
      existing.quantity = newQuantity;
    } else {
      this.items.push({ product, quantity });
    }
  }

  updateQuantity(productId: number, quantity: number): void {
    const item = this.items.find(
      item => item.product.id === productId
    );

    if (!item) {
      return;
    }

    if (quantity <= 0) {
      this.removeItem(productId);
      return;
    }

    if (quantity <= item.product.stockQuantity) {
      item.quantity = quantity;
    }
  }

  removeItem(productId: number): void {
    this.items = this.items.filter(
      item => item.product.id !== productId
    );
  }

  getTotalItems(): number {
    return this.items.reduce(
      (total, item) => total + item.quantity, 0
    );
  }

  getTotalPrice(): number {
    return this.items.reduce(
      (total, item) => total + item.product.price * item.quantity, 0
    );
  }

  clearCart(): void {
    this.items = [];
  }
}
