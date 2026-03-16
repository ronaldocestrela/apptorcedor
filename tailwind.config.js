/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        ffc: {
          green: '#1B5E20',
          'green-light': '#2E7D32',
          'bg': '#F5F5F5',
        },
      },
    },
  },
  plugins: [],
}
