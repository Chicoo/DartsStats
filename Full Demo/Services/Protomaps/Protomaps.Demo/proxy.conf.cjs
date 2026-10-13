module.exports = {
  '/protomaps/**': {
    target: process.env.PROTOMAPS_HTTP || 'http://localhost:5138',
    changeOrigin: true,
  },
};
