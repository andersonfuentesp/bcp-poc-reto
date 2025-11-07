import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class ExchangeService {
  private base = '/api';
  private token = 'supersecreto'; // pon tu API_TOKEN si lo usas; si no, déjalo vacío

  constructor(private http: HttpClient) {}

  convert(monto: number, from: string, to: string) {
    const headers = this.token ? new HttpHeaders().set('Authorization', `Bearer ${this.token}`) : undefined;
    return this.http.get<any>(`${this.base}/convert`, { headers, params: { monto, monedaOrigen: from, monedaDestino: to } });
  }

  upsert(from: string, to: string, rate: number) {
    const headers = (this.token ? new HttpHeaders().set('Authorization', `Bearer ${this.token}`) : new HttpHeaders())
      .set('Content-Type','application/json');
    return this.http.post<any>(`${this.base}/rates`, { from, to, rate }, { headers });
  }
}