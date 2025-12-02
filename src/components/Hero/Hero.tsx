import React, { useEffect, useState } from "react";
import styles from "./Hero.module.css";

type Props = {
  avatarSrc?: string;
};

export default function Hero({ avatarSrc = "/avatar.jpg" }: Props) {
  const titles = [
    "Full-Stack Developer",
    "Backend Developer",
    "Software Engineer",
  ];

  const [currentTitleIndex, setCurrentTitleIndex] = useState(0);
  const [displayText, setDisplayText] = useState("");
  const [isDeleting, setIsDeleting] = useState(false);
  const [pause, setPause] = useState(false);

  const maxTitleChars = Math.max(...titles.map((t) => t.length));
  const reservedChars = Math.min(Math.max(maxTitleChars, 12), 28);

  useEffect(() => {
    if (pause) return;

    const currentTitle = titles[currentTitleIndex];
    const isComplete = !isDeleting && displayText === currentTitle;
    const isEmpty = isDeleting && displayText === "";

    if (isComplete) {
      setPause(true);
      setTimeout(() => {
        setIsDeleting(true);
        setPause(false);
      }, 1200);
      return;
    }

    if (isEmpty) {
      setPause(true);
      setTimeout(() => {
        setIsDeleting(false);
        setCurrentTitleIndex((prev) => (prev + 1) % titles.length);
        setPause(false);
      }, 400); // pause before next word
      return;
    }

    const nextText = isDeleting
      ? currentTitle.slice(0, displayText.length - 1)
      : currentTitle.slice(0, displayText.length + 1);

    // Add a touch of randomness for natural feel
    const baseSpeed = isDeleting ? 60 : 130;
    const randomDelay = baseSpeed + Math.random() * 50;

    const timeout = setTimeout(() => {
      setDisplayText(nextText);
    }, randomDelay);

    return () => clearTimeout(timeout);
  }, [displayText, isDeleting, pause, currentTitleIndex, titles]);

  return (
    <section id="home" className={`container ${styles.fullSection}`}>
      <div className={styles.center}>
        <div className={styles.hero}>
          <div className={styles.intro}>
            <div className={styles.columns}>
              <div className={styles.textCol}>
                <h1 className={styles.title}>Hi, I'm Hang Kheang Taing</h1>

                <h3 className={styles.rotatingTitle}>
                  <span
                    className={styles.rotatingItem}
                    style={{
                      width: `${reservedChars}ch`,
                      display: "inline-block",
                      overflow: "hidden",
                      verticalAlign: "bottom",
                    }}
                  >
                    {displayText}
                    <span className={styles.cursor} aria-hidden="true" />
                  </span>
                </h3>

                <p className={styles.lead}>
                  As a driven software developer with a passion for innovation,
                  I leverage technology to drive business growth and
                  improvement. With expertise in .NET, full-stack development,
                  and database management, I deliver scalable solutions that
                  enhance operational efficiency. Currently pursuing an MS in
                  Computer Science at Maharishi International University, I'm
                  committed to lifelong learning and staying ahead of industry
                  trends.
                </p>

                <div className={styles.socialIcons}>
                  <a
                    href="https://www.linkedin.com/in/hang-kheang-taing"
                    target="_blank"
                    rel="noreferrer"
                    aria-label="LinkedIn"
                  >
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      viewBox="0 0 24 24"
                      fill="currentColor"
                      width="20"
                      height="20"
                      aria-hidden="true"
                    >
                      <path d="M4.98 3.5C4.98 4.88 3.88 6 2.5 6S0 4.88 0 3.5 1.12 1 2.5 1 4.98 2.12 4.98 3.5zM.5 8h4V24h-4V8zM8.5 8h3.84v2.18h.05c.54-1.02 1.86-2.09 3.83-2.09 4.1 0 4.85 2.7 4.85 6.21V24h-4V15.2c0-2.1-.04-4.8-2.92-4.8-2.92 0-3.37 2.28-3.37 4.64V24h-4V8z" />
                    </svg>
                  </a>

                  <a
                    href="https://github.com/Kheang1409"
                    target="_blank"
                    rel="noreferrer"
                    aria-label="GitHub"
                  >
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      viewBox="0 0 24 24"
                      fill="currentColor"
                      width="20"
                      height="20"
                      aria-hidden="true"
                    >
                      <path d="M12 .5C5.73.5.5 5.73.5 12c0 5.08 3.29 9.39 7.86 10.92.58.11.8-.25.8-.56 0-.28-.01-1.02-.02-2-3.2.7-3.88-1.54-3.88-1.54-.53-1.35-1.3-1.71-1.3-1.71-1.06-.72.08-.71.08-.71 1.17.08 1.78 1.2 1.78 1.2 1.04 1.78 2.73 1.27 3.4.97.11-.76.41-1.27.74-1.56-2.55-.29-5.23-1.28-5.23-5.69 0-1.26.45-2.29 1.2-3.1-.12-.29-.52-1.47.11-3.06 0 0 .98-.31 3.2 1.19.93-.26 1.92-.4 2.91-.4.99 0 1.98.14 2.91.4 2.22-1.5 3.2-1.19 3.2-1.19.63 1.59.23 2.77.11 3.06.75.81 1.2 1.84 1.2 3.1 0 4.42-2.69 5.4-5.25 5.68.42.36.79 1.08.79 2.18 0 1.57-.01 2.84-.01 3.23 0 .31.21.68.81.56C20.71 21.39 24 17.08 24 12 24 5.73 18.27.5 12 .5z" />
                    </svg>
                  </a>
                </div>
              </div>

              <div className={styles.avatarCol}>
                <div className={styles.heroAvatarWrap}>
                  <img
                    src={avatarSrc}
                    alt="Hang Kheang Taing profile"
                    className={styles.heroAvatar}
                  />
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
