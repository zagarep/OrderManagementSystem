
import { CommonModule } from '@angular/common';
import { Component, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth, LoginRequest } from '../../../services/auth';
import { finalize } from 'rxjs';


@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  email = '';
  password = '';
  message = '';
  isError = false;
  isLoading = false;

  constructor(
    private auth: Auth,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) { }



  onLogin(): void {
    this.message = '';
    this.isError = false;
    this.isLoading = true;

    const request: LoginRequest = {
      email: this.email.trim(),
      password: this.password
    };

    this.auth.login(request)
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
          console.log('Loading reset to false:', this.isLoading);
        })
      )
      .subscribe({
        next: (response: any) => {
          console.log('Login response received:', response);
          console.log('Token exists:', !!response?.token);


          if (!response?.token) {
            this.isError = true;
            this.message = 'Login response did not contain a token.';
            return;
          }

          localStorage.setItem('token', response.token);
          this.message = `Login successful! Welcome, ${response.user?.fullName ?? 'Customer'
            }.`;
          this.isError = false;

          setTimeout(() => {
            this.router.navigate(['/products']);
          }, 1000);
        },
        error: (error) => {
          console.error('Login error:', error);
          this.isError = true;
          this.message =
            error.error?.message ||
            'Login failed. Please check your credentials.';
        }
      });
  }

  
}
