import { useState } from "react";
import styles from "./Contact.module.css";

type ContactRequest = {
  name: string;
  email: string;
  message: string;
};

type ContactResponse = {
  message: string;
};

export default function Contact() {
  const [form, setForm] = useState<ContactRequest>({
    name: "",
    email: "",
    message: "",
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<
    Partial<Record<keyof ContactRequest, string>>
  >({});
  const [flipped, setFlipped] = useState(false);

  const proxyUrl = "/api/contacts";

  function validateEmail(value: string) {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(value);
  }

  function validateAll() {
    const errors: Partial<Record<keyof ContactRequest, string>> = {};
    if (!form.name.trim()) errors.name = "Please enter your name.";
    if (!form.email.trim()) errors.email = "Please enter your email.";
    else if (!validateEmail(form.email.trim()))
      errors.email = "Please enter a valid email.";
    if (!form.message.trim()) errors.message = "Please enter a message.";
    return { valid: Object.keys(errors).length === 0, errors } as const;
  }

  function validateField(field: keyof ContactRequest) {
    const copy: Partial<Record<keyof ContactRequest, string>> = {};
    if (field === "name") {
      if (!form.name.trim()) copy.name = "Please enter your name.";
    }
    if (field === "email") {
      if (!form.email.trim()) copy.email = "Please enter your email.";
      else if (!validateEmail(form.email.trim()))
        copy.email = "Please enter a valid email.";
    }
    if (field === "message") {
      if (!form.message.trim()) copy.message = "Please enter a message.";
    }
    setFieldErrors((prev) => ({ ...prev, ...copy }));
    return copy;
  }

  function update(field: keyof ContactRequest, value: string) {
    setForm((s) => ({ ...s, [field]: value }));
    setFieldErrors((prev) => ({ ...prev, [field]: undefined }));
  }

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    setSuccess(null);
    setLoading(true);

    const validation = validateAll();
    if (!validation.valid) {
      setFieldErrors(validation.errors);
      setLoading(false);
      return;
    }

    const payload: ContactRequest = {
      name: form.name.trim(),
      email: form.email.trim(),
      message: form.message.trim(),
    };
    await submitPayload(payload);
  }

  async function submitPayload(payload: ContactRequest) {
    setError(null);
    setSuccess(null);
    setLoading(true);
    try {
      const res = await fetch(proxyUrl, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });

      if (!res.ok) {
        setError("Something went wrong. Please try again.");
        const body = await res.text().catch(() => null);
        console.error("Contact form submit error (server):", res.status, body);
        return;
      }

      const data = (await res.json()) as ContactResponse | null;
      setSuccess(data?.message ?? "Message sent successfully.");
      setForm({ name: "", email: "", message: "" });
    } catch (err: any) {
      console.error("Contact form submit error (network):", err);
      setError("Something went wrong. Please try again.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <section
      id="contact"
      className={styles.wrap}
      aria-labelledby="contact-heading"
    >
      <div className={styles.inner}>
        <header className={styles.header}>
          <h1 id="contact-heading">Contact</h1>
          <p className={styles.lead}>
            Send a message to collaborate or say hello.
          </p>
        </header>

        <main className={styles.content}>
          <div className={styles.cardWrap}>
            <div className={`${styles.flip} ${flipped ? styles.flipped : ""}`}>
              <div className={styles.flipInner}>
                <div className={styles.flipFront}>
                  <form
                    className={styles.formCard}
                    onSubmit={handleSubmit}
                    noValidate
                  >
                    <div className={styles.grid}>
                      <label className={styles.field}>
                        <span className={styles.label}>
                          Name<span className={styles.required}>*</span>
                        </span>
                        <input
                          className={styles.input}
                          aria-invalid={!!fieldErrors.name}
                          name="name"
                          value={form.name}
                          onChange={(e) => update("name", e.target.value)}
                          onBlur={() => validateField("name")}
                          required
                        />
                        {fieldErrors.name && (
                          <div className={styles.fieldError}>
                            {fieldErrors.name}
                          </div>
                        )}
                      </label>

                      <label className={styles.field}>
                        <span className={styles.label}>
                          Email<span className={styles.required}>*</span>
                        </span>
                        <input
                          className={styles.input}
                          aria-invalid={!!fieldErrors.email}
                          name="email"
                          type="email"
                          value={form.email}
                          onChange={(e) => update("email", e.target.value)}
                          onBlur={() => validateField("email")}
                          required
                        />
                        {fieldErrors.email && (
                          <div className={styles.fieldError}>
                            {fieldErrors.email}
                          </div>
                        )}
                      </label>
                    </div>

                    <label className={styles.field}>
                      <span className={styles.label}>
                        Message<span className={styles.required}>*</span>
                      </span>
                      <textarea
                        className={styles.textarea}
                        aria-invalid={!!fieldErrors.message}
                        name="message"
                        value={form.message}
                        onChange={(e) => update("message", e.target.value)}
                        onBlur={() => validateField("message")}
                        required
                      />
                      {fieldErrors.message && (
                        <div className={styles.fieldError}>
                          {fieldErrors.message}
                        </div>
                      )}
                    </label>

                    <div className={styles.actions}>
                      <button
                        className={styles.submit}
                        type="submit"
                        disabled={loading || !validateAll().valid}
                      >
                        {loading ? "Sending..." : "Send message"}
                      </button>

                      <button
                        type="button"
                        className={styles.ghost}
                        onClick={() => setFlipped(true)}
                        aria-pressed={flipped}
                      >
                        Contact info
                      </button>

                      <div className={styles.status} aria-live="polite">
                        {success && (
                          <div className={styles.success}>{success}</div>
                        )}
                        {error && <div className={styles.error}>{error}</div>}
                      </div>
                    </div>
                  </form>
                </div>
                <div className={styles.flipBack}>
                  <div className={styles.backCard}>
                    <h3>Contact details</h3>
                    <p>
                      Email:{" "}
                      <a href="mailto:hangkheangtaing@gmail.com">
                        hangkheangtaing@gmail.com
                      </a>
                    </p>
                    <p>
                      Phone: <a href="tel:+16412330129">+1 641 233 0129</a>
                    </p>
                    <div style={{ marginTop: 12 }}>
                      <button
                        className={styles.ghost}
                        type="button"
                        onClick={() => setFlipped(false)}
                      >
                        Back to form
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </main>
      </div>
    </section>
  );
}
