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
      id="home"
      aria-labelledby="hero-heading"
      className="hero-shell campaign-hero min-h-screen overflow-hidden bg-light-background dark:bg-dark-background"
    >
      <div className="campaign-hero__scene">
        <HeroScene />
      </div>
      <div className="campaign-hero__overlay" />
      <div className="campaign-hero__topline">
        <span>PORTFOLIO CAMPAIGN</span><span>IOWA · USA</span><span>2026 EDITION</span>
      </div>
      <div className="campaign-hero__layout">
        <motion.div
          variants={containerVariants}
          initial="hidden"
          animate="visible"
          className="campaign-hero__panel"
        >
          <Badge variants={itemVariants} />
          <Heading variants={itemVariants} />
          <Tagline variants={itemVariants} />
          <CTAButtons variants={itemVariants} />
          <ScrollIndicator />
        </motion.div>
        <motion.aside initial={{ opacity: 0, x: 30 }} animate={{ opacity: 1, x: 0 }} transition={{ duration: .7, delay: .5 }} className="campaign-stats">
          <span className="campaign-stats__title">PLAYER PROFILE</span>
          <div><strong>08+</strong><span>Years building</span></div>
          <div><strong>24</strong><span>Core skills</span></div>
          <div><strong>∞</strong><span>Ideas queued</span></div>
          <p><i /> AVAILABLE FOR PARTY</p>
        </motion.aside>
      </div>
    </section>
  );
}
