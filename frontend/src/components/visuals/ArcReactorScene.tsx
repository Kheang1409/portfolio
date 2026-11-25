"use client";
import { useEffect, useRef } from "react";
import * as THREE from "three";

export default function ArcReactorScene() {
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const host = ref.current;
    if (!host || !window.WebGLRenderingContext) return;
    const reduced = matchMedia("(prefers-reduced-motion: reduce)").matches;
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(42, 1, 0.1, 100);
    camera.position.z = 8;
    const renderer = new THREE.WebGLRenderer({
      alpha: true,
      antialias: innerWidth > 700,
      powerPreference: "low-power",
    });
    renderer.setPixelRatio(
      Math.min(devicePixelRatio, innerWidth < 700 ? 1 : 1.5),
    );
    host.appendChild(renderer.domElement);
    const group = new THREE.Group();
    scene.add(group);
    const cyan = new THREE.Color(
      getComputedStyle(document.documentElement)
        .getPropertyValue("--arc-cyan")
        .trim() || "#78e8ff",
    );
    const materials: THREE.Material[] = [];
    [1.05, 1.48, 2.05, 2.7].forEach((radius, i) => {
      const geometry = new THREE.TorusGeometry(
        radius,
        i === 0 ? 0.055 : 0.018,
        8,
        96,
      );
      const material = new THREE.MeshBasicMaterial({
        color: cyan,
        transparent: true,
        opacity: 0.75 - i * 0.12,
      });
      materials.push(material);
      const mesh = new THREE.Mesh(geometry, material);
      mesh.rotation.x = i % 2 ? 0.65 : 0;
      mesh.rotation.y = i % 3 ? 0.18 : -0.2;
      group.add(mesh);
    });
    const coreGeo = new THREE.CircleGeometry(0.72, 64);
    const coreMat = new THREE.MeshBasicMaterial({
      color: 0xeaffff,
      transparent: true,
      opacity: 0.35,
    });
    materials.push(coreMat);
    group.add(new THREE.Mesh(coreGeo, coreMat));
    const pointsGeo = new THREE.BufferGeometry();
    const count = innerWidth < 700 ? 80 : 180;
    const positions = new Float32Array(count * 3);
    for (let i = 0; i < count; i++) {
      const a = Math.random() * Math.PI * 2,
        r = 3 + Math.random() * 2.8;
      positions[i * 3] = Math.cos(a) * r;
      positions[i * 3 + 1] = Math.sin(a) * r;
      positions[i * 3 + 2] = (Math.random() - 0.5) * 2;
    }
    pointsGeo.setAttribute("position", new THREE.BufferAttribute(positions, 3));
    const pointsMat = new THREE.PointsMaterial({
      color: cyan,
      size: 0.035,
      transparent: true,
      opacity: 0.55,
    });
    materials.push(pointsMat);
    group.add(new THREE.Points(pointsGeo, pointsMat));
    const resize = () => {
      renderer.setSize(host.clientWidth, host.clientHeight, false);
      camera.aspect = host.clientWidth / Math.max(host.clientHeight, 1);
      camera.updateProjectionMatrix();
    };
    const observer = new ResizeObserver(resize);
    observer.observe(host);
    resize();
    let frame = 0,
      visible = true;
    const visibility = new IntersectionObserver(([e]) => {
      visible = e.isIntersecting;
    });
    visibility.observe(host);
    const render = () => {
      if (visible) {
        group.rotation.z += reduced ? 0 : 0.0007;
        group.children.forEach(
          (c, i) => (c.rotation.z += (i % 2 ? 1 : -1) * (reduced ? 0 : 0.0012)),
        );
        renderer.render(scene, camera);
      }
      frame = requestAnimationFrame(render);
    };
    frame = requestAnimationFrame(render);
    return () => {
      cancelAnimationFrame(frame);
      observer.disconnect();
      visibility.disconnect();
      group.traverse((o) => {
        if (o instanceof THREE.Mesh || o instanceof THREE.Points)
          o.geometry.dispose();
      });
      materials.forEach((m) => m.dispose());
      renderer.dispose();
      renderer.domElement.remove();
    };
  }, []);
  return <div ref={ref} className="arc-reactor-scene" aria-hidden="true" />;
}
