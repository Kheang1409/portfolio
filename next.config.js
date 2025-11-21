/** @type {import('next').NextConfig} */
const nextConfig = {
  useDeploymentId: true,
  output: "standalone",
  useDeploymentIdServerActions: true,
};

module.exports = nextConfig;
