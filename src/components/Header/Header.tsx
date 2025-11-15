import Link from "next/link";
import { useRouter } from "next/router";
import { useEffect, useRef, useState } from "react";
import styles from "./Header.module.css";
import NavItem from "./NavItem";
import { navItems } from "../../data/data";

export default function Header() {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [strongSnap, setStrongSnap] = useState(false);
  const navRef = useRef<HTMLElement | null>(null);
  const btnRef = useRef<HTMLButtonElement | null>(null);

  const SCROLL_THRESHOLD = 12;

  function waitForScrollEnd(
    target: number,
    threshold = SCROLL_THRESHOLD,
    timeout = 1200
  ) {
    return new Promise<void>((resolve) => {
      const start = performance.now();
      let last = window.scrollY || window.pageYOffset || 0;
      let stableFrames = 0;
      function check() {
        const current = window.scrollY || window.pageYOffset || 0;
        if (Math.abs(current - target) <= threshold) {
          resolve();
          return;
        }
        if (Math.abs(current - last) < 0.5) {
          stableFrames++;
        } else {
          stableFrames = 0;
          last = current;
        }
        if (stableFrames >= 6) {
          resolve();
          return;
        }
        if (performance.now() - start > timeout) {
          resolve();
          return;
        }
        requestAnimationFrame(check);
      }
      requestAnimationFrame(check);
    });
  }

  async function performScrollToElement(el: HTMLElement) {
    const headerElem = document.querySelector("header") as HTMLElement | null;
    const headerH = headerElem
      ? Math.ceil(headerElem.getBoundingClientRect().height)
      : parseInt(
          getComputedStyle(document.documentElement)
            .getPropertyValue("--header-height")
            .trim() || "64"
        );
    const extraGap = 0;
    const top =
      el.getBoundingClientRect().top + window.scrollY - headerH + extraGap;
    // round to integer pixels to avoid fractional differences triggering a scroll
    const topRounded = Math.round(top);
    const current = Math.round(window.scrollY || window.pageYOffset || 0);
    if (Math.abs(current - topRounded) <= SCROLL_THRESHOLD) {
      return;
    }

    const main = document.querySelector("main.page-scroll");
    if (main) main.classList.add("page-scroll--nosnap");

    window.scrollTo({ top: topRounded, behavior: "smooth" });

    await waitForScrollEnd(top, SCROLL_THRESHOLD);

    if (main) main.classList.remove("page-scroll--nosnap");
  }

  function handleNav(e: React.MouseEvent, section: string, href = "/") {
    e.preventDefault();
    if (router.pathname === "/") {
      const el = document.getElementById(section);
      if (el) {
        performScrollToElement(el).catch(() => {});
      }
      setOpen(false);
      return;
    }
    const onComplete = () => {
      // Give the page a short moment to render DOM nodes before attempting to
      // query and scroll to the target element. This fixes cases where
      // `routeChangeComplete` fires before the element is mounted.
      setTimeout(() => {
        const el = document.getElementById(section);
        if (el) {
          performScrollToElement(el).catch(() => {});
        }
        setOpen(false);
      }, 40);
      router.events.off("routeChangeComplete", onComplete);
    };

    // Prefetch the home route to reduce perceived latency, then navigate
    router.prefetch(href).catch(() => {});
    router.events.on("routeChangeComplete", onComplete);
    router.push(href).catch(() => {
      router.events.off("routeChangeComplete", onComplete);
      setOpen(false);
    });
  }

  useEffect(() => {
    const handleRoute = () => setOpen(false);
    router.events.on("routeChangeStart", handleRoute);
    return () => router.events.off("routeChangeStart", handleRoute);
  }, [router.events]);

  useEffect(() => {
    const main = document.querySelector("main.page-scroll");
    if (main) setStrongSnap(main.classList.contains("page-scroll--strong"));
  }, []);

  function toggleSnapMode() {
    const main = document.querySelector("main.page-scroll");
    if (!main) return;
    const enabled = main.classList.toggle("page-scroll--strong");
    setStrongSnap(enabled);
  }

  useEffect(() => {
    function onDocClick(e: MouseEvent | TouchEvent) {
      if (!open) return;
      const target = (e.target as Node) || null;
      if (
        navRef.current &&
        !navRef.current.contains(target) &&
        btnRef.current &&
        !btnRef.current.contains(target)
      ) {
        setOpen(false);
      }
    }

    if (open) {
      document.addEventListener("mousedown", onDocClick);
      document.addEventListener("touchstart", onDocClick);
      const prev = document.body.style.overflow;
      document.body.style.overflow = "hidden";
      return () => {
        document.removeEventListener("mousedown", onDocClick);
        document.removeEventListener("touchstart", onDocClick);
        document.body.style.overflow = prev;
      };
    }

    return () => {};
  }, [open]);

  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape" && open) setOpen(false);
    }
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [open]);

  return (
    <header className={styles.header}>
      <div className={styles.container}>
        <Link href="/" className={styles.logo}>
          Kai
        </Link>
        <button
          ref={btnRef}
          className={`${styles.hamburger} ${open ? styles.open : ""}`}
          aria-label={open ? "Close menu" : "Toggle menu"}
          aria-expanded={open}
          onClick={() => setOpen((v) => !v)}
        >
          <span className={styles.hamburgerBar} />
          <span className={styles.hamburgerBar} />
          <span className={styles.hamburgerBar} />
        </button>

        <nav
          ref={navRef}
          className={`${styles.nav} ${open ? styles.open : ""}`}
        >
          {navItems.map((n) => (
            <NavItem
              key={n.id}
              href={n.href}
              ariaLabel={`Navigate to ${n.label}`}
              onClick={(e) => {
                setOpen(false);
                handleNav(e, n.section ?? "home", n.href);
              }}
            >
              {n.label}
            </NavItem>
          ))}
        </nav>
      </div>
    </header>
  );
}
