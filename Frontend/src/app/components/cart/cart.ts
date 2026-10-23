
import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Cart, CartItem } from '../../services/cart';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './cart.html',
  styleUrl: './cart.css'
})
export class CartComponent {
  items: CartItem[] = [];

  constructor(
    private cart: Cart,
    private router: Router
  ) {
    this.refreshCart();
  }

  refreshCart(): void {
    this.items = this.cart.getItems();
  }

  updateQuantity(item: CartItem, quantity: number): void {
    this.cart.updateQuantity(item.product.id, quantity);
    this.refreshCart();
  }

  removeItem(productId: number): void {
    this.cart.removeItem(productId);
    this.refreshCart();
  }

  get totalPrice(): number {
    return this.cart.getTotalPrice();
  }

  get totalItems(): number {
    return this.cart.getTotalItems();
  }

  continueShopping(): void {
    this.router.navigate(['/products']);
  }

  logout(): void {
    localStorage.removeItem('token');
    this.cart.clearCart();
    this.router.navigate(['/login']);
  }
}
