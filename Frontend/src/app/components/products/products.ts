
import { CommonModule } from '@angular/common';
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ProductService } from '../../services/product';
import { Product } from '../../models/product';
import { Cart } from '../../services/cart';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CommonModule, RouterLink,FormsModule],
  templateUrl: './products.html',
  styleUrl: './products.css'
})
export class Products implements OnInit {
  products: Product[] = [];
  isLoading = false;
  errorMessage = '';

  constructor(
    private productService: ProductService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private cart: Cart
  ) { }

  ngOnInit(): void {
    this.loadProducts();
  }
  get cartItemCount(): number {
    return this.cart.getTotalItems();
  }

  loadProducts(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.productService.getProducts(1, 10).subscribe({
      next: (response) => {
        console.log('Products response:', response);

        this.products = response.data ?? [];
        this.isLoading = false;

        this.cdr.detectChanges();
        console.log('Loading reset:', this.isLoading);
      },
      error: (error) => {
        console.error('Products API error:', error);

        this.errorMessage =
          'Unable to load products. Please try again.';

        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }


  logout(): void {
    localStorage.removeItem('token');
    this.router.navigate(['/login']);
  }

  quantities: { [productId: number]: number } = {};

  addToCart(product: Product): void {
    const quantity = this.quantities[product.id] || 1;

    if (quantity > product.stockQuantity) {
      alert('Requested quantity exceeds available stock.');
      return;
    }

    this.cart.addToCart(product, quantity);
    alert(`${product.name} added to cart.`);
  }
}
