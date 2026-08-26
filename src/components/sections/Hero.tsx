"use client";

import { motion } from "framer-motion";
import {
  Badge,
  Heading,
  Tagline,
  CTAButtons,
  ScrollIndicator,
} from "./Hero/HeroComponents";
import HeroScene from "@/components/three/HeroScene";

export default function Hero() {
  const containerVariants = {
    hidden: { opacity: 0 },
    visible: {
      opacity: 1,
      transition: {
        staggerChildren: 0.2,
        delayChildren: 0.1,
      },
    },
  };

  const itemVariants = {
    hidden: { opacity: 0, y: 20 },
    visible: {
      opacity: 1,
      y: 0,
      transition: { duration: 0.5, ease: "easeOut" },
    },
  };

  return (
    <section
      aria-labelledby="hero-heading"
      className="hero-shell min-h-screen flex items-center pt-20 overflow-hidden bg-light-background dark:bg-dark-background"
    >
      <div className="max-w-container mx-auto px-sm md:px-lg w-full grid lg:grid-cols-[1.08fr_0.92fr] items-center gap-xl lg:gap-lg">
        <motion.div
          variants={containerVariants}
          initial="hidden"
          animate="visible"
          className="relative z-10 text-center md:text-left max-w-3xl py-xl lg:py-0"
        >
          <Badge variants={itemVariants} />
          <Heading variants={itemVariants} />
          <Tagline variants={itemVariants} />
          <CTAButtons variants={itemVariants} />
          <ScrollIndicator />
        </motion.div>
        <motion.div
          initial={{ opacity: 0, scale: 0.88, y: 24 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          transition={{ duration: 0.9, delay: 0.25, ease: [0.22, 1, 0.36, 1] }}
          className="relative h-[340px] sm:h-[430px] lg:h-[610px] -mt-xl lg:mt-0"
        >
          <HeroScene />
        </motion.div>
      </div>
    </section>
  );
}
