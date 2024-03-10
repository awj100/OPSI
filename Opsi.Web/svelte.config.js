import { vitePreprocess } from "@sveltejs/vite-plugin-svelte";
import { optimizeImports } from "carbon-preprocess-svelte";

export default {
  // Consult https://svelte.dev/docs#compile-time-svelte-preprocess
  // for more information about preprocessors
  preprocess: [
    vitePreprocess({
      scss: {
          // prependData: `@import './src/lib/scss/variables.scss';`
        }
    }),
    optimizeImports()
  ]
}
