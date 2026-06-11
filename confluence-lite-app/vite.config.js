import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'

import {writeFileSync} from 'fs';
import {resolve} from 'path';

// 生成版本号：20250822
function generateVersion() {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    const hour = String(now.getHours()).padStart(2, '0');
    const minute = String(now.getMinutes()).padStart(2, '0');
    return `${year}${month}${day}.${hour}${minute}`;
}

// 写入 src/version.js
function writeVersionFile() {
    const version = generateVersion();
    const filePath = resolve(__dirname, 'src/version.js');
    const content = `// 自动生成，请勿手动修改\nexport const version = ${version};\n`;
    writeFileSync(filePath, content);
    console.log(`✅ 已写入版本号到 src/version.js：${version}`);
}
writeVersionFile();

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5135',
        changeOrigin: true
      }
    }
  }
})
