import React from "react";

type Props = {
  href: string;
  onClick?: (e: React.MouseEvent<HTMLAnchorElement, MouseEvent>) => void;
  children: React.ReactNode;
  className?: string;
  ariaLabel?: string;
};

export default function NavItem({
  href,
  onClick,
  children,
  className,
  ariaLabel,
}: Props) {
  return (
    <a
      href={href}
      onClick={(e) => {
        onClick?.(e as any);
      }}
      className={className}
      aria-label={ariaLabel}
      role="link"
      tabIndex={0}
    >
      {children}
    </a>
  );
}
