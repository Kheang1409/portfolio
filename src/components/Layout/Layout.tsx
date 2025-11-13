import type { PropsWithChildren } from "react";
import Header from "../Header";
import Assistant from "../Assistant";

export default function Layout({ children }: PropsWithChildren) {
  return (
    <div className="site-root">
      <Header />
      <main className="container">{children}</main>
      <Assistant />
    </div>
  );
}
