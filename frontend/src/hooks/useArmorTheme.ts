"use client";

import { createContext, useContext } from "react";
import type { ArmorTheme } from "@/config/armorThemes";

type ArmorThemeContextValue = {
  theme: ArmorTheme;
  setTheme: (theme: ArmorTheme) => void;
};

export const ArmorThemeContext = createContext<ArmorThemeContextValue | null>(
  null,
);

export function useArmorTheme() {
  const context = useContext(ArmorThemeContext);
  if (!context) throw new Error("useArmorTheme must be used within Providers");
  return context;
}
