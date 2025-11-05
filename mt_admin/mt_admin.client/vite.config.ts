import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5174, // порт dev-сервера
    proxy: {
      // Все запросы к /api будут проксироваться на твой ASP.NET бэкенд
      '^/api': {
        target: 'http://localhost:5144',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
