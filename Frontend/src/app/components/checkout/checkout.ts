import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Cart, CartItem } from '../../services/cart';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Auth } from '../../services/auth';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './checkout.html',
  styleUrl: './checkout.css'
})
export class CheckoutComponent {
  items: CartItem[] = [];
  deliveryAddress = '';
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  private readonly apiUrl = 'https://localhost:7119/api/Orders';

  constructor(
    private cart: Cart,
    private router: Router,
    private http: HttpClient,
    private auth: Auth
  ) {
    this.items = this.cart.getItems();
  }

  get totalPrice(): number {
    return this.cart.getTotalPrice();
  }

  get totalItems(): number {
    return this.cart.getTotalItems();
  }

  backToCart(): void {
    this.router.navigate(['/cart']);
  }
  placeOrder(): void {
    if (!this.deliveryAddress.trim()) {
      this.errorMessage = 'Please enter your delivery address.';
      return;
    }

    if (this.items.length === 0) {
      this.errorMessage = 'Your cart is empty.';
      return;
    }

    const token = localStorage.getItem('token');

    if (!token) {
      this.router.navigate(['/login']);
      return;
    }

    const order = {
      customerName: '',
      customerEmail: '',
      deliveryAddress: this.deliveryAddress.trim(),
      items: this.items.map(item => ({
        productId: item.product.id,
        quantity: item.quantity
      }))
    };

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.http.post(this.apiUrl, order, {
      headers: new HttpHeaders({
        Authorization: `Bearer ${token}`
      })
    })
      .pipe(finalize(() => {
        this.isLoading = false;
      }))
      .subscribe({
        next: () => {
          this.cart.clearCart();
          this.items = [];
          this.successMessage = 'Your order was placed successfully!';
        },
        error: (error) => {
          this.errorMessage =
            error.error?.message ||
            error.error?.title ||
            'Unable to place your order. Please try again.';
        }
      });
  }
}
