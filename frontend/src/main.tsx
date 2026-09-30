import React from 'react';
import ReactDOM from 'react-dom/client';
import { App } from './app/App';
import './index.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

if ('serviceWorker' in navigator && !window.location.host.includes('localhost')) {
  window.addEventListener('load', () => {
    navigator.serviceWorker.register('/sw.js').catch((err) => {
      console.log('ServiceWorker registration failed:', err);
    });
  });
}
