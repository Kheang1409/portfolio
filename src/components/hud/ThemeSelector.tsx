"use client";

import { useState } from "react";
import { armorThemes, type ArmorTheme } from "@/config/armorThemes";
import { useArmorTheme } from "@/hooks/useArmorTheme";

export default function ThemeSelector() {
  const [open, setOpen] = useState(false);
  const { theme, setTheme } = useArmorTheme();
  return (
    <div className="theme-selector">
      <button
        type="button"
        className="theme-trigger"
        aria-expanded={open}
        onClick={() => setOpen(!open)}
      >
        <span>SUIT</span> {armorThemes[theme].label}
      </button>
      {open && (
        <div className="theme-menu" role="menu" aria-label="Suit configuration">
          <p>SUIT CONFIGURATION</p>
          {(
            Object.entries(armorThemes) as [
              ArmorTheme,
              (typeof armorThemes)[ArmorTheme],
            ][]
          ).map(([id, item]) => (
            <button
              key={id}
              role="menuitemradio"
              aria-checked={theme === id}
              onClick={() => {
                setTheme(id);
                setOpen(false);
              }}
            >
              <span>{item.label}</span>
              <small>{item.description}</small>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
