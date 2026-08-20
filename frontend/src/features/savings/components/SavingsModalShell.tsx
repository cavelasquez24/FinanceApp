import { X } from "lucide-react";

export const savingsInputClass = "input-restoration";

interface ShellProps {
  eyebrow: string;
  title: string;
  description: string;
  onClose: () => void;
  children: React.ReactNode;
  maxWidth?: string;
}

export function SavingsModalShell({
  eyebrow,
  title,
  description,
  onClose,
  children,
  maxWidth = "max-w-2xl",
}: ShellProps) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-[#2C2A29]/55 p-3 backdrop-blur-sm sm:p-5">
      <div
        className={`max-h-[94vh] w-full ${maxWidth} overflow-y-auto rounded-[30px] border border-[#E8E1D8] bg-[#FBF9F4] shadow-2xl`}
      >
        <header className="sticky top-0 z-10 flex items-start justify-between gap-4 border-b border-[#E8E1D8] bg-[#FBF9F4]/95 px-6 py-5 backdrop-blur sm:px-8">
          <div>
            <p className="mb-1 text-xs font-semibold uppercase tracking-[0.18em] text-[#7A4B3A]">
              {eyebrow}
            </p>
            <h2 className="font-serif text-2xl font-semibold text-[#2C2A29] sm:text-3xl">
              {title}
            </h2>
            <p className="mt-1 max-w-2xl text-sm text-[#7C756E]">
              {description}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-full p-2 text-[#7C756E] transition hover:bg-[#EFEAE2]"
            aria-label="Cerrar"
          >
            <X className="h-5 w-5" />
          </button>
        </header>
        {children}
      </div>
    </div>
  );
}

export function SavingsModalSection({
  icon,
  number,
  title,
  description,
  children,
}: {
  icon: React.ReactNode;
  number?: string;
  title: string;
  description?: string;
  children: React.ReactNode;
}) {
  return (
    <section className="rounded-[24px] border border-[#E8E1D8] bg-white/65 p-5 sm:p-6">
      <div className="mb-5 flex items-start gap-3">
        <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-[#F4E8E2] text-[#7A4B3A]">
          {icon}
        </span>
        <div>
          <h3 className="font-semibold text-[#2C2A29]">
            {number ? `${number}. ` : ""}
            {title}
          </h3>
          {description && (
            <p className="mt-0.5 text-sm text-[#7C756E]">{description}</p>
          )}
        </div>
      </div>
      {children}
    </section>
  );
}

export function SavingsField({
  label,
  hint,
  className = "",
  children,
}: {
  label: string;
  hint?: string;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <label className={`block text-sm font-medium text-[#5F5953] ${className}`}>
      <span className="mb-1.5 block">{label}</span>
      {children}
      {hint && (
        <span className="mt-1.5 block text-xs font-normal text-[#8B837B]">
          {hint}
        </span>
      )}
    </label>
  );
}

export function SavingsMetric({
  label,
  value,
  tone = "default",
}: {
  label: string;
  value: string;
  tone?: "default" | "warning" | "success";
}) {
  const tones = {
    default: "border-[#E8E1D8] bg-[#FBF9F4] text-[#2C2A29]",
    warning: "border-amber-200 bg-amber-50 text-amber-900",
    success: "border-[#DDE7D8] bg-[#F3F7F0] text-[#304B38]",
  };
  return (
    <div className={`rounded-2xl border p-3 ${tones[tone]}`}>
      <span className="block text-xs text-[#7C756E]">{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

export function SavingsModalActions({
  onClose,
  isPending,
  submitLabel,
  disabled = false,
  danger = false,
}: {
  onClose: () => void;
  isPending: boolean;
  submitLabel: string;
  disabled?: boolean;
  danger?: boolean;
}) {
  return (
    <div className="flex flex-col-reverse justify-end gap-3 border-t border-[#E8E1D8] pt-5 sm:flex-row">
      <button
        type="button"
        onClick={onClose}
        disabled={isPending}
        className="rounded-xl bg-[#EFEAE2] px-5 py-2.5 font-medium text-[#5F5953] transition hover:bg-[#E5DED5]"
      >
        Cancelar
      </button>
      <button
        type="submit"
        disabled={isPending || disabled}
        className={`rounded-xl px-5 py-2.5 font-medium text-white transition disabled:cursor-not-allowed disabled:opacity-45 ${danger ? "bg-[#B5573F] hover:bg-[#93442F]" : "bg-[#7A4B3A] hover:bg-[#633C2F]"}`}
      >
        {isPending ? "Guardando..." : submitLabel}
      </button>
    </div>
  );
}
