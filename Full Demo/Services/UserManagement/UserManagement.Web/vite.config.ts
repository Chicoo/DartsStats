import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { execFileSync } from 'node:child_process';

const certDirectory = path.join(os.tmpdir(), 'dartsstats-user-management');
const certPath = path.join(certDirectory, 'dev.pem');
const keyPath = path.join(certDirectory, 'dev.key');
fs.mkdirSync(certDirectory, { recursive: true });
if (!fs.existsSync(certPath) || !fs.existsSync(keyPath))
  execFileSync('dotnet', ['dev-certs', 'https', '--export-path', certPath, '--format', 'PEM', '--no-password']);

export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0', port: Number(process.env.PORT || 5178), strictPort: true,
    https: { cert: fs.readFileSync(certPath), key: fs.readFileSync(keyPath) },
    proxy: Object.fromEntries(['/api', '/signin-oidc', '/signout-callback-oidc'].map(path => [path, {
      target: process.env.USER_MANAGEMENT_API || 'https://localhost:7068', changeOrigin: false, secure: true
    }]))
  },
  build: { outDir: 'dist' }
});
