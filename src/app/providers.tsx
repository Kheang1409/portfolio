"use client";

import { MotionConfig } from "framer-motion";
import { ReactNode, useEffect, useState } from "react";
import {
  armorThemes,
  defaultArmorTheme,
  type ArmorTheme,
} from "@/config/armorThemes";
import { ArmorThemeContext } from "@/hooks/useArmorTheme";

const STORAGE_KEY = "kai-armor-theme";

export function Providers({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState<ArmorTheme>(defaultArmorTheme);

  useEffect(() => {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved && saved in armorThemes) setTheme(saved as ArmorTheme);
  }, []);

  useEffect(() => {
    document.documentElement.dataset.armorTheme = theme;
    localStorage.setItem(STORAGE_KEY, theme);
  }, [theme]);

  return (
    <ArmorThemeContext.Provider value={{ theme, setTheme }}>
      <MotionConfig reducedMotion="user">{children}</MotionConfig>
    </ArmorThemeContext.Provider>
  );
}
