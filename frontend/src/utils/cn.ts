// Utilidad para combinar clases de Tailwind condicionalmente
// Similar a la librería clsx pero más simple
export function cn(...classes: (string | undefined | null | false)[]): string {
  return classes.filter(Boolean).join(' ');
}