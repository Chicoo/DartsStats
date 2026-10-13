import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import istanbul from 'vite-plugin-istanbul'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    // Istanbul instrumentation — enabled only when VITE_COVERAGE=true
    istanbul({
      include: 'src/*',
      exclude: ['node_modules', 'test/'],
      extension: ['.js', '.ts', '.tsx'],
      requireEnv: true,
      cypress: false,
    }),
  ],
  server: {
    host: '0.0.0.0',
    port: parseInt(process.env.PORT || '5173'),
    strictPort: false,
    proxy: process.env.PROTOMAPS_HTTP ? {
      "/protomaps": {
        target: process.env.PROTOMAPS_HTTP,
        changeOrigin: true,
      },
    } : undefined
  }
})
