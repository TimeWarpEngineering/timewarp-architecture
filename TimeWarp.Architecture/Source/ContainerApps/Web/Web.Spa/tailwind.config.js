// tailwind.config.js

const defaultTheme = require("tailwindcss/defaultTheme");
const colors = require("tailwindcss/colors");

/** @type {import('tailwindcss').Config} */

module.exports = {
  corePlugins: {
    preflight: true,
  },
  content: [
    "**/*.razor",
    "**/*.razor.cs",
    "**/*.html",
    "./obj/css/Web.Spa.styles.css",
    "../Web.Server/**/*.razor",
    "../Web.Server/**/*.razor.cs",
    "../**/*.html",
  ],
  safelist: [
    {
      pattern:
        /bg-(primary|secondary|accent|danger|warning|positive|gray)-(50|100|200|300|400|500|600|700|800|900|950)/,
    },
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          DEFAULT: colors.blue,
          dark: colors.blue,
        },
        secondary: {
          DEFAULT: colors.gray,
          dark: colors.gray,
        },
        accent: {
          DEFAULT: colors.teal,
          dark: colors.teal,
        },
        danger: colors.red,
        warning: colors.amber,
        positive: colors.emerald,
        info: colors.blue,
      },
      fontFamily: {
        sans: ["Inter var", ...defaultTheme.fontFamily.sans],
      },
    },
  },
  plugins: [
    require("@tailwindcss/typography"),
    // require("@tailwindcss/forms"),
    require("@tailwindcss/aspect-ratio"),
  ],
};
