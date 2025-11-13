import type { AppProps } from "next/app";
import Head from "next/head";
import Layout from "./../src/components/Layout";
import "../src/styles/globals.css";
import { useEffect } from "react";
import { useRouter } from "next/router";

export default function App({ Component, pageProps }: AppProps) {
  const router = useRouter();

  useEffect(() => {
    function waitForScrollEnd(target: number, threshold = 12, timeout = 1200) {
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
      const topRounded = Math.round(top);
      const current = Math.round(window.scrollY || window.pageYOffset || 0);
      if (Math.abs(current - topRounded) <= 12) return;
      const main = document.querySelector("main.page-scroll");
      if (main) main.classList.add("page-scroll--nosnap");
      window.scrollTo({ top: topRounded, behavior: "smooth" });
      await waitForScrollEnd(topRounded, 12);
      if (main) main.classList.remove("page-scroll--nosnap");
    }

    function handleRouteComplete(url: string) {
      if (typeof window === "undefined") return;
      const hash =
        window.location.hash ||
        (url && url.includes("#") ? `#${url.split("#")[1]}` : "");
      if (!hash) return;
      const id = hash.replace("#", "");
      setTimeout(() => {
        const el = document.getElementById(id);
        if (el) performScrollToElement(el).catch(() => {});
      }, 60);
    }

    router.events.on("routeChangeComplete", handleRouteComplete);
    if (typeof window !== "undefined" && window.location.hash) {
      const id = window.location.hash.replace("#", "");
      setTimeout(() => {
        const el = document.getElementById(id);
        if (el) performScrollToElement(el).catch(() => {});
      }, 60);
    }
    return () => {
      router.events.off("routeChangeComplete", handleRouteComplete);
    };
  }, [router.events]);

  return (
    <>
      <Head>
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <title>Kai Portfolio</title>
      </Head>
      <Layout>
        <Component {...pageProps} />
      </Layout>
    </>
  );
}
