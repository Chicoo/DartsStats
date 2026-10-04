const { spawn } = require('node:child_process');

const server = spawn(process.execPath, [
  require.resolve('@angular/cli/bin/ng.js'),
  'serve',
  '--host', '0.0.0.0',
  '--port', process.env.PORT || '4212',
  '--proxy-config', 'proxy.conf.cjs',
  ...process.argv.slice(2),
], { stdio: 'inherit' });

server.on('error', (error) => {
  console.error(error);
  process.exitCode = 1;
});
server.on('exit', (code) => {
  process.exitCode = code ?? 1;
});
for (const signal of ['SIGINT', 'SIGTERM']) {
  process.on(signal, () => server.kill(signal));
}
