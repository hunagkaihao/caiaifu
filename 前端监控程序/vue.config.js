const { defineConfig } = require('@vue/cli-service')
module.exports = defineConfig({
  transpileDependencies: true,
  devServer: {
    proxy: {
      '/ecs': {
        target: 'http://localhost:3270',
        changeOrigin: true,
      },
    },
  },
})
