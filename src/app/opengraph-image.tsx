import { ImageResponse } from "next/og";

export const size = {
  width: 1200,
  height: 630,
};

export const contentType = "image/png";

export default function OpengraphImage() {
  return new ImageResponse(
    <div
      style={{
        display: "flex",
        width: "100%",
        height: "100%",
        background:
          "linear-gradient(135deg, #0f172a 0%, #1e293b 45%, #2563eb 100%)",
        color: "#f8fafc",
        padding: "64px",
        flexDirection: "column",
        justifyContent: "space-between",
        fontFamily: "Inter, Arial, sans-serif",
      }}
    >
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: "14px",
          fontSize: "30px",
          fontWeight: 600,
          color: "#bfdbfe",
        }}
      >
        <span
          style={{
            width: "18px",
            height: "18px",
            borderRadius: "999px",
            background: "#38bdf8",
          }}
        />
        Portfolio
      </div>

      <div style={{ display: "flex", flexDirection: "column", gap: "20px" }}>
        <h1 style={{ fontSize: "78px", margin: 0, lineHeight: 1.05 }}>
          Hang Kheang Taing
        </h1>
        <p
          style={{
            margin: 0,
            fontSize: "38px",
            color: "#e2e8f0",
            maxWidth: "1000px",
          }}
        >
          Software Engineer · C# · .NET Core · React · Cloud-native Systems
        </p>
      </div>

      <div style={{ fontSize: "28px", color: "#bae6fd" }}>
        kaitaing.netlify.app
      </div>
    </div>,
    size,
  );
}
