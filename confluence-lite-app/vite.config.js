import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  // 优化 TinyMCE 的加载性能
  optimizeDeps: {
    include: [
      'tinymce/tinymce',
      '@tinymce/tinymce-vue',
      'tinymce/plugins/image',
      'tinymce/plugins/table',
      'tinymce/plugins/lists',
      'tinymce/plugins/wordcount',
      'tinymce/plugins/code',
      'tinymce/plugins/fullscreen',
      'tinymce/plugins/autoresize',
      'tinymce/plugins/advlist',
      'tinymce/plugins/autolink',
      'tinymce/plugins/link',
      'tinymce/plugins/charmap',
      'tinymce/plugins/preview',
      'tinymce/plugins/anchor',
      'tinymce/plugins/searchreplace',
      'tinymce/plugins/visualblocks',
      'tinymce/plugins/insertdatetime',
      'tinymce/plugins/media',
      'tinymce/plugins/help',
    ]
  }
})
