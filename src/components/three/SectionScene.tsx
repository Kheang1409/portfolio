"use client";

import { useEffect, useRef, useState } from "react";
import * as THREE from "three";
import { useTheme } from "next-themes";
import Image from "next/image";

export type SceneVariant =
  | "about"
  | "skills"
  | "experience"
  | "projects"
  | "education"
  | "contact";

type Props = { variant: SceneVariant };

const variantIndex: Record<SceneVariant, number> = {
  about: 0,
  skills: 1,
  experience: 2,
  projects: 3,
  education: 4,
  contact: 5,
};

function geometryFor(variant: SceneVariant, index: number) {
  const choice = (variantIndex[variant] + index) % 5;
  if (choice === 0) return new THREE.BoxGeometry(0.72, 0.72, 0.72);
  if (choice === 1) return new THREE.CylinderGeometry(0.38, 0.38, 0.12, 12);
  if (choice === 2) return new THREE.BoxGeometry(1.25, 0.38, 0.7);
  if (choice === 3) return new THREE.ConeGeometry(0.52, 0.85, 4);
  return new THREE.BoxGeometry(0.52, 1.1, 0.52);
}

export default function SectionScene({ variant }: Props) {
  const mountRef = useRef<HTMLDivElement>(null);
  const [active, setActive] = useState(false);
  const { resolvedTheme } = useTheme();

  useEffect(() => {
    const mount = mountRef.current;
    if (!mount) return;
    const observer = new IntersectionObserver(
      ([entry]) => setActive(entry.isIntersecting),
      { rootMargin: "400px 0px" },
    );
    observer.observe(mount);
    return () => observer.disconnect();
  }, []);

  useEffect(() => {
    const mount = mountRef.current;
    if (!mount || !active) return;
    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const isMobile = window.matchMedia("(max-width: 767px)").matches;
    const isDark = resolvedTheme === "dark";
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(48, 1, 0.1, 50);
    camera.position.z = 8;
    const renderer = new THREE.WebGLRenderer({ alpha: true, antialias: true });
    renderer.setPixelRatio(isMobile ? 1 : Math.min(window.devicePixelRatio, 1.35));
    renderer.outputColorSpace = THREE.SRGBColorSpace;
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = isDark ? 1.1 : 0.82;
    renderer.setClearColor(0x000000, 0);
    mount.appendChild(renderer.domElement);

    const root = new THREE.Group();
    scene.add(root);
    const palette = isDark
      ? [0xff6b57, 0xffca3a, 0x57b84b, 0x5fa8ff]
      : [0xe94f37, 0xffca3a, 0x57b84b, 0x2878d0];
    const seed = variantIndex[variant];
    const objects: THREE.Mesh[] = [];

    const objectCount = isMobile ? 5 : 9;
    for (let i = 0; i < objectCount; i += 1) {
      const material = new THREE.MeshPhysicalMaterial({
        color: palette[(i + seed) % palette.length],
        roughness: 0.82,
        metalness: 0.04,
        transparent: true,
        opacity: isDark ? (i < 3 ? 0.44 : 0.24) : (i < 3 ? 0.22 : 0.11),
        wireframe: false,
        emissive: palette[(i + seed + 1) % palette.length],
        emissiveIntensity: isDark ? 0.08 : 0,
        flatShading: true,
      });
      const mesh = new THREE.Mesh(geometryFor(variant, i), material);
      const side = i % 2 === 0 ? -1 : 1;
      mesh.position.set(side * (2.8 + (i % 3) * 0.75), ((i * 1.7 + seed) % 6) - 2.7, -0.4 - (i % 4) * 0.7);
      mesh.scale.setScalar(0.72 + (i % 3) * 0.22);
      mesh.rotation.set(i * 0.6, i * 0.35, i * 0.18);
      root.add(mesh);
      objects.push(mesh);
    }

    // A software-architecture floor grid anchors every scene in a shared environment.
    const grid = new THREE.GridHelper(15, 24, palette[1], palette[0]);
    grid.rotation.x = Math.PI / 2;
    grid.position.z = -3.5;
    const gridMaterials = Array.isArray(grid.material) ? grid.material : [grid.material];
    gridMaterials.forEach((material) => {
      material.transparent = true;
      material.opacity = isDark ? 0.09 : 0.045;
    });
    root.add(grid);

    // Floating code-window frames make the geometry read as an engineering workspace.
    const panels = (isMobile ? [] : [-1, 1]).map((side, index) => {
      const panel = new THREE.Group();
      const frameGeometry = new THREE.PlaneGeometry(2.2, 1.35);
      const edgeGeometry = new THREE.EdgesGeometry(frameGeometry);
      frameGeometry.dispose();
      const edges = new THREE.LineSegments(
        edgeGeometry,
        new THREE.LineBasicMaterial({ color: palette[index], transparent: true, opacity: isDark ? 0.24 : 0.12 }),
      );
      panel.add(edges);
      for (let lineIndex = 0; lineIndex < 5; lineIndex += 1) {
        const width = 0.65 + ((lineIndex + seed) % 3) * 0.3;
        const line = new THREE.Mesh(
          new THREE.PlaneGeometry(width, 0.025),
          new THREE.MeshBasicMaterial({ color: palette[(lineIndex + 1) % palette.length], transparent: true, opacity: isDark ? 0.3 : 0.16 }),
        );
        line.position.set(-0.75 + width / 2, 0.38 - lineIndex * 0.2, 0.01);
        panel.add(line);
      }
      panel.position.set(side * 4.05, index === 0 ? 1.65 : -1.55, -1.5);
      panel.rotation.set(side * -0.08, side * -0.2, side * 0.035);
      root.add(panel);
      return panel;
    });

    const linePoints: THREE.Vector3[] = [];
    objects.forEach((object, index) => {
      if (index < objects.length - 1) {
        linePoints.push(object.position.clone(), objects[index + 1].position.clone());
      }
    });
    const lineGeometry = new THREE.BufferGeometry().setFromPoints(linePoints);
    const lines = new THREE.LineSegments(lineGeometry, new THREE.LineBasicMaterial({ color: palette[1], transparent: true, opacity: isDark ? 0.18 : 0.08 }));
    root.add(lines);

    const particlesGeometry = new THREE.BufferGeometry();
    const particleCount = isMobile ? 28 : 75;
    const particlesArray = new Float32Array(particleCount * 3);
    for (let i = 0; i < particleCount; i += 1) {
      particlesArray[i * 3] = (Math.random() - 0.5) * 12;
      particlesArray[i * 3 + 1] = (Math.random() - 0.5) * 7;
      particlesArray[i * 3 + 2] = -1 - Math.random() * 4;
    }
    particlesGeometry.setAttribute("position", new THREE.BufferAttribute(particlesArray, 3));
    const particles = new THREE.Points(particlesGeometry, new THREE.PointsMaterial({ color: palette[1], size: 0.025, transparent: true, opacity: isDark ? 0.5 : 0.24 }));
    root.add(particles);

    scene.add(new THREE.HemisphereLight(isDark ? 0xbfe8ff : 0xffffff, isDark ? 0x020617 : 0xe2e8f0, isDark ? 2.2 : 1.65));
    const light = new THREE.PointLight(palette[1], isDark ? 12 : 5, 18);
    light.position.set(seed % 2 ? -3 : 3, 2, 4);
    scene.add(light);

    let pointerX = 0;
    let pointerY = 0;
    const onPointer = (event: PointerEvent) => {
      pointerX = (event.clientX / window.innerWidth - 0.5) * 0.3;
      pointerY = (event.clientY / window.innerHeight - 0.5) * 0.2;
    };
    window.addEventListener("pointermove", onPointer, { passive: true });

    const resize = () => {
      const bounds = mount.getBoundingClientRect();
      renderer.setSize(bounds.width, bounds.height, false);
      camera.aspect = bounds.width / Math.max(bounds.height, 1);
      camera.updateProjectionMatrix();
    };
    const resizeObserver = new ResizeObserver(resize);
    resizeObserver.observe(mount);
    resize();

    let visible = false;
    let frame = 0;
    const observer = new IntersectionObserver(([entry]) => { visible = entry.isIntersecting; }, { rootMargin: "150px" });
    observer.observe(mount);
    const clock = new THREE.Clock();
    let lastRenderedAt = 0;
    const render = () => {
      frame = requestAnimationFrame(render);
      if (!visible) return;
      const time = clock.getElapsedTime();
      if (isMobile && time - lastRenderedAt < 1 / 30) return;
      lastRenderedAt = time;
      if (!reducedMotion) {
        root.rotation.y += (pointerX - root.rotation.y) * 0.012;
        root.rotation.x += (-pointerY - root.rotation.x) * 0.012;
        objects.forEach((object, index) => {
          object.rotation.x += 0.0012 * (index % 3 + 1);
          object.rotation.y += 0.0018 * (index % 2 + 1);
          object.position.y += Math.sin(time * 0.45 + index) * 0.0008;
        });
        particles.rotation.z = time * 0.008;
        panels.forEach((panel, index) => {
          panel.position.y += Math.sin(time * 0.35 + index * 2) * 0.0005;
        });
      }
      renderer.render(scene, camera);
    };
    render();

    return () => {
      cancelAnimationFrame(frame);
      observer.disconnect();
      resizeObserver.disconnect();
      window.removeEventListener("pointermove", onPointer);
      scene.traverse((item) => {
        if (!(item instanceof THREE.Mesh || item instanceof THREE.Points || item instanceof THREE.LineSegments)) return;
        item.geometry.dispose();
        const materials = Array.isArray(item.material) ? item.material : [item.material];
        materials.forEach((material) => material.dispose());
      });
      renderer.dispose();
      renderer.domElement.remove();
    };
  }, [active, resolvedTheme, variant]);

  const actors: Record<SceneVariant, Array<{ src: string; className: string }>> = {
    about: [
      { src: "/avatar-pixel-wave.gif", className: "pixel-actor pixel-actor--right pixel-actor--large" },
      { src: "/pixel-cloud.png", className: "pixel-cloud pixel-cloud--left" },
    ],
    skills: [
      { src: "/avatar-pixel-working.gif", className: "pixel-actor pixel-actor--left pixel-actor--large" },
      { src: "/pixel-coin.gif", className: "pixel-pickup pixel-pickup--right" },
    ],
    experience: [
      { src: "/pixel-enemy-walking.gif", className: "pixel-actor pixel-actor--right" },
      { src: "/avatar-pixel-running.gif", className: "pixel-actor pixel-actor--left" },
    ],
    projects: [
      { src: "/pixel-portal.gif", className: "pixel-portal pixel-portal--right" },
      { src: "/pixel-slime-walking.gif", className: "pixel-actor pixel-actor--left pixel-actor--small" },
    ],
    education: [
      { src: "/avatar-pixel-jump.gif", className: "pixel-actor pixel-actor--right pixel-actor--large" },
      { src: "/pixel-mushroom.gif", className: "pixel-pickup pixel-pickup--left" },
    ],
    contact: [
      { src: "/pixel-portal.gif", className: "pixel-portal pixel-portal--left" },
      { src: "/kai-bot-pixel.gif", className: "pixel-actor pixel-actor--right pixel-actor--large" },
    ],
  };

  return (
    <>
      <div ref={mountRef} className={`section-scene section-scene--${variant}`} aria-hidden="true" />
      <div className={`pixel-level-actors pixel-level-actors--${variant}`} aria-hidden="true">
        {actors[variant].map((actor) => (
          <Image key={actor.src} src={actor.src} alt="" width={384} height={362} unoptimized className={actor.className} />
        ))}
      </div>
    </>
  );
}
