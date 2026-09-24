// @ts-check
import { defineConfig } from 'astro/config';

import tailwindcss from '@tailwindcss/vite';
import preact from '@astrojs/preact';
import mdx from '@astrojs/mdx';
import sitemap from '@astrojs/sitemap';

import shikiTheme from './src/styles/shiki-debugger.json' with { type: 'json' };

// https://astro.build/config
export default defineConfig({
  site: 'https://pattern-debugger.pages.dev',
  trailingSlash: 'always',
  vite: {
    plugins: [tailwindcss()],
  },
  markdown: {
    shikiConfig: {
      theme: /** @type {import('shiki').ThemeRegistrationRaw} */ (shikiTheme),
    },
  },
  integrations: [preact(), mdx(), sitemap()],
});
