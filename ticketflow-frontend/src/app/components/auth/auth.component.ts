import { Component } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { AuthService } from "../../services/auth.service";

@Component({
  selector: "app-auth",
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div>
      <h2>{{ isRegisterMode ? "Kayıt Ol" : "Giriş Yap" }}</h2>

      <form (ngSubmit)="onSubmit()">
        <div>
          <label>Email:</label>
          <input type="email" [(ngModel)]="email" name="email" required />
        </div>
        <div>
          <label>Şifre:</label>
          <input type="password" [(ngModel)]="password" name="password" required />
        </div>
        <button type="submit">{{ isRegisterMode ? "Kayıt Ol" : "Giriş Yap" }}</button>
      </form>

      <p *ngIf="message" [style.color]="isError ? 'red' : 'green'">{{ message }}</p>

      <button (click)="toggleMode()">
        {{ isRegisterMode ? "Giriş sayfasına geç" : "Kayıt sayfasına geç" }}
      </button>

      <button *ngIf="authService.isLoggedIn()" (click)="logout()">Çıkış Yap</button>
    </div>
  `
})
export class AuthComponent {
  email = "";
  password = "";
  message = "";
  isError = false;
  isRegisterMode = false;

  constructor(public authService: AuthService) {}

  onSubmit(): void {
    this.message = "";
    if (this.isRegisterMode) {
      this.authService.register(this.email, this.password).subscribe({
        next: () => {
          this.message = "Kayıt başarılı, giriş yapabilirsiniz.";
          this.isError = false;
          this.isRegisterMode = false;
        },
        error: (err) => {
          this.message = err.error?.message ?? "Kayıt başarısız.";
          this.isError = true;
        }
      });
    } else {
      this.authService.login(this.email, this.password).subscribe({
        next: () => {
          this.message = "Giriş başarılı!";
          this.isError = false;
        },
        error: () => {
          this.message = "Giriş başarısız.";
          this.isError = true;
        }
      });
    }
  }

  toggleMode(): void {
    this.isRegisterMode = !this.isRegisterMode;
    this.message = "";
  }

  logout(): void {
    this.authService.logout();
    this.message = "Çıkış yapıldı.";
    this.isError = false;
  }
}
