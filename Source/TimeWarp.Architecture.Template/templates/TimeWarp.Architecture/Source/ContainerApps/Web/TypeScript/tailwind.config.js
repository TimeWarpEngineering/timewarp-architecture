// tailwind.config.js

const defaultTheme = require("tailwindcss/defaultTheme");

module.exports = {
  purge: {
    content: ["../Client/**/*.razor", "../**/*.html"],
    options: {
      whitelist: [""],
    },
  },
  darkmode: false,
  theme: {
    extend: {
      colors: {
        primary: defaultTheme.colors.blue,
        secondary: defaultTheme.colors.gray,
        accent: defaultTheme.colors.teal,
        danger: defaultTheme.colors.red,
        warning: defaultTheme.colors.yellow,
        positive: defaultTheme.colors.green,
      },
      fontFamily: {
        sans: ["Inter var", ...defaultTheme.fontFamily.sans],
      },
    },
  },
  variants: {
    borderWidth: ["responsive", "hover"],
    textColor: ["responsive", "hover", "focus", "visited"],
  },
  plugins: [],
};
