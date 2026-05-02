"use client";

import dynamic from "next/dynamic";

const Assistant = dynamic(() => import("@/components/sections/Assistant"), {
  ssr: false,
});

export default function AssistantLauncher() {
  return <Assistant />;
}
