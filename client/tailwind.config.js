/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  safelist: [
    'bg-orange-100',
    'text-orange-800',
    'border-orange-300',
    'text-orange-700',
    'text-orange-600',
    'bg-yellow-100',
    'text-yellow-800',
    'border-yellow-300',
    'bg-blue-100',
    'text-blue-800',
    'border-blue-300',
    'bg-blue-500',
    'hover:bg-blue-600',
  ],
  theme: {
    extend: {},
  },
  plugins: [],
}
