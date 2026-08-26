"use client";

import { useEffect, useRef } from "react";
import * as THREE from "three";
import { useTheme } from "next-themes";

export default function HeroScene() {
  const mountRef = useRef<HTMLDivElement>(null);
  const { resolvedTheme } = useTheme();

  useEffect(() => {
    const mount = mountRef.current;
    if (!mount) return;
    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const isMobile = window.matchMedia("(max-width: 767px)").matches;
    const isDark = resolvedTheme === "dark";
    const primary = isDark ? 0x3b82f6 : 0xd8dee8;
    const accent = isDark ? 0x22d3ee : 0x94a3b8;
    const lightColor = isDark ? accent : 0xffffff;
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(42, 1, 0.1, 100);
    camera.position.set(0, 0.15, 7);
    const renderer = new THREE.WebGLRenderer({ alpha: true, antialias: true });
    renderer.setPixelRatio(isMobile ? 1 : Math.min(window.devicePixelRatio, 1.6));
    renderer.outputColorSpace = THREE.SRGBColorSpace;
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = isDark ? 1.15 : 0.88;
    renderer.setClearColor(0x000000, 0);
    mount.appendChild(renderer.domElement);

    const world = new THREE.Group();
    scene.add(world);
    const core = new THREE.Mesh(
      new THREE.IcosahedronGeometry(1.2, 2),
      new THREE.MeshPhysicalMaterial({ color: primary, roughness: isDark ? 0.2 : 0.3, metalness: isDark ? 0.45 : 0.3, transmission: isDark ? 0.12 : 0.08, clearcoat: isDark ? 0.15 : 0.5, clearcoatRoughness: 0.25, emissive: isDark ? 0x071c4f : 0x000000, emissiveIntensity: isDark ? 0.9 : 0, flatShading: true }),
    );
    world.add(core);
    const wire = new THREE.Mesh(
      new THREE.IcosahedronGeometry(1.47, 2),
      new THREE.MeshBasicMaterial({ color: accent, wireframe: true, transparent: true, opacity: isDark ? 0.22 : 0.13 }),
    );
    world.add(wire);

    // Layered service modules turn the abstract core into a system architecture hub.
    const serviceModules: THREE.Mesh[] = [];
    for (let index = 0; index < 4; index += 1) {
      const module = new THREE.Mesh(
        new THREE.BoxGeometry(0.68, 0.18, 0.68),
        new THREE.MeshPhysicalMaterial({
          color: isDark ? (index % 2 === 0 ? primary : accent) : (index % 2 === 0 ? 0xe2e8f0 : 0xcbd5e1),
          roughness: 0.26,
          metalness: 0.55,
          transparent: true,
          opacity: isDark ? 0.72 : 0.66,
          emissive: isDark ? accent : 0x000000,
          emissiveIntensity: isDark ? 0.08 : 0,
        }),
      );
      const angle = (index / 4) * Math.PI * 2 + Math.PI / 4;
      module.position.set(Math.cos(angle) * 1.8, Math.sin(angle) * 1.8, 0);
      module.rotation.z = angle;
      world.add(module);
      serviceModules.push(module);
    }

    const rings = [
      { radius: 2.05, tube: 0.018, x: 1.05, y: 0.18 },
      { radius: 2.45, tube: 0.012, x: -0.55, y: 1.12 },
      { radius: 2.8, tube: 0.009, x: 0.3, y: -0.72 },
    ].map(({ radius, tube, x, y }) => {
      const ring = new THREE.Mesh(new THREE.TorusGeometry(radius, tube, 8, 140), new THREE.MeshBasicMaterial({ color: accent, transparent: true, opacity: isDark ? 0.42 : 0.24 }));
      ring.rotation.set(x, y, 0);
      world.add(ring);
      return ring;
    });

    const particleCount = isMobile ? 64 : 150;
    const positions = new Float32Array(particleCount * 3);
    for (let i = 0; i < particleCount; i += 1) {
      const radius = 2.1 + Math.random() * 2.35;
      const theta = Math.random() * Math.PI * 2;
      const phi = Math.acos(2 * Math.random() - 1);
      positions[i * 3] = radius * Math.sin(phi) * Math.cos(theta);
      positions[i * 3 + 1] = radius * Math.sin(phi) * Math.sin(theta);
      positions[i * 3 + 2] = radius * Math.cos(phi);
    }
    const particleGeometry = new THREE.BufferGeometry();
    particleGeometry.setAttribute("position", new THREE.BufferAttribute(positions, 3));
    const particles = new THREE.Points(particleGeometry, new THREE.PointsMaterial({ color: accent, size: 0.032, transparent: true, opacity: isDark ? 0.7 : 0.34, sizeAttenuation: true }));
    world.add(particles);

    scene.add(new THREE.HemisphereLight(isDark ? 0xbfe4ff : 0xffffff, isDark ? 0x071126 : 0xcbd5e1, isDark ? 2.4 : 1.85));
    const keyLight = new THREE.PointLight(lightColor, isDark ? 22 : 10, 14);
    keyLight.position.set(3, 3, 4);
    scene.add(keyLight);
    const rimLight = new THREE.PointLight(isDark ? primary : 0x94a3b8, isDark ? 18 : 5, 12);
    rimLight.position.set(-4, -2, 2);
    scene.add(rimLight);

    let pointerX = 0;
    let pointerY = 0;
    const onPointerMove = (event: PointerEvent) => {
      const bounds = mount.getBoundingClientRect();
      pointerX = ((event.clientX - bounds.left) / bounds.width - 0.5) * 0.7;
      pointerY = ((event.clientY - bounds.top) / bounds.height - 0.5) * 0.5;
    };
    mount.addEventListener("pointermove", onPointerMove);
    const resize = () => {
      const { width, height } = mount.getBoundingClientRect();
      renderer.setSize(width, height, false);
      camera.aspect = width / Math.max(height, 1);
      camera.updateProjectionMatrix();
    };
    const resizeObserver = new ResizeObserver(resize);
    resizeObserver.observe(mount);
    resize();

    let frame = 0;
    let visible = true;
    const visibilityObserver = new IntersectionObserver(([entry]) => { visible = entry.isIntersecting; });
    visibilityObserver.observe(mount);
    const clock = new THREE.Clock();
    let lastRenderedAt = 0;
    const animate = () => {
      frame = requestAnimationFrame(animate);
      if (!visible) return;
      const t = clock.getElapsedTime();
      if (isMobile && t - lastRenderedAt < 1 / 30) return;
      lastRenderedAt = t;
      if (!reduceMotion) {
        world.rotation.y += (pointerX - world.rotation.y) * 0.025;
        world.rotation.x += (-pointerY - world.rotation.x) * 0.025;
        core.rotation.set(t * 0.08, t * 0.16, 0);
        wire.rotation.set(0, -t * 0.11, t * 0.06);
        particles.rotation.y = t * 0.025;
        rings.forEach((ring, index) => { ring.rotation.z = t * (0.045 + index * 0.018); });
        serviceModules.forEach((module, index) => {
          module.rotation.x = t * 0.16 + index;
          module.rotation.y = t * 0.12 + index * 0.4;
        });
      }
      renderer.render(scene, camera);
    };
    animate();

    return () => {
      cancelAnimationFrame(frame);
      resizeObserver.disconnect();
      visibilityObserver.disconnect();
      mount.removeEventListener("pointermove", onPointerMove);
      scene.traverse((object) => {
        if (!(object instanceof THREE.Mesh || object instanceof THREE.Points)) return;
        object.geometry.dispose();
        (Array.isArray(object.material) ? object.material : [object.material]).forEach((material) => material.dispose());
      });
      renderer.dispose();
      renderer.domElement.remove();
    };
  }, [resolvedTheme]);

  return (
    <div ref={mountRef} className="hero-scene" aria-hidden="true">
      <div className="hero-scene__fallback" />
      <div className="hero-tech-icon hero-tech-icon--dotnet" title="C# and .NET">
        <svg viewBox="0 0 64 64" role="img">
          <path d="M32 4 56 18v28L32 60 8 46V18Z" fill="currentColor" opacity=".18" />
          <path d="M32 4 56 18v28L32 60 8 46V18Z" fill="none" stroke="currentColor" strokeWidth="3" />
          <text x="32" y="31" textAnchor="middle" fill="currentColor" fontSize="17" fontWeight="800">C#</text>
          <text x="32" y="44" textAnchor="middle" fill="currentColor" fontSize="9" fontWeight="700">.NET</text>
        </svg>
      </div>
      <div className="hero-tech-icon hero-tech-icon--react" title="React">
        <svg viewBox="0 0 64 64" role="img" fill="none" stroke="currentColor" strokeWidth="2.8">
          <ellipse cx="32" cy="32" rx="27" ry="10" />
          <ellipse cx="32" cy="32" rx="27" ry="10" transform="rotate(60 32 32)" />
          <ellipse cx="32" cy="32" rx="27" ry="10" transform="rotate(120 32 32)" />
          <circle cx="32" cy="32" r="4.5" fill="currentColor" stroke="none" />
        </svg>
      </div>
      <div className="hero-tech-icon hero-tech-icon--cloud" title="Cloud architecture">
        <svg viewBox="0 0 64 64" role="img" fill="none" stroke="currentColor" strokeWidth="4" strokeLinecap="round" strokeLinejoin="round">
          <path d="M18 48h29a11 11 0 0 0 1-22 17 17 0 0 0-32-3A13 13 0 0 0 18 48Z" />
          <path d="m25 36 7-7 7 7M32 29v14" />
        </svg>
      </div>
      <div className="hero-tech-icon hero-tech-icon--api" title="API engineering">
        <svg viewBox="0 0 64 64" role="img" fill="none" stroke="currentColor" strokeWidth="4" strokeLinecap="round" strokeLinejoin="round">
          <path d="m23 14-12 18 12 18M41 14l12 18-12 18M37 9 27 55" />
        </svg>
      </div>
    </div>
  );
}
