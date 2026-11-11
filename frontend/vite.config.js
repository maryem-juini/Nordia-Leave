import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// The dev server forwards /api/* to the .NET API, so the browser only talks to one origin (no CORS setup).
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: { '/api': 'http://localhost:5080' },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: './src/setupTests.js',
  },
})
