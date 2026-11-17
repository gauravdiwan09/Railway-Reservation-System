import { Injectable } from '@angular/core';
import { RazorpayOrderResponse } from './booking.service';

export interface RazorpayCheckoutResult {
  razorpay_payment_id: string;
  razorpay_order_id: string;
  razorpay_signature: string;
}

interface RazorpayCheckoutOptions {
  key: string;
  amount: number;
  currency: string;
  name: string;
  description: string;
  order_id: string;
  prefill: { name?: string; email?: string; contact?: string };
  handler: (result: RazorpayCheckoutResult) => void;
  modal: { ondismiss: () => void };
}

interface RazorpayFailure {
  error?: { description?: string };
}

interface RazorpayCheckoutInstance {
  open(): void;
  on(event: 'payment.failed', handler: (failure: RazorpayFailure) => void): void;
}

declare global {
  interface Window {
    Razorpay?: new (options: RazorpayCheckoutOptions) => RazorpayCheckoutInstance;
  }
}

@Injectable({ providedIn: 'root' })
export class RazorpayCheckoutService {
  async openCheckout(
    order: RazorpayOrderResponse,
    prefill: { name?: string; email?: string; contact?: string }
  ): Promise<RazorpayCheckoutResult | null> {
    await this.loadCheckoutScript();
    const Checkout = window.Razorpay;
    if (!Checkout) throw new Error('Razorpay Checkout could not be loaded.');

    return new Promise((resolve, reject) => {
      let completed = false;
      const checkout = new Checkout({
        key: order.keyId,
        amount: order.amount,
        currency: order.currency,
        name: 'Indian Railways',
        description: 'Railway ticket reservation',
        order_id: order.orderId,
        prefill,
        handler: (result) => {
          completed = true;
          resolve(result);
        },
        modal: {
          ondismiss: () => {
            if (!completed) resolve(null);
          }
        }
      });
      checkout.on('payment.failed', (failure) => {
        if (completed) return;
        completed = true;
        reject(new Error(failure.error?.description || 'Razorpay payment failed.'));
      });
      checkout.open();
    });
  }

  private loadCheckoutScript(): Promise<void> {
    if (typeof window === 'undefined') return Promise.reject(new Error('Razorpay Checkout requires a browser.'));
    if (window.Razorpay) return Promise.resolve();

    return new Promise((resolve, reject) => {
      let script = document.querySelector<HTMLScriptElement>('#razorpay-checkout-script');
      if (!script) {
        script = document.createElement('script');
        script.id = 'razorpay-checkout-script';
        script.src = 'https://checkout.razorpay.com/v1/checkout.js';
        script.async = true;
      }
      script.addEventListener('load', () => resolve(), { once: true });
      script.addEventListener('error', () => reject(new Error('Unable to load Razorpay Checkout.')), { once: true });
      if (!script.isConnected) document.head.append(script);
    });
  }
}
