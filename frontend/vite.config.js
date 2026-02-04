import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5076', // Adjust to your backend port
        changeOrigin: true,
        secure: false,
      },
      '/planningpokerhub': {
        target: 'http://localhost:5076', // Adjust to your backend port
        ws: true,
      },
    },
  },
})
