import Image from "next/image";
import Link from "next/link";
import type { ReactNode } from "react";

type StatusPageProps = {
  code: string;
  title: string;
  description: string;
  image: string;
  imageAlt: string;
  primaryHref?: string;
  primaryLabel?: string;
  onRetry?: () => void;
  children?: ReactNode;
};

export default function StatusPage({
  code,
  title,
  description,
  image,
  imageAlt,
  primaryHref = "/",
  primaryLabel = "Back to home",
  onRetry,
  children,
}: StatusPageProps) {
  return (
    <main className="status-page" id="main-content">
      <section className="status-card" aria-labelledby="status-heading">
        <div className="status-copy">
          <p className="status-code">{code}</p>
          <h1 id="status-heading">{title}</h1>
          <p className="status-description">{description}</p>
          {children}
          <div className="status-actions">
            {onRetry ? (
              <button className="energy-button" onClick={onRetry}>
                Try again
              </button>
            ) : null}
            <Link className={onRetry ? "ghost-button" : "energy-button"} href={primaryHref}>
              {primaryLabel}
            </Link>
            {code === "404" ? (
              <Link className="status-secondary-link" href="/#contact">
                Contact Kai
              </Link>
            ) : null}
          </div>
        </div>
        <div className="status-illustration">
          <Image
            src={image}
            alt={imageAlt}
            fill
            priority
            sizes="(max-width: 760px) 100vw, 48vw"
            className="status-illustration__image"
          />
        </div>
      </section>
    </main>
  );
}
