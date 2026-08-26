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
    const isDark = resolvedTheme === "dark";
    const primary = isDark ? 0x3b82f6 : 0x1d4ed8;
    const accent = isDark ? 0x22d3ee : 0x0284c7;
    const violet = isDark ? 0x8b5cf6 : 0x6d28d9;
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(42, 1, 0.1, 100);
    camera.position.set(0, 0.15, 7);
    const renderer = new THREE.WebGLRenderer({ alpha: true, antialias: true });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.75));
    renderer.outputColorSpace = THREE.SRGBColorSpace;
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = isDark ? 1.15 : 0.88;
    renderer.setClearColor(0x000000, 0);
    mount.appendChild(renderer.domElement);

    const world = new THREE.Group();
    scene.add(world);
    const core = new THREE.Mesh(
      new THREE.IcosahedronGeometry(1.25, 3),
      new THREE.MeshPhysicalMaterial({ color: primary, roughness: isDark ? 0.2 : 0.34, metalness: 0.45, transmission: isDark ? 0.12 : 0.04, emissive: isDark ? 0x071c4f : 0x000000, emissiveIntensity: isDark ? 0.9 : 0, flatShading: true }),
    );
    world.add(core);
    const wire = new THREE.Mesh(
      new THREE.IcosahedronGeometry(1.47, 2),
      new THREE.MeshBasicMaterial({ color: accent, wireframe: true, transparent: true, opacity: isDark ? 0.22 : 0.13 }),
    );
    world.add(wire);

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

    const particleCount = window.innerWidth < 640 ? 90 : 170;
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

    scene.add(new THREE.HemisphereLight(isDark ? 0xbfe4ff : 0xffffff, isDark ? 0x071126 : 0xdbeafe, isDark ? 2.4 : 1.65));
    const keyLight = new THREE.PointLight(accent, isDark ? 22 : 9, 14);
    keyLight.position.set(3, 3, 4);
    scene.add(keyLight);
    const rimLight = new THREE.PointLight(violet, isDark ? 18 : 7, 12);
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
    const animate = () => {
      frame = requestAnimationFrame(animate);
      if (!visible) return;
      const t = clock.getElapsedTime();
      if (!reduceMotion) {
        world.rotation.y += (pointerX - world.rotation.y) * 0.025;
        world.rotation.x += (-pointerY - world.rotation.x) * 0.025;
        core.rotation.set(t * 0.08, t * 0.16, 0);
        wire.rotation.set(0, -t * 0.11, t * 0.06);
        particles.rotation.y = t * 0.025;
        rings.forEach((ring, index) => { ring.rotation.z = t * (0.045 + index * 0.018); });
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

  return <div ref={mountRef} className="hero-scene" aria-hidden="true"><div className="hero-scene__fallback" /></div>;
}
