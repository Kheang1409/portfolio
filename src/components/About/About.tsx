import styles from "./About.module.css";

export default function About() {
  return (
    <section id="about" className={`${styles.aboutSection} container`}>
      <div className={styles.center}>
        <div className={styles.intro}>
          <h1 className={styles.title}>About</h1>
          <p className={styles.lead}>
            I'm passionate about staying up-to-date with the latest technologies
            and trends, ensuring my skills remain relevant and effective. With a
            strong foundation in computer science and a drive to continuously
            learn, I'm committed to delivering high-quality solutions that meet
            the evolving needs of businesses and organizations.
          </p>
          <div className={styles.actions}>
            <button
              type="button"
              className={styles.primary}
              onClick={() => {
                const el = document.getElementById("contact");
                if (el) el.scrollIntoView({ behavior: "smooth" });
              }}
            >
              Contact
            </button>
            <a
              href="/resume.pdf"
              className={styles.primary}
              download="Kai_Taing_Resume.pdf"
              style={{ marginLeft: 12 }}
            >
              Download Resume
            </a>
          </div>
        </div>
      </div>
    </section>
  );
}
