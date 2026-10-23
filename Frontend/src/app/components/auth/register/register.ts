
import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth, RegisterRequest } from '../../../services/auth';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class Register {
  fullName = '';
  email = '';
  password = '';
  message = '';
  isError = false;
  isLoading = false;

  constructor(
    private auth: Auth,
    private router: Router
  ) { }

  onRegister(): void {
    this.message = '';
    this.isError = false;

    const request: RegisterRequest = {
      fullName: this.fullName.trim(),
      email: this.email.trim(),
      password: this.password
    };

    this.isLoading = true;

    this.auth.register(request).subscribe({
      next: () => {
        this.isLoading = false;
        this.message = 'Registration successful! Please log in.';
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 1000);
      },
      error: (error) => {
        this.isLoading = false;
        this.isError = true;
        this.message =
          error.error?.message ||
          'Registration failed. Please check your details.';
      }
    });
  }
}
