import { useState } from 'react';
import { toErrorMessage } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import { Field } from '../../../components/Field';
import type { EscalateTicketResponse, TicketDetail } from '../../../types/api';

interface EscalationPanelProps {
  ticket: TicketDetail;
  onEscalated: (response: EscalateTicketResponse) => void;
}

/**
 * Allows an agent or lead to escalate a ticket one priority level.
 * Invariants: reason 5–500 chars; cannot escalate Critical or Resolved/Closed tickets.
 */
export function EscalationPanel({ ticket, onEscalated }: EscalationPanelProps) {
  const [reason, setReason] = useState('');
  const [escalatedBy, setEscalatedBy] = useState('Dispatcher');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [validationError, setValidationError] = useState<string>();
  const [serverError, setServerError] = useState<string>();

  const isCritical = ticket.priority === 'Critical';
  const isClosedOrResolved = ticket.status === 'Resolved' || ticket.status === 'Closed';
  const canEscalate = !isCritical && !isClosedOrResolved;

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setValidationError(undefined);
    setServerError(undefined);

    const trimmedReason = reason.trim();
    if (trimmedReason.length < 5 || trimmedReason.length > 500) {
      setValidationError('Reason must be between 5 and 500 characters.');
      return;
    }

    const trimmedActor = escalatedBy.trim();
    if (!trimmedActor) {
      setValidationError('Escalated by is required.');
      return;
    }

    setIsSubmitting(true);
    try {
      const response = await ticketsApi.escalateTicket(ticket.id, {
        reason: trimmedReason,
        escalatedBy: trimmedActor,
      });
      setReason('');
      onEscalated(response);
    } catch (caught) {
      setServerError(toErrorMessage(caught, 'Could not escalate ticket.'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="card" data-testid="escalation-panel">
      <h2>Escalate ticket</h2>

      {!canEscalate && (
        <p className="field__hint" role="note" style={{ marginBottom: '1rem', color: 'var(--colour-muted)' }}>
          {isCritical
            ? 'This ticket is already at Critical priority and cannot be escalated.'
            : 'Resolved or closed tickets cannot be escalated.'}
        </p>
      )}

      <form onSubmit={handleSubmit} noValidate>
        <Field
          id="escalation-reason"
          label="Reason for escalation"
          error={validationError}
          hint="Describe why priority should be raised (5–500 characters)."
        >
          {(fieldProps) => (
            <textarea
              {...fieldProps}
              rows={3}
              value={reason}
              disabled={!canEscalate || isSubmitting}
              onChange={(e) => {
                setReason(e.target.value);
                if (validationError) setValidationError(undefined);
              }}
              placeholder="Why does this ticket need urgent escalation?"
            />
          )}
        </Field>

        <Field
          id="escalation-actor"
          label="Escalated by"
          hint="Name of the person requesting escalation."
        >
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="text"
              value={escalatedBy}
              disabled={!canEscalate || isSubmitting}
              onChange={(e) => setEscalatedBy(e.target.value)}
            />
          )}
        </Field>

        {serverError && (
          <div
            className="field__error"
            role="alert"
            data-testid="escalation-error"
            style={{ marginBottom: '1rem' }}
          >
            {serverError}
          </div>
        )}

        <button
          type="submit"
          className="button button--danger"
          disabled={!canEscalate || isSubmitting}
        >
          {isSubmitting ? 'Escalating...' : 'Escalate ticket'}
        </button>
      </form>
    </div>
  );
}
