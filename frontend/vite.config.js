import { defineConfig } from 'vite';

export default defineConfig({
  root: '.',
  base: './',
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:8765',
        changeOrigin: true
      }
    }
  },
  build: {
    outDir: 'dist',
    emptyOutDir: true,
    rollupOptions: {
      input: {
        main: 'index.html',
        'vj-output': 'vj-output.html',
        'preview-output': 'preview-output.html',
        'gig-join-qr': 'gig-join-qr.html'
      }
    }
  }
});
