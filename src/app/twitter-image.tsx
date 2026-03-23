import { ImageResponse } from "next/og";

export const size = {
  width: 1200,
  height: 630,
};

export const contentType = "image/png";

export default function TwitterImage() {
  return new ImageResponse(
    <div
      style={{
        display: "flex",
        width: "100%",
        height: "100%",
        background: "linear-gradient(140deg, #020617 0%, #1d4ed8 100%)",
        color: "#f8fafc",
        padding: "64px",
        flexDirection: "column",
        justifyContent: "space-between",
        fontFamily: "Inter, Arial, sans-serif",
      }}
    >
      <div style={{ fontSize: "28px", color: "#bae6fd" }}>
        Hang Kheang Taing
      </div>

      <h1 style={{ margin: 0, fontSize: "74px", lineHeight: 1.05 }}>
        Software Engineer Portfolio
      </h1>

      <p style={{ margin: 0, fontSize: "34px", color: "#e2e8f0" }}>
        C# · .NET Core · ASP.NET Core · React · Azure · AWS
      </p>
    </div>,
    size,
  );
}
