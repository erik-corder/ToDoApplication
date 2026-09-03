import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Standalone output keeps the Docker image minimal — only the files
  // actually needed to run `node server.js` get copied into the final
  // runtime stage (see apps/web/Dockerfile).
  output: "standalone",
};

export default nextConfig;
