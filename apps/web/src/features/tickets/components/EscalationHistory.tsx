import { formatDateTime } from '../../../lib/format';
import type { TicketEscalation } from '../../../types/api';
import { PriorityBadge } from './TicketBadges';

interface EscalationHistoryProps {
  escalations: TicketEscalation[];
  isLoading?: boolean;
}

/** Displays the immutable audit trail of escalations for a ticket, newest first. */
export function EscalationHistory({ escalations, isLoading }: EscalationHistoryProps) {
  return (
    <div className="card" data-testid="escalation-history">
      <h2>Escalation history</h2>

      {isLoading && <p className="field__hint">Loading escalations...</p>}

      {!isLoading && escalations.length === 0 && (
        <p className="field__hint">No escalations recorded for this ticket.</p>
      )}

      {escalations.length > 0 && (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {escalations.map((item) => (
            <li
              key={item.id}
              style={{
                borderLeft: '3px solid var(--colour-danger)',
                paddingLeft: '1rem',
                display: 'flex',
                flexDirection: 'column',
                gap: '0.4rem',
              }}
            >
              <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: '0.5rem', fontSize: '0.9rem' }}>
                <span style={{ fontWeight: 600 }}>{item.escalatedBy}</span>
                <span className="summary__note">escalated on {formatDateTime(item.escalatedAtUtc)}</span>
              </div>

              <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: '0.5rem', fontSize: '0.88rem' }}>
                <span>Priority:</span>
                <PriorityBadge priority={item.fromPriority} />
                <span>→</span>
                <PriorityBadge priority={item.toPriority} />
              </div>

              <div style={{ fontSize: '0.85rem', color: 'var(--colour-muted)' }}>
                <span>Agent: {item.fromAgent?.fullName ?? 'Unassigned'} → {item.toAgent?.fullName ?? 'Unassigned'}</span>
              </div>

              {item.toDueAtUtc && (
                <div style={{ fontSize: '0.85rem', color: 'var(--colour-muted)' }}>
                  <span>Due date: {formatDateTime(item.fromDueAtUtc)} → {formatDateTime(item.toDueAtUtc)}</span>
                </div>
              )}

              <div style={{ fontSize: '0.9rem', fontStyle: 'italic', marginTop: '0.2rem' }}>
                &ldquo;{item.reason}&rdquo;
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
