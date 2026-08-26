"use client";

import { useEffect, useRef } from "react";
import Image from "next/image";
import * as THREE from "three";

const palette = { grass: 0x57b84b, grassDark: 0x26863a, dirt: 0xb76532, brick: 0xd85632, brickDark: 0x8f2f29, gold: 0xffcc35, question: 0xffa928, cloud: 0xffffff };

export default function HeroScene() {
  const mountRef = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const mount = mountRef.current;
    if (!mount) return;
    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const isMobile = window.matchMedia("(max-width: 767px)").matches;
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(38, 1, 0.1, 100);
    camera.position.set(0, 2.2, 10.5);
    camera.lookAt(0, 0.4, 0);
    const renderer = new THREE.WebGLRenderer({ alpha: true, antialias: false });
    renderer.setPixelRatio(isMobile ? 1 : Math.min(window.devicePixelRatio, 1.5));
    renderer.outputColorSpace = THREE.SRGBColorSpace;
    renderer.shadowMap.enabled = !isMobile;
    renderer.shadowMap.type = THREE.BasicShadowMap;
    renderer.setClearColor(0x000000, 0);
    mount.appendChild(renderer.domElement);
    const world = new THREE.Group();
    world.rotation.x = -0.08;
    scene.add(world);
    const box = (color: number, size: [number, number, number], position: [number, number, number]) => {
      const mesh = new THREE.Mesh(new THREE.BoxGeometry(...size), new THREE.MeshLambertMaterial({ color, flatShading: true }));
      mesh.position.set(...position);
      mesh.castShadow = true;
      mesh.receiveShadow = true;
      world.add(mesh);
      return mesh;
    };
    for (let x = -4; x <= 4; x += 1) {
      box(palette.grass, [0.98, 0.3, 1.8], [x, -1.2, 0]);
      box(x % 2 ? palette.dirt : 0xc97838, [0.98, 0.72, 1.8], [x, -1.7, 0]);
    }
    box(palette.grassDark, [9.1, 0.1, 1.88], [0, -1.08, 0]);
    [-2.2, -1.25, 1.45, 2.4].forEach((x, index) => {
      const block = box(index === 2 ? palette.question : palette.brick, [0.84, 0.84, 0.84], [x, index === 2 ? 0.45 : -0.1, 0]);
      block.add(new THREE.LineSegments(new THREE.EdgesGeometry(block.geometry), new THREE.LineBasicMaterial({ color: index === 2 ? 0xb56514 : palette.brickDark })));
    });
    const coins: THREE.Mesh[] = [];
    [-3.25, -0.2, 0.72, 3.35].forEach((x, index) => {
      const coin = new THREE.Mesh(new THREE.CylinderGeometry(0.28, 0.28, 0.1, 12), new THREE.MeshLambertMaterial({ color: palette.gold, emissive: 0x8a4a00, emissiveIntensity: 0.18, flatShading: true }));
      coin.rotation.x = Math.PI / 2;
      coin.position.set(x, 0.75 + (index % 2) * 0.65, 0.1);
      coin.castShadow = true;
      world.add(coin);
      coins.push(coin);
    });
    [[0, 0], [-0.22, 0], [0.22, 0], [0, 0.18]].forEach(([x, y]) => box(palette.brick, [0.28, 0.2, 0.35], [3.05 + x, -0.25 + y, 0.15]));
    box(0xf7e7bc, [0.3, 0.35, 0.3], [3.05, -0.56, 0.15]);
    [[-3.2, 2.35, -2], [2.8, 2.7, -2.8]].forEach(([x, y, z]) => {
      box(palette.cloud, [1.35, 0.42, 0.45], [x, y, z]);
      box(palette.cloud, [0.55, 0.48, 0.48], [x - 0.25, y + 0.34, z]);
    });
    scene.add(new THREE.HemisphereLight(0xffffff, 0x5c8b4b, 2.4));
    const sun = new THREE.DirectionalLight(0xfff4cf, 3.2);
    sun.position.set(-4, 7, 7);
    sun.castShadow = true;
    scene.add(sun);
    let pointerX = 0;
    let pointerY = 0;
    const onPointer = (event: PointerEvent) => {
      const bounds = mount.getBoundingClientRect();
      pointerX = ((event.clientX - bounds.left) / Math.max(bounds.width, 1) - 0.5) * 0.22;
      pointerY = ((event.clientY - bounds.top) / Math.max(bounds.height, 1) - 0.5) * 0.12;
    };
    mount.addEventListener("pointermove", onPointer);
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
    const clock = new THREE.Clock();
    const animate = () => {
      frame = requestAnimationFrame(animate);
      const time = clock.getElapsedTime();
      if (!reduceMotion) {
        world.rotation.y += (pointerX - world.rotation.y) * 0.035;
        world.rotation.x += (-0.08 - pointerY - world.rotation.x) * 0.035;
        coins.forEach((coin, index) => { coin.rotation.y = time * 2.4 + index; coin.position.y += Math.sin(time * 3 + index) * 0.0015; });
      }
      renderer.render(scene, camera);
    };
    animate();
    return () => {
      cancelAnimationFrame(frame);
      resizeObserver.disconnect();
      mount.removeEventListener("pointermove", onPointer);
      scene.traverse((item) => {
        if (!(item instanceof THREE.Mesh || item instanceof THREE.LineSegments)) return;
        item.geometry.dispose();
        (Array.isArray(item.material) ? item.material : [item.material]).forEach((material) => material.dispose());
      });
      renderer.dispose();
      renderer.domElement.remove();
    };
  }, []);
  return <div ref={mountRef} className="hero-scene" aria-hidden="true"><div className="hero-game-hud"><span>WORLD</span><strong>1-1</strong><span>COINS</span><strong>× 24</strong></div><Image src="/pixel-cloud.png" alt="" width={384} height={341} className="hero-cloud hero-cloud--one" /><Image src="/pixel-cloud.png" alt="" width={384} height={341} className="hero-cloud hero-cloud--two" /><Image src="/pixel-coin.gif" alt="" width={362} height={362} unoptimized className="hero-pickup" /><Image src="/pixel-mushroom.gif" alt="" width={362} height={362} unoptimized className="hero-mushroom" /><Image src="/pixel-enemy-walking.gif" alt="" width={384} height={342} unoptimized className="hero-enemy" /><Image src="/avatar-pixel-running.gif" alt="" width={384} height={362} unoptimized className="hero-runner" /></div>;
}
