"use client";

import { motion } from "framer-motion";
import {
  Badge,
  Heading,
  Tagline,
  CTAButtons,
  ScrollIndicator,
} from "./Hero/HeroComponents";

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
    <section className="min-h-screen flex items-center pt-20 bg-light-background dark:bg-dark-background">
      <div className="max-w-container mx-auto px-sm md:px-lg w-full">
        <motion.div
          variants={containerVariants}
          initial="hidden"
          animate="visible"
          className="text-center md:text-left max-w-3xl"
        >
          <Badge variants={itemVariants} />
          <Heading variants={itemVariants} />
          <Tagline variants={itemVariants} />
          <CTAButtons variants={itemVariants} />
          <ScrollIndicator />
        </motion.div>
      </div>
    </section>
  );
}
