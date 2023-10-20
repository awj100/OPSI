import { defineConfig } from 'vite'
import { svelte } from '@sveltejs/vite-plugin-svelte'
import sveltePreprocess from 'svelte-preprocess';
import path from 'path';

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [svelte({
    preprocess: sveltePreprocess()
  })],
  css: {
      preprocessorOptions: {
          scss: {                                 
          //     additionalData: `
          //     @use './src/lib/scss/mixins' as *;
          //     @use './src/lib/scss/variables.scss' as *;
          // `,
          }
      }
  },
  resolve: {
    alias: {
      "@": path.resolve("/src"),
    },
  }
});
