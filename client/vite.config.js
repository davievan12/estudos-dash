import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// O build sai direto para ../wwwroot, que é servido pela API em C#/.NET.
// Em dev (npm run dev), o Vite faz proxy de /api para a API rodando em :5099.
export default defineConfig({
  plugins: [react()],
  base: "./",
  build: {
    outDir: "../wwwroot",
    emptyOutDir: true,
  },
  server: {
    proxy: {
      "/api": "http://localhost:5099",
    },
  },
});
