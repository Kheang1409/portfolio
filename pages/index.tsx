import blogs from "../src/data/blogs.json";
import projects from "../src/data/projects.json";
import timelines from "../src/data/timeline.json";
import TimeSection from "../src/components/TimeSection";
import Hero from "../src/components/Hero";
import About from "../src/components/About";
import Portfolio from "../src/components/Portfolio";
import BlogList from "../src/components/BlogList";
import Contact from "../src/components/Contact/Contact";

export default function Home() {
  return (
    <main className="page-scroll">
      <Hero />
      <About />
      <Portfolio projects={projects} />
      <TimeSection timelines={timelines} />
      <BlogList blogs={blogs} />
      <Contact />
    </main>
  );
}
