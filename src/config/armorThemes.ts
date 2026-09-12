export const armorThemes = {
  mark2: { label: "MK II", description: "Prototype titanium" },
  mark3: { label: "MK III", description: "Classic alloy" },
  mark45: { label: "MK 45", description: "Crimson systems" },
  mark46: { label: "MK 46", description: "Tactical nodes" },
  mark50: { label: "MK 50", description: "Nanotech assembly" },
  mark85: { label: "MK 85", description: "Advanced nanotech" },
} as const;

export type ArmorTheme = keyof typeof armorThemes;
export const defaultArmorTheme: ArmorTheme = "mark85";
