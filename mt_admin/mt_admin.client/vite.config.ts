import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

const target = 'http://127.0.0.1:8013'; // явно указываем localhost

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5174, // порт dev-сервера
    proxy: {
      // ¬се запросы к /api будут проксироватьс€ на твой ASP.NET бэкенд
      '^/swagger': { target, secure: false },
      '^/api': { target, secure: false },
      '^/public': { target, secure: false },
      '^/static_files': { target, secure: false },
    },
  },
})
