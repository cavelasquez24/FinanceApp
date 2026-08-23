import type { FinancialHealthScoreDto } from '../types/analytics.types';
import { cn } from '../../../utils/cn';

interface Props {
  data: FinancialHealthScoreDto;
}

const GRADE_COLOR: Record<string, string> = {
  A: '#8FA888',
  B: '#5C7A99',
  C: '#D4A855',
  D: '#C97B63',
  F: '#B85450',
};

const SCORE_COLOR = (score: number) => {
  if (score >= 80) return '#8FA888';
  if (score >= 60) return '#5C7A99';
  if (score >= 40) return '#D4A855';
  return '#C97B63';
};

export function FinancialHealthScore({ data }: Props) {
  const { score, grade, recommendations } = data;
  const color = SCORE_COLOR(score);
  const gradeColor = GRADE_COLOR[grade] ?? '#7C756E';

  // SVG arc for score gauge
  const R = 60;
  const cx = 80;
  const cy = 80;
  const circumference = 2 * Math.PI * R;
  // Use 270° arc (from -225° to +45°) — typical gauge shape
  const arcLength = (circumference * 270) / 360;
  const arcOffset = circumference - (arcLength * score) / 100;

  return (
    <div className="flex flex-col items-center gap-6 sm:flex-row sm:items-start">
      {/* Score circle */}
      <div className="flex shrink-0 flex-col items-center gap-2">
        <svg width="160" height="160" viewBox="0 0 160 160" aria-label={`Score ${score}`}>
          {/* Track */}
          <circle
            cx={cx}
            cy={cy}
            r={R}
            fill="none"
            stroke="#EFEAE2"
            strokeWidth={12}
            strokeDasharray={`${arcLength} ${circumference - arcLength}`}
            strokeDashoffset={circumference * (45 / 360)}
            strokeLinecap="round"
            transform={`rotate(-225 ${cx} ${cy})`}
          />
          {/* Fill */}
          <circle
            cx={cx}
            cy={cy}
            r={R}
            fill="none"
            stroke={color}
            strokeWidth={12}
            strokeDasharray={`${arcLength} ${circumference - arcLength}`}
            strokeDashoffset={arcOffset + circumference * (45 / 360)}
            strokeLinecap="round"
            transform={`rotate(-225 ${cx} ${cy})`}
            style={{ transition: 'stroke-dashoffset 0.6s ease' }}
          />
          {/* Score text */}
          <text
            x={cx}
            y={cy - 6}
            textAnchor="middle"
            fontSize="28"
            fontWeight="700"
            fill="#2C2A29"
            fontFamily="inherit"
          >
            {score}
          </text>
          <text
            x={cx}
            y={cy + 14}
            textAnchor="middle"
            fontSize="11"
            fill="#7C756E"
            fontFamily="inherit"
          >
            de 100
          </text>
        </svg>

        {/* Grade badge */}
        <span
          className="flex h-10 w-10 items-center justify-center rounded-full text-lg font-bold text-white"
          style={{ backgroundColor: gradeColor }}
        >
          {grade}
        </span>
      </div>

      {/* Recommendations */}
      {recommendations.length > 0 && (
        <div className="flex-1 space-y-2">
          <p className="text-xs font-semibold uppercase tracking-wide text-finflow-muted">
            Recomendaciones
          </p>
          <ul className="space-y-2">
            {recommendations.map((rec, i) => (
              <li
                key={i}
                className={cn(
                  'flex items-start gap-2 rounded-xl border border-[#EFEAE2] bg-finflow-cream p-3 text-xs text-finflow-dark'
                )}
              >
                <span className="mt-0.5 h-4 w-4 shrink-0 rounded-full bg-finflow-blue/15 text-center text-[10px] font-bold leading-4 text-finflow-blue">
                  {i + 1}
                </span>
                {rec}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
