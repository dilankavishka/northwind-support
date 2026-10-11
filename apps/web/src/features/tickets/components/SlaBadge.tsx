import { Badge, type BadgeTone } from '../../../components/Badge';
import type { SlaStatus } from '../../../types/api';
import { SLA_CONFIG } from '../sla';

export interface SlaBadgeProps {
  status: SlaStatus;
}

/**
 * Reusable SLA indicator displaying current SLA status.
 * Conveys status through tone, explicit text, and a distinct symbol so meaning is never
 * communicated by colour alone (WCAG 1.4.1).
 */
export function SlaBadge({ status }: SlaBadgeProps) {
  const config = SLA_CONFIG[status] ?? {
    text: status,
    tone: 'neutral' as BadgeTone,
    symbol: '•',
  };

  return (
    <Badge tone={config.tone}>
      <span
        data-testid={`sla-badge-${status}`}
        style={{ display: 'inline-flex', alignItems: 'center', gap: '0.3rem' }}
      >
        <span aria-hidden="true" style={{ fontSize: '0.8em', lineHeight: 1 }}>
          {config.symbol}
        </span>
        <span>{config.text}</span>
      </span>
    </Badge>
  );
}
