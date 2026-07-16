/** @type {import('tailwindcss').Config} */
export default {
  // Le dice a Tailwind dónde buscar clases para incluirlas en el bundle
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      // Paleta de colores personalizada del proyecto
      colors: {
        primary: {
          50:  '#f0fdf4',
          100: '#dcfce7',
          200: '#bbf7d0',
          300: '#86efac',
          400: '#4ade80',
          500: '#22c55e',  // color principal
          600: '#16a34a',
          700: '#15803d',
          800: '#166534',
          900: '#14532d',
        },
        income:  '#22c55e',   // verde para ingresos
        expense: '#ef4444',   // rojo para gastos
        saving:  '#3b82f6',   // azul para ahorros
        invest:  '#8b5cf6',   // violeta para inversiones
      },
      // Fuente del proyecto
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [
    require('@tailwindcss/forms'), // estilos base para inputs y formularios
  ],
}