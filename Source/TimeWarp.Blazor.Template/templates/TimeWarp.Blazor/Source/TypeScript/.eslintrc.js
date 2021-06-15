module.exports = {
  env: {
    browser: true,
    es2020: true,
  },
  extends: [
    "airbnb-base",
    "plugin:import/errors",
    "plugin:import/warnings",
    "plugin:import/typescript",
    "plugin:prettier/recommended",
  ],
  parser: "@typescript-eslint/parser",
  parserOptions: {
    ecmaVersion: 2020,
    sourceType: "module",
  },
  plugins: ["@typescript-eslint", "prettier"],
  rules: {
    "prettier/prettier": "error",
    "no-console": "off",
    "no-undef": "off",
    "no-use-before-define": "off",
    "import/no-default-export": "error",
    "import/prefer-default-export": "off",
    "import/no-unresolved": "off",
    "import/extensions": ["error", "always"],
    "no-unused-vars": "off",
    "@typescript-eslint/no-unused-vars": ["error"],
  },
};
