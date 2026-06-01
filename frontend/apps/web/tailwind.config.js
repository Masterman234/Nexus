import tailwindcssAnimate from "tailwindcss-animate"

/** @type {import('tailwindcss').Config} */
export default {
  darkMode: ["class"],
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    container: {
      center: true,
      padding: "2rem",
      screens: {
        "2xl": "1400px",
      },
    },
    extend: {
      colors: {
        border: "#334155",
        input: "#334155",
        ring: "#06B6D4",
        background: "#0F172A",
        foreground: "#FFFFFF",
        primary: {
          DEFAULT: "#06B6D4",
          foreground: "#FFFFFF",
        },
        secondary: {
          DEFAULT: "#1E293B",
          foreground: "#CBD5E1",
        },
        destructive: {
          DEFAULT: "#EF4444",
          foreground: "#FFFFFF",
        },
        muted: {
          DEFAULT: "#1E293B",
          foreground: "#CBD5E1",
        },
        accent: {
          DEFAULT: "#06B6D4",
          foreground: "#FFFFFF",
        },
        popover: {
          DEFAULT: "#0F172A",
          foreground: "#FFFFFF",
        },
        card: {
          DEFAULT: "#1E293B",
          foreground: "#FFFFFF",
        },
        surface: "#F8FAFC",
        success: "#10B981",
        warning: "#F59E0B",
        danger: "#EF4444",
      },
      borderRadius: {
        lg: "12px",
        md: "10px",
        sm: "8px",
      },
      keyframes: {
        "accordion-down": {
          from: { height: "0" },
          to: { height: "var(--radix-accordion-content-height)" },
        },
        "accordion-up": {
          from: { height: "var(--radix-accordion-content-height)" },
          to: { height: "0" },
        },
      },
      animation: {
        "accordion-down": "accordion-down 0.2s ease-out",
        "accordion-up": "accordion-up 0.2s ease-out",
      },
    },
  },
  plugins: [tailwindcssAnimate],
}
