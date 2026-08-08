import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'job-search-aggregator.theme';
const DARK_CLASS = 'dark-theme';

@Injectable({
  providedIn: 'root',
})
export class Theme {
  readonly isDark = signal(this.readStoredPreference());

  constructor() {
    this.applyTheme(this.isDark());
  }

  toggle(): void {
    this.setDark(!this.isDark());
  }

  setDark(isDark: boolean): void {
    this.isDark.set(isDark);
    this.applyTheme(isDark);
    localStorage.setItem(STORAGE_KEY, isDark ? 'dark' : 'light');
  }

  private applyTheme(isDark: boolean): void {
    document.documentElement.classList.toggle(DARK_CLASS, isDark);
  }

  private readStoredPreference(): boolean {
    return localStorage.getItem(STORAGE_KEY) === 'dark';
  }
}
